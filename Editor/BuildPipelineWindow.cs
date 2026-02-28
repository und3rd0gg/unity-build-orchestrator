using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildOrchestrator.Editor
{
    public sealed class BuildPipelineWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Build/Build Pipeline Manager";
        private const string FileMenuPath = "File/Build Profiles/Build Pipeline Manager";

        [Serializable]
        private sealed class FlagStateItem
        {
            public string Id;
            public bool Value;
        }

        [SerializeField] private BuildPipelineConfig _config;
        [SerializeField] private string _selectedProfileId = string.Empty;
        [SerializeField] private List<FlagStateItem> _flagState = new();
        [SerializeField] private bool _isBuilding;

        private Vector2 _scroll;

        [MenuItem(MenuPath)]
        [MenuItem(FileMenuPath, false, 2100)]
        public static void Open()
        {
            BuildPipelineWindow window = GetWindow<BuildPipelineWindow>();
            window.titleContent = new GUIContent("Build Pipeline");
            window.minSize = new Vector2(700f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_config == null)
            {
                _config = BuildPipelineConfigUtility.LoadConfig();
            }

            EnsureSelectedProfile();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_config == null)
            {
                DrawMissingConfigState();
                return;
            }

            BuildValidationReport validation = BuildProfileValidator.ValidateConfig(_config);
            DrawValidation(validation);

            BuildProfileConfig selectedProfile = DrawProfileSelector();
            if (selectedProfile == null)
            {
                return;
            }

            EnsureFlagStateForProfile(selectedProfile);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawProfileInfo(selectedProfile);
            DrawFlags(selectedProfile);
            DrawPreview(selectedProfile);
            DrawActions(selectedProfile);
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                _config = BuildPipelineConfigUtility.LoadConfig();
                EnsureSelectedProfile();
            }

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(_isBuilding);
            if (GUILayout.Button("Select Config", EditorStyles.toolbarButton, GUILayout.Width(100f)))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMissingConfigState()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "BuildPipelineConfig не найден. Создайте конфиг, затем отредактируйте профили и флаги.",
                MessageType.Warning);

            if (GUILayout.Button("Create Build Pipeline Config", GUILayout.Height(28f)))
            {
                _config = BuildPipelineConfigUtility.GetOrCreateConfigAsset();
                EnsureSelectedProfile();
            }
        }

        private void DrawValidation(BuildValidationReport validation)
        {
            if (validation == null)
            {
                return;
            }

            foreach (string warning in validation.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            foreach (string error in validation.Errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }

        private BuildProfileConfig DrawProfileSelector()
        {
            if (_config.Profiles.Count == 0)
            {
                EditorGUILayout.HelpBox("В конфиге нет профилей сборки.", MessageType.Error);
                return null;
            }

            List<BuildProfileConfig> profiles = _config.Profiles.ToList();
            string[] labels = profiles.Select(profile =>
                string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.Id : profile.DisplayName).ToArray();

            int selectedIndex = GetSelectedProfileIndex(profiles);
            int newIndex = EditorGUILayout.Popup("Build Profile", selectedIndex, labels);

            if (newIndex != selectedIndex)
            {
                _selectedProfileId = profiles[newIndex].Id;
                _config.LastSelectedProfileId = _selectedProfileId;
                BuildPipelineConfigUtility.SaveConfig(_config);
                EnsureFlagStateForProfile(profiles[newIndex]);
                GUI.FocusControl(null);
            }

            return profiles[newIndex];
        }

        private void DrawProfileInfo(BuildProfileConfig profile)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Id", profile.Id);
            EditorGUILayout.LabelField("Target", profile.Target.ToString());
            EditorGUILayout.LabelField("Version Mode", profile.VersionMode.ToString());
            EditorGUILayout.LabelField("Name Template", profile.NameTemplate);
            EditorGUILayout.Space(4f);
        }

        private void DrawFlags(BuildProfileConfig profile)
        {
            EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);

            if (profile.AvailableFlagIds.Count == 0)
            {
                EditorGUILayout.HelpBox("Для профиля не настроены дополнительные флаги.", MessageType.Info);
                return;
            }

            foreach (string flagId in profile.AvailableFlagIds)
            {
                BuildOptionFlagConfig flag = _config.GetFlagById(flagId);
                if (flag == null)
                {
                    EditorGUILayout.HelpBox($"Flag '{flagId}' не найден в конфиге.", MessageType.Error);
                    continue;
                }

                bool current = GetFlagValue(flag.Id, flag.DefaultEnabled);
                bool next = EditorGUILayout.ToggleLeft($"{flag.Label} ({flag.Id})", current);
                if (next != current)
                {
                    SetFlagValue(flag.Id, next);
                }

                if (!string.IsNullOrWhiteSpace(flag.Description))
                {
                    EditorGUILayout.LabelField(flag.Description, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(8f);
        }

        private void DrawPreview(BuildProfileConfig profile)
        {
            BuildPreviewData preview = BuildPipelineService.CreatePreview(
                _config,
                profile.Id,
                BuildFlagDictionary(),
                forceZip: false);

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Build Name", preview.BuildName);
            EditorGUILayout.LabelField("Version", $"{preview.VersionBefore} -> {preview.VersionAfter}");
            EditorGUILayout.LabelField("Output Folder", preview.BuildDirectoryPath);
            EditorGUILayout.LabelField("Zip", preview.ZipAfterBuild ? preview.ZipPath : "disabled");
            EditorGUILayout.LabelField("Increment Version", preview.IncrementVersion ? "Yes" : "No");
            EditorGUILayout.LabelField("Apply Defines", preview.ApplyDefines ? "Yes" : "No");
            EditorGUILayout.LabelField("Remove do-not-ship", preview.RemoveDoNotShip ? "Yes" : "No");
            EditorGUILayout.LabelField("Defines", string.IsNullOrWhiteSpace(preview.DefinesPreview) ? "-" : preview.DefinesPreview);
            EditorGUILayout.Space(12f);
        }

        private void DrawActions(BuildProfileConfig profile)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_isBuilding);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build", GUILayout.Height(30f)))
            {
                ExecuteBuild(profile, forceZip: false);
            }

            if (GUILayout.Button("Build + Force Zip", GUILayout.Height(30f)))
            {
                ExecuteBuild(profile, forceZip: true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Increment Version Only", GUILayout.Height(24f)))
            {
                IncrementVersion(profile);
            }

            if (GUILayout.Button("Apply Defines Only", GUILayout.Height(24f)))
            {
                ApplyDefines(profile);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Open Output Folder", GUILayout.Height(22f)))
            {
                EditorUtility.RevealInFinder(BuildPipelineConfigUtility.GetOutputRootPath(_config));
            }

            EditorGUI.EndDisabledGroup();
        }

        private void ExecuteBuild(BuildProfileConfig profile, bool forceZip)
        {
            _isBuilding = true;
            try
            {
                _config.LastSelectedProfileId = profile.Id;
                BuildPipelineConfigUtility.SaveConfig(_config);

                BuildExecutionResult result = BuildPipelineService.ExecuteBuild(
                    _config,
                    new BuildExecutionRequest
                    {
                        ProfileId = profile.Id,
                        FlagOverrides = BuildFlagDictionary(),
                        ForceZipAfterBuild = forceZip,
                    });

                EditorUtility.DisplayDialog(
                    "Build Pipeline",
                    result.Succeeded ? result.Message : $"Build failed:\n{result.Message}",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError("[Build Pipeline] " + exception);
                EditorUtility.DisplayDialog("Build Pipeline", $"Error:\n{exception.Message}", "OK");
            }
            finally
            {
                _isBuilding = false;
            }
        }

        private void IncrementVersion(BuildProfileConfig profile)
        {
            try
            {
                BuildExecutionResult result = BuildPipelineService.IncrementVersionOnly(_config, profile.Id);
                EditorUtility.DisplayDialog("Build Pipeline", result.Message, "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError("[Build Pipeline] " + exception);
                EditorUtility.DisplayDialog("Build Pipeline", $"Error:\n{exception.Message}", "OK");
            }
        }

        private void ApplyDefines(BuildProfileConfig profile)
        {
            try
            {
                BuildExecutionResult result = BuildPipelineService.ApplyDefinesOnly(_config, profile.Id, BuildFlagDictionary());
                EditorUtility.DisplayDialog("Build Pipeline", result.Message, "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError("[Build Pipeline] " + exception);
                EditorUtility.DisplayDialog("Build Pipeline", $"Error:\n{exception.Message}", "OK");
            }
        }

        private void EnsureSelectedProfile()
        {
            if (_config == null || _config.Profiles.Count == 0)
            {
                _selectedProfileId = string.Empty;
                return;
            }

            BuildProfileConfig selected = _config.GetProfileById(_selectedProfileId);
            if (selected != null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_config.LastSelectedProfileId))
            {
                selected = _config.GetProfileById(_config.LastSelectedProfileId);
                if (selected != null)
                {
                    _selectedProfileId = selected.Id;
                    return;
                }
            }

            _selectedProfileId = _config.Profiles[0].Id;
            _config.LastSelectedProfileId = _selectedProfileId;
            BuildPipelineConfigUtility.SaveConfig(_config);
        }

        private int GetSelectedProfileIndex(IReadOnlyList<BuildProfileConfig> profiles)
        {
            if (profiles == null || profiles.Count == 0)
            {
                return 0;
            }

            int index = profiles
                .Select((profile, position) => new { profile, position })
                .Where(x => string.Equals(x.profile.Id, _selectedProfileId, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.position)
                .DefaultIfEmpty(0)
                .First();

            _selectedProfileId = profiles[index].Id;
            return index;
        }

        private void EnsureFlagStateForProfile(BuildProfileConfig profile)
        {
            if (profile == null)
            {
                _flagState.Clear();
                return;
            }

            var allowedIds = new HashSet<string>(profile.AvailableFlagIds, StringComparer.OrdinalIgnoreCase);
            _flagState.RemoveAll(item => item == null || string.IsNullOrWhiteSpace(item.Id) || !allowedIds.Contains(item.Id));

            foreach (string flagId in profile.AvailableFlagIds)
            {
                BuildOptionFlagConfig flag = _config.GetFlagById(flagId);
                if (flag == null)
                {
                    continue;
                }

                if (_flagState.Any(item => string.Equals(item.Id, flag.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                _flagState.Add(new FlagStateItem
                {
                    Id = flag.Id,
                    Value = flag.DefaultEnabled,
                });
            }
        }

        private bool GetFlagValue(string flagId, bool defaultValue)
        {
            FlagStateItem item = _flagState.FirstOrDefault(state =>
                string.Equals(state.Id, flagId, StringComparison.OrdinalIgnoreCase));
            return item != null ? item.Value : defaultValue;
        }

        private void SetFlagValue(string flagId, bool value)
        {
            FlagStateItem item = _flagState.FirstOrDefault(state =>
                string.Equals(state.Id, flagId, StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                _flagState.Add(new FlagStateItem
                {
                    Id = flagId,
                    Value = value,
                });
                return;
            }

            item.Value = value;
        }

        private Dictionary<string, bool> BuildFlagDictionary()
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (FlagStateItem item in _flagState)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                result[item.Id.Trim()] = item.Value;
            }

            return result;
        }
    }
}



