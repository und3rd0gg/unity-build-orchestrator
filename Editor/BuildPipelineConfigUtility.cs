using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildOrchestrator.Editor
{
    public static class BuildPipelineConfigUtility
    {
        private const string DefaultConfigDirectoryPath = "Assets/BuildPipeline/Config";
        private const string DefaultConfigAssetPath = DefaultConfigDirectoryPath + "/BuildPipelineConfig.asset";

        [MenuItem("Tools/Build/Create Build Pipeline Config")]
        [MenuItem("File/Build Profiles/Create Build Pipeline Config", false, 2101)]
        public static void CreateConfigAssetMenu()
        {
            BuildPipelineConfig config = GetOrCreateConfigAsset();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        public static BuildPipelineConfig LoadConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:BuildPipelineConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<BuildPipelineConfig>(path);
                if (config != null)
                {
                    return config;
                }
            }

            return null;
        }

        public static BuildPipelineConfig GetOrCreateConfigAsset()
        {
            BuildPipelineConfig existing = LoadConfig();
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(DefaultConfigDirectoryPath);

            BuildPipelineConfig created = ScriptableObject.CreateInstance<BuildPipelineConfig>();
            created.SetProfiles(CreateDefaultProfiles());
            created.SetOptionFlags(CreateDefaultFlags());
            created.SetGlobalActions(new[]
            {
                new BuildActionBinding("log-context", BuildActionStage.BeforeBuild),
                new BuildActionBinding("log-context", BuildActionStage.AfterBuild),
            });
            created.PreprocessProfileIdValue = "dev";
            created.LastSelectedProfileId = "dev";

            AssetDatabase.CreateAsset(created, DefaultConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return created;
        }

        public static string GetProjectRootPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        public static string GetOutputRootPath(BuildPipelineConfig config)
        {
            string rootName = config != null ? config.OutputRootFolderName : "BUILD";
            return Path.Combine(GetProjectRootPath(), rootName);
        }

        public static BuildProfileConfig ResolvePreprocessProfile(BuildPipelineConfig config, BuildTarget target)
        {
            if (config == null || config.Profiles.Count == 0)
            {
                return null;
            }

            BuildProfileConfig profile = config.GetProfileById(config.PreprocessProfileId);
            if (profile != null)
            {
                return profile;
            }

            profile = config.GetProfileById(config.LastSelectedProfileId);
            if (profile != null && profile.Target == target)
            {
                return profile;
            }

            profile = config.GetFirstProfileForTarget(target);
            return profile ?? config.Profiles.FirstOrDefault();
        }

        public static void SaveConfig(BuildPipelineConfig config)
        {
            if (config == null)
            {
                return;
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static List<BuildProfileConfig> CreateDefaultProfiles()
        {
            var profiles = new List<BuildProfileConfig>();
            string[] commonFlags =
            {
                "demo-content",
                "skip-zip",
                "skip-version-bump",
            };

            BuildProfileConfig dev = new BuildProfileConfig(
                id: "dev",
                displayName: "Dev",
                target: BuildTarget.StandaloneWindows64,
                nameTemplate: "{product}_{version}",
                versionMode: VersionIncrementMode.Patch,
                defineSymbols: new[] { "TL_BUILD_DEV" });
            dev.SetBuildOptions(
                incrementVersion: true,
                versionMode: VersionIncrementMode.Patch,
                applyDefines: true,
                zipAfterBuild: true,
                removeDoNotShip: true,
                unityBuildOptions: BuildOptions.None);
            dev.SetAvailableFlagIds(commonFlags);
            dev.SetDoNotShipDirectoryNames(new[] { "do not ship", "BurstDebugInformation_DoNotShip" });
            profiles.Add(dev);

            BuildProfileConfig reviewDemo = new BuildProfileConfig(
                id: "review_demo",
                displayName: "Review + Demo",
                target: BuildTarget.StandaloneWindows64,
                nameTemplate: "{product}_demo_review",
                versionMode: VersionIncrementMode.Patch,
                defineSymbols: new[] { "TL_BUILD_REVIEW", "TL_BUILD_DEMO" });
            reviewDemo.SetBuildOptions(true, VersionIncrementMode.Patch, true, true, true, BuildOptions.None);
            reviewDemo.SetAvailableFlagIds(commonFlags);
            reviewDemo.SetDoNotShipDirectoryNames(new[] { "do not ship", "BurstDebugInformation_DoNotShip" });
            profiles.Add(reviewDemo);

            BuildProfileConfig releaseDemo = new BuildProfileConfig(
                id: "release_demo",
                displayName: "Release + Demo",
                target: BuildTarget.StandaloneWindows64,
                nameTemplate: "{product}_demo_release",
                versionMode: VersionIncrementMode.Patch,
                defineSymbols: new[] { "TL_BUILD_RELEASE", "TL_BUILD_DEMO" });
            releaseDemo.SetBuildOptions(true, VersionIncrementMode.Patch, true, true, true, BuildOptions.None);
            releaseDemo.SetAvailableFlagIds(commonFlags);
            releaseDemo.SetDoNotShipDirectoryNames(new[] { "do not ship", "BurstDebugInformation_DoNotShip" });
            profiles.Add(releaseDemo);

            BuildProfileConfig review = new BuildProfileConfig(
                id: "review",
                displayName: "Review",
                target: BuildTarget.StandaloneWindows64,
                nameTemplate: "{product}_review",
                versionMode: VersionIncrementMode.Patch,
                defineSymbols: new[] { "TL_BUILD_REVIEW" });
            review.SetBuildOptions(true, VersionIncrementMode.Patch, true, true, true, BuildOptions.None);
            review.SetAvailableFlagIds(commonFlags);
            review.SetDoNotShipDirectoryNames(new[] { "do not ship", "BurstDebugInformation_DoNotShip" });
            profiles.Add(review);

            BuildProfileConfig release = new BuildProfileConfig(
                id: "release",
                displayName: "Release",
                target: BuildTarget.StandaloneWindows64,
                nameTemplate: "{product}_release",
                versionMode: VersionIncrementMode.Patch,
                defineSymbols: new[] { "TL_BUILD_RELEASE" });
            release.SetBuildOptions(true, VersionIncrementMode.Patch, true, true, true, BuildOptions.None);
            release.SetAvailableFlagIds(commonFlags);
            release.SetDoNotShipDirectoryNames(new[] { "do not ship", "BurstDebugInformation_DoNotShip" });
            profiles.Add(release);

            return profiles;
        }

        private static List<BuildOptionFlagConfig> CreateDefaultFlags()
        {
            var flags = new List<BuildOptionFlagConfig>();

            var demoContent = new BuildOptionFlagConfig("demo-content", "Demo Content", false);
            demoContent.SetDescription("Добавляет define TL_BUILD_DEMO к выбранному профилю во время сборки.");
            demoContent.SetDefineSymbolsWhenEnabled(new[] { "TL_BUILD_DEMO" });
            flags.Add(demoContent);

            var skipZip = new BuildOptionFlagConfig("skip-zip", "Skip Zip", false);
            skipZip.SetDescription("Отключает упаковку в zip для конкретного запуска.");
            skipZip.SetZipOverride(true, false);
            flags.Add(skipZip);

            var skipVersion = new BuildOptionFlagConfig("skip-version-bump", "Skip Version Bump", false);
            skipVersion.SetDescription("Не увеличивать версию в этом запуске.");
            skipVersion.SetIncrementVersionOverride(true, false);
            flags.Add(skipVersion);

            return flags;
        }
    }
}



