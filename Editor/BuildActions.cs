using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace BuildOrchestrator.Editor
{
    public sealed class BuildActionContext
    {
        public BuildActionContext(
            BuildPipelineConfig config,
            BuildProfileConfig profile,
            IReadOnlyDictionary<string, bool> flags,
            Action<string> infoLogger,
            Action<string> errorLogger)
        {
            Config = config;
            Profile = profile;
            Flags = flags;
            LogInfo = infoLogger;
            LogError = errorLogger;
        }

        public BuildPipelineConfig Config { get; }
        public BuildProfileConfig Profile { get; }
        public IReadOnlyDictionary<string, bool> Flags { get; }

        public BuildActionStage Stage { get; internal set; }
        public string BuildName { get; internal set; } = string.Empty;
        public string BuildDirectoryPath { get; internal set; } = string.Empty;
        public string OutputRootPath { get; internal set; } = string.Empty;
        public string ZipPath { get; internal set; } = string.Empty;
        public string VersionBefore { get; internal set; } = string.Empty;
        public string VersionAfter { get; internal set; } = string.Empty;
        public BuildReport BuildReport { get; internal set; }

        public Action<string> LogInfo { get; }
        public Action<string> LogError { get; }
    }

    public interface IBuildAction
    {
        string Id { get; }
        string Description { get; }
        void Execute(BuildActionContext context);
    }

    public static class BuildActionRegistry
    {
        private static Dictionary<string, IBuildAction> _cachedActions;

        public static IReadOnlyDictionary<string, IBuildAction> GetActions(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedActions != null)
            {
                return _cachedActions;
            }

            _cachedActions = new Dictionary<string, IBuildAction>(StringComparer.OrdinalIgnoreCase);

            foreach (Type type in TypeCache.GetTypesDerivedFrom<IBuildAction>())
            {
                if (type == null || type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                try
                {
                    object instance = Activator.CreateInstance(type);
                    if (!(instance is IBuildAction action))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(action.Id))
                    {
                        continue;
                    }

                    _cachedActions[action.Id.Trim()] = action;
                }
                catch
                {
                    // Игнорируем некорректные action-классы, чтобы не падать на открытии окна.
                }
            }

            return _cachedActions;
        }
    }
}



