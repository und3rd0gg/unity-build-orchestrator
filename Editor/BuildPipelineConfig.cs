using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildOrchestrator.Editor
{
    public enum VersionIncrementMode
    {
        None = 0,
        Patch = 1,
        Minor = 2,
        Major = 3,
    }

    public enum BuildActionStage
    {
        BeforeVersioning = 0,
        AfterVersioning = 1,
        BeforeDefines = 2,
        AfterDefines = 3,
        BeforeBuild = 4,
        AfterBuild = 5,
        BeforePackaging = 6,
        AfterPackaging = 7,
        OnSuccess = 8,
        OnFailure = 9,
    }

    [Serializable]
    public sealed class BuildActionBinding
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private string _actionId = string.Empty;
        [SerializeField] private BuildActionStage _stage = BuildActionStage.BeforeBuild;

        public bool Enabled => _enabled;
        public string ActionId => _actionId ?? string.Empty;
        public BuildActionStage Stage => _stage;

        public BuildActionBinding()
        {
        }

        public BuildActionBinding(string actionId, BuildActionStage stage, bool enabled = true)
        {
            _actionId = actionId ?? string.Empty;
            _stage = stage;
            _enabled = enabled;
        }
    }

    [Serializable]
    public sealed class BuildOptionFlagConfig
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _label = string.Empty;
        [SerializeField] private string _description = string.Empty;
        [SerializeField] private bool _defaultEnabled;
        [SerializeField] private List<string> _defineSymbolsWhenEnabled = new();

        [Header("Overrides")]
        [SerializeField] private bool _overrideIncrementVersion;
        [SerializeField] private bool _incrementVersionValue = true;
        [SerializeField] private bool _overrideApplyDefines;
        [SerializeField] private bool _applyDefinesValue = true;
        [SerializeField] private bool _overrideZipAfterBuild;
        [SerializeField] private bool _zipAfterBuildValue = true;
        [SerializeField] private bool _overrideRemoveDoNotShip;
        [SerializeField] private bool _removeDoNotShipValue = true;

        public string Id => (_id ?? string.Empty).Trim();
        public string Label => string.IsNullOrWhiteSpace(_label) ? Id : _label.Trim();
        public string Description => _description ?? string.Empty;
        public bool DefaultEnabled => _defaultEnabled;
        public IReadOnlyList<string> DefineSymbolsWhenEnabled => _defineSymbolsWhenEnabled;

        public bool OverrideIncrementVersion => _overrideIncrementVersion;
        public bool IncrementVersionValue => _incrementVersionValue;
        public bool OverrideApplyDefines => _overrideApplyDefines;
        public bool ApplyDefinesValue => _applyDefinesValue;
        public bool OverrideZipAfterBuild => _overrideZipAfterBuild;
        public bool ZipAfterBuildValue => _zipAfterBuildValue;
        public bool OverrideRemoveDoNotShip => _overrideRemoveDoNotShip;
        public bool RemoveDoNotShipValue => _removeDoNotShipValue;

        public BuildOptionFlagConfig()
        {
        }

        public BuildOptionFlagConfig(string id, string label, bool defaultEnabled)
        {
            _id = id ?? string.Empty;
            _label = label ?? string.Empty;
            _defaultEnabled = defaultEnabled;
        }

        public void SetDescription(string description)
        {
            _description = description ?? string.Empty;
        }

        public void SetDefineSymbolsWhenEnabled(IEnumerable<string> symbols)
        {
            _defineSymbolsWhenEnabled = symbols?
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList() ?? new List<string>();
        }

        public void SetIncrementVersionOverride(bool enabled, bool value)
        {
            _overrideIncrementVersion = enabled;
            _incrementVersionValue = value;
        }

        public void SetApplyDefinesOverride(bool enabled, bool value)
        {
            _overrideApplyDefines = enabled;
            _applyDefinesValue = value;
        }

        public void SetZipOverride(bool enabled, bool value)
        {
            _overrideZipAfterBuild = enabled;
            _zipAfterBuildValue = value;
        }

        public void SetRemoveDoNotShipOverride(bool enabled, bool value)
        {
            _overrideRemoveDoNotShip = enabled;
            _removeDoNotShipValue = value;
        }
    }

    [Serializable]
    public sealed class BuildProfileConfig
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private BuildTarget _target = BuildTarget.StandaloneWindows64;

        [Header("Build Name")]
        [SerializeField] private string _nameTemplate = "{product}_{profile}_{version}";

        [Header("Version")]
        [SerializeField] private bool _incrementVersion = true;
        [SerializeField] private VersionIncrementMode _versionMode = VersionIncrementMode.Patch;

        [Header("Defines")]
        [SerializeField] private bool _applyDefines = true;
        [SerializeField] private List<string> _defineSymbols = new();

        [Header("Packaging")]
        [SerializeField] private bool _zipAfterBuild = true;
        [SerializeField] private bool _removeDoNotShipDirectories = true;
        [SerializeField] private List<string> _doNotShipDirectoryNames = new() { "do not ship", "BurstDebugInformation_DoNotShip" };

        [Header("Build Options")]
        [SerializeField] private BuildOptions _unityBuildOptions = BuildOptions.None;

        [Header("UI Flags")]
        [SerializeField] private List<string> _availableFlagIds = new();

        [Header("Custom Actions")]
        [SerializeField] private List<BuildActionBinding> _actions = new();

        public string Id => (_id ?? string.Empty).Trim();
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? Id : _displayName.Trim();
        public BuildTarget Target => _target;
        public BuildTargetGroup TargetGroup => UnityEditor.BuildPipeline.GetBuildTargetGroup(_target);

        public string NameTemplate => _nameTemplate ?? string.Empty;

        public bool IncrementVersion => _incrementVersion;
        public VersionIncrementMode VersionMode => _versionMode;

        public bool ApplyDefines => _applyDefines;
        public IReadOnlyList<string> DefineSymbols => _defineSymbols;

        public bool ZipAfterBuild => _zipAfterBuild;
        public bool RemoveDoNotShipDirectories => _removeDoNotShipDirectories;
        public IReadOnlyList<string> DoNotShipDirectoryNames => _doNotShipDirectoryNames;

        public BuildOptions UnityBuildOptions => _unityBuildOptions;

        public IReadOnlyList<string> AvailableFlagIds => _availableFlagIds;
        public IReadOnlyList<BuildActionBinding> Actions => _actions;

        public BuildProfileConfig()
        {
        }

        public BuildProfileConfig(
            string id,
            string displayName,
            BuildTarget target,
            string nameTemplate,
            VersionIncrementMode versionMode,
            IEnumerable<string> defineSymbols)
        {
            _id = id ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _target = target;
            _nameTemplate = nameTemplate ?? "{product}_{profile}_{version}";
            _versionMode = versionMode;
            _defineSymbols = defineSymbols?.Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList() ?? new List<string>();
        }

        public void SetAvailableFlagIds(IEnumerable<string> flagIds)
        {
            _availableFlagIds = flagIds?
                .Where(flagId => !string.IsNullOrWhiteSpace(flagId))
                .Select(flagId => flagId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        public void SetBuildOptions(
            bool incrementVersion,
            VersionIncrementMode versionMode,
            bool applyDefines,
            bool zipAfterBuild,
            bool removeDoNotShip,
            BuildOptions unityBuildOptions)
        {
            _incrementVersion = incrementVersion;
            _versionMode = versionMode;
            _applyDefines = applyDefines;
            _zipAfterBuild = zipAfterBuild;
            _removeDoNotShipDirectories = removeDoNotShip;
            _unityBuildOptions = unityBuildOptions;
        }

        public void SetDoNotShipDirectoryNames(IEnumerable<string> names)
        {
            _doNotShipDirectoryNames = names?
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        public void SetActions(IEnumerable<BuildActionBinding> actions)
        {
            _actions = actions?.ToList() ?? new List<BuildActionBinding>();
        }
    }

    [CreateAssetMenu(
        fileName = "BuildPipelineConfig",
        menuName = "Build Pipeline/Build Pipeline Config")]
    public sealed class BuildPipelineConfig : ScriptableObject
    {
        [Header("General")]
        [SerializeField] private string _outputRootFolderName = "BUILD";

        [Header("Preprocess (manual Unity Build)")]
        [SerializeField] private bool _enablePreprocessVersioning = true;
        [SerializeField] private bool _enablePreprocessDefines = true;
        [SerializeField] private string _preprocessProfileId = string.Empty;

        [Header("Editor UI State")]
        [SerializeField] private string _lastSelectedProfileId = string.Empty;

        [Header("Profiles")]
        [SerializeField] private List<BuildProfileConfig> _profiles = new();

        [Header("Flags")]
        [SerializeField] private List<BuildOptionFlagConfig> _optionFlags = new();

        [Header("Global Custom Actions")]
        [SerializeField] private List<BuildActionBinding> _globalActions = new();

        public string OutputRootFolderName => string.IsNullOrWhiteSpace(_outputRootFolderName)
            ? "BUILD"
            : _outputRootFolderName.Trim();

        public bool EnablePreprocessVersioning => _enablePreprocessVersioning;
        public bool EnablePreprocessDefines => _enablePreprocessDefines;
        public string PreprocessProfileId => (_preprocessProfileId ?? string.Empty).Trim();

        public string LastSelectedProfileId
        {
            get => (_lastSelectedProfileId ?? string.Empty).Trim();
            set => _lastSelectedProfileId = value ?? string.Empty;
        }

        public IReadOnlyList<BuildProfileConfig> Profiles => _profiles;
        public IReadOnlyList<BuildOptionFlagConfig> OptionFlags => _optionFlags;
        public IReadOnlyList<BuildActionBinding> GlobalActions => _globalActions;

        public string PreprocessProfileIdValue
        {
            get => _preprocessProfileId ?? string.Empty;
            set => _preprocessProfileId = value ?? string.Empty;
        }

        public void SetProfiles(IEnumerable<BuildProfileConfig> profiles)
        {
            _profiles = profiles?.ToList() ?? new List<BuildProfileConfig>();
        }

        public void SetOptionFlags(IEnumerable<BuildOptionFlagConfig> flags)
        {
            _optionFlags = flags?.ToList() ?? new List<BuildOptionFlagConfig>();
        }

        public void SetGlobalActions(IEnumerable<BuildActionBinding> actions)
        {
            _globalActions = actions?.ToList() ?? new List<BuildActionBinding>();
        }

        public BuildProfileConfig GetProfileById(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return null;
            }

            return _profiles.FirstOrDefault(profile =>
                string.Equals(profile.Id, profileId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public BuildProfileConfig GetFirstProfileForTarget(BuildTarget target)
        {
            return _profiles.FirstOrDefault(profile => profile.Target == target);
        }

        public BuildOptionFlagConfig GetFlagById(string flagId)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return null;
            }

            return _optionFlags.FirstOrDefault(flag =>
                string.Equals(flag.Id, flagId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public HashSet<string> GetManagedDefineSymbols()
        {
            var managed = new HashSet<string>(StringComparer.Ordinal);

            foreach (BuildProfileConfig profile in _profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                foreach (string symbol in profile.DefineSymbols)
                {
                    AddSymbol(managed, symbol);
                }

                foreach (string flagId in profile.AvailableFlagIds)
                {
                    BuildOptionFlagConfig flag = GetFlagById(flagId);
                    if (flag == null)
                    {
                        continue;
                    }

                    foreach (string symbol in flag.DefineSymbolsWhenEnabled)
                    {
                        AddSymbol(managed, symbol);
                    }
                }
            }

            return managed;
        }

        private static void AddSymbol(HashSet<string> symbols, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return;
            }

            symbols.Add(symbol.Trim());
        }
    }
}



