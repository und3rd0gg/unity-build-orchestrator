using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace BuildOrchestrator.Editor
{
    public static class BuildNamingService
    {
        private static readonly Regex TokenRegex = new Regex(@"\{([^{}]+)\}", RegexOptions.Compiled);

        public static string BuildName(
            BuildProfileConfig profile,
            string productName,
            string version,
            IReadOnlyDictionary<string, bool> flags,
            DateTime now)
        {
            if (profile == null)
            {
                return MakeSafeFileName(productName);
            }

            string template = string.IsNullOrWhiteSpace(profile.NameTemplate)
                ? "{product}_{profile}_{version}"
                : profile.NameTemplate;

            string resolved = TokenRegex.Replace(template, match => ResolveToken(match.Groups[1].Value, profile, productName, version, flags, now));
            return MakeSafeFileName(resolved);
        }

        public static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "build";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);

            foreach (char symbol in value.Trim())
            {
                builder.Append(Array.IndexOf(invalid, symbol) >= 0 ? '_' : symbol);
            }

            string result = builder.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "build" : result;
        }

        public static string GetExecutableFileName(BuildTarget target, string productName)
        {
            string safeProductName = MakeSafeFileName(productName);

            return target switch
            {
                BuildTarget.StandaloneWindows => safeProductName + ".exe",
                BuildTarget.StandaloneWindows64 => safeProductName + ".exe",
                BuildTarget.StandaloneOSX => safeProductName + ".app",
                BuildTarget.Android => safeProductName + ".apk",
                _ => safeProductName,
            };
        }

        private static string ResolveToken(
            string token,
            BuildProfileConfig profile,
            string productName,
            string version,
            IReadOnlyDictionary<string, bool> flags,
            DateTime now)
        {
            string normalizedToken = (token ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalizedToken))
            {
                return string.Empty;
            }

            if (normalizedToken.StartsWith("flag:", StringComparison.OrdinalIgnoreCase))
            {
                string flagId = normalizedToken.Substring("flag:".Length).Trim();
                bool enabled = flags != null && flags.TryGetValue(flagId, out bool value) && value;
                return enabled ? "true" : "false";
            }

            if (normalizedToken.StartsWith("date", StringComparison.OrdinalIgnoreCase))
            {
                string format = ExtractFormat(normalizedToken, "date", "yyyyMMdd");
                return now.ToString(format);
            }

            if (normalizedToken.StartsWith("time", StringComparison.OrdinalIgnoreCase))
            {
                string format = ExtractFormat(normalizedToken, "time", "HHmmss");
                return now.ToString(format);
            }

            if (normalizedToken.StartsWith("datetime", StringComparison.OrdinalIgnoreCase))
            {
                string format = ExtractFormat(normalizedToken, "datetime", "yyyyMMdd_HHmmss");
                return now.ToString(format);
            }

            return normalizedToken.ToLowerInvariant() switch
            {
                "product" => string.IsNullOrWhiteSpace(productName) ? "Game" : productName.Trim(),
                "version" => string.IsNullOrWhiteSpace(version) ? "0.0.0" : version.Trim(),
                "profile" => profile.DisplayName,
                "profileid" => profile.Id,
                "platform" => profile.Target.ToString(),
                "target" => profile.Target.ToString(),
                "flags" => ResolveFlagsToken(flags),
                _ => string.Empty,
            };
        }

        private static string ResolveFlagsToken(IReadOnlyDictionary<string, bool> flags)
        {
            if (flags == null || flags.Count == 0)
            {
                return string.Empty;
            }

            string[] enabled = flags
                .Where(pair => pair.Value)
                .Select(pair => MakeSafeFileName(pair.Key))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return enabled.Length == 0 ? "none" : string.Join("-", enabled);
        }

        private static string ExtractFormat(string token, string prefix, string defaultFormat)
        {
            if (!token.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase))
            {
                return defaultFormat;
            }

            string format = token.Substring(prefix.Length + 1).Trim();
            return string.IsNullOrWhiteSpace(format) ? defaultFormat : format;
        }
    }
}



