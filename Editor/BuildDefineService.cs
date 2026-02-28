using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BuildOrchestrator.Editor
{
    public static class BuildDefineService
    {
        public static HashSet<string> ParseSymbols(string symbols)
        {
            return new HashSet<string>(
                (symbols ?? string.Empty)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(symbol => symbol.Trim())
                    .Where(symbol => !string.IsNullOrWhiteSpace(symbol)),
                StringComparer.Ordinal);
        }

        public static string GetSymbolsRaw(BuildTargetGroup targetGroup)
        {
            if (targetGroup == BuildTargetGroup.Unknown)
            {
                return string.Empty;
            }

#pragma warning disable CS0618
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#pragma warning restore CS0618
        }

        public static void SetSymbols(BuildTargetGroup targetGroup, IEnumerable<string> symbols)
        {
            if (targetGroup == BuildTargetGroup.Unknown)
            {
                return;
            }

            string value = string.Join(";", symbols
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Select(symbol => symbol.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(symbol => symbol, StringComparer.Ordinal));

#pragma warning disable CS0618
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, value);
#pragma warning restore CS0618
        }

        public static HashSet<string> ApplyManagedDefines(
            BuildTargetGroup targetGroup,
            IEnumerable<string> managedSymbols,
            IEnumerable<string> targetSymbols)
        {
            HashSet<string> current = ParseSymbols(GetSymbolsRaw(targetGroup));

            foreach (string managed in managedSymbols ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(managed))
                {
                    continue;
                }

                current.Remove(managed.Trim());
            }

            foreach (string target in targetSymbols ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                current.Add(target.Trim());
            }

            SetSymbols(targetGroup, current);
            return current;
        }
    }
}



