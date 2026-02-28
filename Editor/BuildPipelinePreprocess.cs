using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BuildOrchestrator.Editor
{
    /// <summary>
    /// Автоматические шаги для билда, запущенного не через Build Pipeline Manager.
    /// </summary>
    public sealed class BuildPipelinePreprocess : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (BuildPipelineExecutionContext.IsInternalBuild)
            {
                return;
            }

            BuildPipelineConfig config = BuildPipelineConfigUtility.LoadConfig();
            if (config == null)
            {
                return;
            }

            BuildProfileConfig profile = BuildPipelineConfigUtility.ResolvePreprocessProfile(config, report.summary.platform);
            if (profile == null)
            {
                return;
            }

            ResolvedBuildOptions resolved = BuildPipelineService.ResolveOptionsForPreprocess(config, profile);
            string before = BuildVersionService.GetCurrentVersion();
            string after = before;

            if (config.EnablePreprocessVersioning && resolved.IncrementVersion)
            {
                after = BuildVersionService.ApplyVersion(
                    BuildVersionService.GetNextVersion(before, profile.VersionMode));
                Debug.Log($"[Build Pipeline][Preprocess] Version updated: {before} -> {after} (profile: {profile.Id})");
            }

            if (config.EnablePreprocessDefines && resolved.ApplyDefines)
            {
                BuildDefineService.ApplyManagedDefines(
                    profile.TargetGroup,
                    config.GetManagedDefineSymbols(),
                    resolved.EffectiveDefines);
                Debug.Log($"[Build Pipeline][Preprocess] Defines applied for profile '{profile.Id}'.");
            }
        }
    }
}



