using System;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildOrchestrator.Editor
{
    public static class BuildPipelineService
    {
        public static BuildExecutionResult ExecuteBuild(BuildPipelineConfig config, BuildExecutionRequest request)
        {
            BuildProfileConfig profile = config?.GetProfileById(request?.ProfileId);
            if (profile == null)
            {
                throw new InvalidOperationException("Не удалось определить профиль сборки.");
            }

            BuildValidationReport validation = BuildProfileValidator.ValidateProfile(config, profile);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(string.Join("\n", validation.Errors));
            }

            ResolvedBuildOptions resolved = ResolveOptions(config, profile, request?.FlagOverrides, request?.ForceZipAfterBuild ?? false);
            BuildExecutionResult result = new()
            {
                BuildName = resolved.BuildName,
                BuildDirectoryPath = resolved.BuildDirectoryPath,
                ZipPath = resolved.ZipPath,
                VersionBefore = resolved.VersionBefore,
                VersionAfter = resolved.VersionAfter,
            };

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes found in Build Settings.");
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            BuildReport buildReport = null;
            int removedCount = 0;
            IReadOnlyDictionary<string, IBuildAction> actions = BuildActionRegistry.GetActions();
            var actionContext = CreateActionContext(resolved);

            try
            {
                if (resolved.IncrementVersion)
                {
                    ExecuteActions(resolved, BuildActionStage.BeforeVersioning, actionContext, actions);
                    string nextVersion = BuildVersionService.GetNextVersion(resolved.VersionBefore, profile.VersionMode);
                    resolved.VersionAfter = BuildVersionService.ApplyVersion(nextVersion);
                    actionContext.VersionAfter = resolved.VersionAfter;
                    ExecuteActions(resolved, BuildActionStage.AfterVersioning, actionContext, actions);
                }

                if (resolved.ApplyDefines)
                {
                    ExecuteActions(resolved, BuildActionStage.BeforeDefines, actionContext, actions);
                    HashSet<string> applied = BuildDefineService.ApplyManagedDefines(
                        profile.TargetGroup,
                        config.GetManagedDefineSymbols(),
                        resolved.EffectiveDefines);
                    resolved.EffectiveDefines.Clear();
                    resolved.EffectiveDefines.AddRange(applied.OrderBy(symbol => symbol, StringComparer.Ordinal));
                    ExecuteActions(resolved, BuildActionStage.AfterDefines, actionContext, actions);
                }

                ExecuteActions(resolved, BuildActionStage.BeforeBuild, actionContext, actions);

                Directory.CreateDirectory(resolved.OutputRootPath);
                BuildPackagingService.EnsureBuildDirectory(resolved.BuildDirectoryPath);
                if (resolved.ZipAfterBuild)
                {
                    BuildPackagingService.DeleteZipIfExists(resolved.ZipPath);
                }

                BuildPlayerOptions options = new()
                {
                    scenes = scenes,
                    locationPathName = GetLocationPathName(profile.Target, resolved.BuildDirectoryPath),
                    target = profile.Target,
                    options = profile.UnityBuildOptions,
                };

                using (BuildPipelineExecutionContext.BeginScope())
                {
                    buildReport = UnityEditor.BuildPipeline.BuildPlayer(options);
                }

                actionContext.BuildReport = buildReport;
                ExecuteActions(resolved, BuildActionStage.AfterBuild, actionContext, actions);

                if (buildReport == null || buildReport.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Build failed: " +
                        $"{buildReport?.summary.result}. " +
                        $"Errors: {buildReport?.summary.totalErrors}, " +
                        $"Warnings: {buildReport?.summary.totalWarnings}");
                }

                ExecuteActions(resolved, BuildActionStage.BeforePackaging, actionContext, actions);

                if (resolved.RemoveDoNotShipDirectories)
                {
                    removedCount = BuildPackagingService.RemoveDirectoriesByName(
                        resolved.BuildDirectoryPath,
                        profile.DoNotShipDirectoryNames);
                }

                if (resolved.ZipAfterBuild)
                {
                    removedCount += BuildPackagingService.RemoveBurstDebugInformationDirectories(
                        resolved.BuildDirectoryPath);
                    BuildPackagingService.CreateZip(resolved.BuildDirectoryPath, resolved.ZipPath);
                }

                ExecuteActions(resolved, BuildActionStage.AfterPackaging, actionContext, actions);
                ExecuteActions(resolved, BuildActionStage.OnSuccess, actionContext, actions);

                stopwatch.Stop();

                result.Succeeded = true;
                result.VersionAfter = resolved.VersionAfter;
                result.RemovedDoNotShipDirectoryCount = removedCount;
                result.Duration = stopwatch.Elapsed;
                result.Message =
                    "Build completed.\n\n" +
                    $"Profile: {profile.DisplayName}\n" +
                    $"Build Name: {resolved.BuildName}\n" +
                    $"Build Folder: {resolved.BuildDirectoryPath}\n" +
                    $"Zip: {(resolved.ZipAfterBuild ? resolved.ZipPath : "disabled")}\n" +
                    $"Version: {resolved.VersionBefore} -> {resolved.VersionAfter}\n" +
                    $"Removed do-not-ship dirs: {removedCount}\n" +
                    $"Duration: {stopwatch.Elapsed:mm\\:ss}";

                Debug.Log("[Build Pipeline] " + result.Message.Replace("\n", " | "));
                return result;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                actionContext.BuildReport = buildReport;

                try
                {
                    ExecuteActions(resolved, BuildActionStage.OnFailure, actionContext, actions);
                }
                catch (Exception actionException)
                {
                    Debug.LogError($"[Build Pipeline] Failure action error: {actionException}");
                }

                result.Succeeded = false;
                result.Duration = stopwatch.Elapsed;
                result.Message = exception.Message;
                Debug.LogError("[Build Pipeline] " + exception);
                return result;
            }
        }

        public static BuildExecutionResult IncrementVersionOnly(BuildPipelineConfig config, string profileId)
        {
            BuildProfileConfig profile = config?.GetProfileById(profileId);
            if (profile == null)
            {
                throw new InvalidOperationException("Профиль не найден для increment version.");
            }

            string before = BuildVersionService.GetCurrentVersion();
            string after = BuildVersionService.ApplyVersion(BuildVersionService.GetNextVersion(before, profile.VersionMode));

            return new BuildExecutionResult
            {
                Succeeded = true,
                VersionBefore = before,
                VersionAfter = after,
                Message = $"Version updated: {before} -> {after}",
            };
        }

        public static BuildExecutionResult ApplyDefinesOnly(BuildPipelineConfig config, string profileId, IDictionary<string, bool> flagOverrides)
        {
            BuildProfileConfig profile = config?.GetProfileById(profileId);
            if (profile == null)
            {
                throw new InvalidOperationException("Профиль не найден для apply defines.");
            }

            ResolvedBuildOptions resolved = ResolveOptions(config, profile, flagOverrides, false);
            HashSet<string> applied = BuildDefineService.ApplyManagedDefines(
                profile.TargetGroup,
                config.GetManagedDefineSymbols(),
                resolved.EffectiveDefines);

            string value = string.Join(";", applied.OrderBy(symbol => symbol, StringComparer.Ordinal));

            return new BuildExecutionResult
            {
                Succeeded = true,
                Message = $"Defines applied for {profile.DisplayName}: {value}",
            };
        }

        public static BuildPreviewData CreatePreview(
            BuildPipelineConfig config,
            string profileId,
            IDictionary<string, bool> flagOverrides,
            bool forceZip)
        {
            BuildProfileConfig profile = config?.GetProfileById(profileId);
            if (profile == null)
            {
                return new BuildPreviewData();
            }

            ResolvedBuildOptions resolved = ResolveOptions(config, profile, flagOverrides, forceZip);

            return new BuildPreviewData
            {
                BuildName = resolved.BuildName,
                VersionBefore = resolved.VersionBefore,
                VersionAfter = resolved.VersionAfter,
                BuildDirectoryPath = resolved.BuildDirectoryPath,
                ZipPath = resolved.ZipPath,
                IncrementVersion = resolved.IncrementVersion,
                ApplyDefines = resolved.ApplyDefines,
                ZipAfterBuild = resolved.ZipAfterBuild,
                RemoveDoNotShip = resolved.RemoveDoNotShipDirectories,
                DefinesPreview = string.Join(";", resolved.EffectiveDefines),
            };
        }

        public static Dictionary<string, bool> CreateDefaultFlagState(BuildPipelineConfig config, BuildProfileConfig profile)
        {
            var state = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (config == null || profile == null)
            {
                return state;
            }

            foreach (string flagId in profile.AvailableFlagIds)
            {
                BuildOptionFlagConfig flag = config.GetFlagById(flagId);
                if (flag == null)
                {
                    continue;
                }

                state[flag.Id] = flag.DefaultEnabled;
            }

            return state;
        }

        internal static ResolvedBuildOptions ResolveOptionsForPreprocess(BuildPipelineConfig config, BuildProfileConfig profile)
        {
            return ResolveOptions(config, profile, null, false);
        }

        internal static ResolvedBuildOptions ResolveOptions(
            BuildPipelineConfig config,
            BuildProfileConfig profile,
            IDictionary<string, bool> flagOverrides,
            bool forceZipAfterBuild)
        {
            if (config == null)
            {
                throw new InvalidOperationException("BuildPipelineConfig is null.");
            }

            if (profile == null)
            {
                throw new InvalidOperationException("Build profile is null.");
            }

            var resolved = new ResolvedBuildOptions
            {
                Config = config,
                Profile = profile,
                IncrementVersion = profile.IncrementVersion,
                ApplyDefines = profile.ApplyDefines,
                ZipAfterBuild = profile.ZipAfterBuild,
                RemoveDoNotShipDirectories = profile.RemoveDoNotShipDirectories,
            };

            Dictionary<string, bool> defaults = CreateDefaultFlagState(config, profile);
            foreach (KeyValuePair<string, bool> pair in defaults)
            {
                resolved.FlagStates[pair.Key] = pair.Value;
            }

            if (flagOverrides != null)
            {
                foreach (KeyValuePair<string, bool> pair in flagOverrides)
                {
                    if (!resolved.FlagStates.ContainsKey(pair.Key))
                    {
                        continue;
                    }

                    resolved.FlagStates[pair.Key] = pair.Value;
                }
            }

            resolved.EffectiveDefines.AddRange(profile.DefineSymbols.Where(symbol => !string.IsNullOrWhiteSpace(symbol)).Select(symbol => symbol.Trim()));

            foreach (KeyValuePair<string, bool> pair in resolved.FlagStates)
            {
                if (!pair.Value)
                {
                    continue;
                }

                BuildOptionFlagConfig flag = config.GetFlagById(pair.Key);
                if (flag == null)
                {
                    continue;
                }

                if (flag.OverrideIncrementVersion)
                {
                    resolved.IncrementVersion = flag.IncrementVersionValue;
                }

                if (flag.OverrideApplyDefines)
                {
                    resolved.ApplyDefines = flag.ApplyDefinesValue;
                }

                if (flag.OverrideZipAfterBuild)
                {
                    resolved.ZipAfterBuild = flag.ZipAfterBuildValue;
                }

                if (flag.OverrideRemoveDoNotShip)
                {
                    resolved.RemoveDoNotShipDirectories = flag.RemoveDoNotShipValue;
                }

                foreach (string symbol in flag.DefineSymbolsWhenEnabled)
                {
                    if (string.IsNullOrWhiteSpace(symbol))
                    {
                        continue;
                    }

                    resolved.EffectiveDefines.Add(symbol.Trim());
                }
            }

            if (forceZipAfterBuild)
            {
                resolved.ZipAfterBuild = true;
            }

            resolved.VersionBefore = BuildVersionService.GetCurrentVersion();
            resolved.VersionAfter = resolved.IncrementVersion
                ? BuildVersionService.GetNextVersion(resolved.VersionBefore, profile.VersionMode)
                : resolved.VersionBefore;

            string productName = string.IsNullOrWhiteSpace(PlayerSettings.productName)
                ? "Game"
                : PlayerSettings.productName.Trim();

            resolved.BuildName = BuildNamingService.BuildName(
                profile,
                productName,
                resolved.VersionAfter,
                resolved.FlagStates,
                DateTime.Now);

            resolved.OutputRootPath = BuildPipelineConfigUtility.GetOutputRootPath(config);
            resolved.BuildDirectoryPath = Path.Combine(resolved.OutputRootPath, resolved.BuildName);
            resolved.ZipPath = Path.Combine(resolved.OutputRootPath, resolved.BuildName + ".zip");

            List<string> cleanedDefines = resolved.EffectiveDefines
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(symbol => symbol, StringComparer.Ordinal)
                .ToList();

            resolved.EffectiveDefines.Clear();
            resolved.EffectiveDefines.AddRange(cleanedDefines);

            if (profile.TargetGroup == BuildTargetGroup.Unknown)
            {
                resolved.ApplyDefines = false;
            }

            return resolved;
        }

        private static BuildActionContext CreateActionContext(ResolvedBuildOptions resolved)
        {
            return new BuildActionContext(
                resolved.Config,
                resolved.Profile,
                resolved.FlagStates,
                infoLogger: message => Debug.Log("[Build Pipeline] " + message),
                errorLogger: message => Debug.LogError("[Build Pipeline] " + message))
            {
                BuildName = resolved.BuildName,
                BuildDirectoryPath = resolved.BuildDirectoryPath,
                OutputRootPath = resolved.OutputRootPath,
                ZipPath = resolved.ZipPath,
                VersionBefore = resolved.VersionBefore,
                VersionAfter = resolved.VersionAfter,
            };
        }

        private static void ExecuteActions(
            ResolvedBuildOptions resolved,
            BuildActionStage stage,
            BuildActionContext context,
            IReadOnlyDictionary<string, IBuildAction> actions)
        {
            if (resolved == null || context == null || actions == null)
            {
                return;
            }

            context.Stage = stage;

            ExecuteActionList(resolved.Config.GlobalActions, stage, context, actions);
            ExecuteActionList(resolved.Profile.Actions, stage, context, actions);
        }

        private static void ExecuteActionList(
            IReadOnlyList<BuildActionBinding> bindings,
            BuildActionStage stage,
            BuildActionContext context,
            IReadOnlyDictionary<string, IBuildAction> actions)
        {
            if (bindings == null)
            {
                return;
            }

            foreach (BuildActionBinding binding in bindings)
            {
                if (binding == null || !binding.Enabled || binding.Stage != stage)
                {
                    continue;
                }

                string actionId = binding.ActionId?.Trim();
                if (string.IsNullOrWhiteSpace(actionId))
                {
                    continue;
                }

                if (!actions.TryGetValue(actionId, out IBuildAction action))
                {
                    Debug.LogWarning($"[Build Pipeline] Action '{actionId}' not found. Skipped.");
                    continue;
                }

                action.Execute(context);
            }
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        private static string GetLocationPathName(BuildTarget target, string buildDirectoryPath)
        {
            string productName = string.IsNullOrWhiteSpace(PlayerSettings.productName)
                ? "Game"
                : PlayerSettings.productName.Trim();

            string executable = BuildNamingService.GetExecutableFileName(target, productName);

            return target switch
            {
                BuildTarget.iOS => buildDirectoryPath,
                BuildTarget.WebGL => buildDirectoryPath,
                _ => Path.Combine(buildDirectoryPath, executable),
            };
        }
    }
}



