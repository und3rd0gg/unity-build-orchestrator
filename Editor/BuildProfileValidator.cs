using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildOrchestrator.Editor
{
    public sealed class BuildValidationReport
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();

        public bool IsValid => Errors.Count == 0;
    }

    public static class BuildProfileValidator
    {
        public static BuildValidationReport ValidateConfig(BuildPipelineConfig config)
        {
            var report = new BuildValidationReport();

            if (config == null)
            {
                report.Errors.Add("BuildPipelineConfig не найден.");
                return report;
            }

            if (config.Profiles == null || config.Profiles.Count == 0)
            {
                report.Errors.Add("В BuildPipelineConfig не добавлены профили сборки.");
                return report;
            }

            ValidateProfileIds(config, report);
            ValidateFlagIds(config, report);
            ValidateActions(config, report);

            return report;
        }

        public static BuildValidationReport ValidateProfile(BuildPipelineConfig config, BuildProfileConfig profile)
        {
            BuildValidationReport report = ValidateConfig(config);
            if (!report.IsValid)
            {
                return report;
            }

            if (profile == null)
            {
                report.Errors.Add("Профиль сборки не выбран.");
                return report;
            }

            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                report.Errors.Add("У профиля отсутствует Id.");
            }

            if (profile.TargetGroup == UnityEditor.BuildTargetGroup.Unknown)
            {
                report.Warnings.Add($"У профиля '{profile.DisplayName}' BuildTargetGroup = Unknown. Defines не будут применены.");
            }

            foreach (string flagId in profile.AvailableFlagIds)
            {
                if (config.GetFlagById(flagId) == null)
                {
                    report.Errors.Add($"Профиль '{profile.DisplayName}' ссылается на отсутствующий flag '{flagId}'.");
                }
            }

            return report;
        }

        private static void ValidateProfileIds(BuildPipelineConfig config, BuildValidationReport report)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (BuildProfileConfig profile in config.Profiles)
            {
                if (profile == null)
                {
                    report.Errors.Add("В списке Profiles найден null-элемент.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(profile.Id))
                {
                    report.Errors.Add($"Профиль '{profile.DisplayName}' не имеет Id.");
                    continue;
                }

                if (!ids.Add(profile.Id))
                {
                    report.Errors.Add($"Дублирующийся Id профиля: '{profile.Id}'.");
                }
            }
        }

        private static void ValidateFlagIds(BuildPipelineConfig config, BuildValidationReport report)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (BuildOptionFlagConfig flag in config.OptionFlags)
            {
                if (flag == null)
                {
                    report.Errors.Add("В списке OptionFlags найден null-элемент.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(flag.Id))
                {
                    report.Errors.Add("Один из OptionFlags не имеет Id.");
                    continue;
                }

                if (!ids.Add(flag.Id))
                {
                    report.Errors.Add($"Дублирующийся Id флага: '{flag.Id}'.");
                }
            }
        }

        private static void ValidateActions(BuildPipelineConfig config, BuildValidationReport report)
        {
            IReadOnlyDictionary<string, IBuildAction> actions = BuildActionRegistry.GetActions();

            ValidateActionBindings("GlobalActions", config.GlobalActions, actions, report);

            foreach (BuildProfileConfig profile in config.Profiles)
            {
                if (profile == null)
                {
                    continue;
                }

                ValidateActionBindings($"Profile '{profile.DisplayName}'", profile.Actions, actions, report);
            }
        }

        private static void ValidateActionBindings(
            string owner,
            IReadOnlyList<BuildActionBinding> bindings,
            IReadOnlyDictionary<string, IBuildAction> actions,
            BuildValidationReport report)
        {
            if (bindings == null)
            {
                return;
            }

            foreach (BuildActionBinding binding in bindings)
            {
                if (binding == null || !binding.Enabled)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(binding.ActionId))
                {
                    report.Errors.Add($"{owner}: найден BuildActionBinding без ActionId.");
                    continue;
                }

                if (!actions.ContainsKey(binding.ActionId.Trim()))
                {
                    report.Warnings.Add($"{owner}: action '{binding.ActionId}' не найден в реестре и будет пропущен.");
                }
            }
        }
    }
}



