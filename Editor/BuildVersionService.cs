using System;
using UnityEditor;
using UnityEngine;

namespace BuildOrchestrator.Editor
{
    public static class BuildVersionService
    {
        private const string DefaultVersion = "0.0.0";

        public static string GetCurrentVersion()
        {
            string currentVersion = PlayerSettings.bundleVersion;
            return string.IsNullOrWhiteSpace(currentVersion) ? DefaultVersion : currentVersion.Trim();
        }

        public static string GetNextVersion(string currentVersion, VersionIncrementMode mode)
        {
            VersionParts parts = Parse(currentVersion);

            switch (mode)
            {
                case VersionIncrementMode.None:
                    break;
                case VersionIncrementMode.Patch:
                    parts.Patch += 1;
                    break;
                case VersionIncrementMode.Minor:
                    parts.Minor += 1;
                    parts.Patch = 0;
                    break;
                case VersionIncrementMode.Major:
                    parts.Major += 1;
                    parts.Minor = 0;
                    parts.Patch = 0;
                    break;
                default:
                    parts.Patch += 1;
                    break;
            }

            return parts.ToVersionString();
        }

        public static string ApplyVersion(string version)
        {
            string normalized = Parse(version).ToVersionString();
            PlayerSettings.bundleVersion = normalized;

            VersionParts parts = Parse(normalized);
            int androidVersionCode = Mathf.Max(1, (parts.Major * 10000) + (parts.Minor * 100) + parts.Patch);
            PlayerSettings.Android.bundleVersionCode = androidVersionCode;
            PlayerSettings.iOS.buildNumber = normalized;

            return normalized;
        }

        private static VersionParts Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new VersionParts(0, 0, 0);
            }

            string[] parts = input.Trim().Split('.');
            int major = TryParsePart(parts, 0);
            int minor = TryParsePart(parts, 1);
            int patch = TryParsePart(parts, 2);

            return new VersionParts(
                Mathf.Max(0, major),
                Mathf.Max(0, minor),
                Mathf.Max(0, patch));
        }

        private static int TryParsePart(string[] parts, int index)
        {
            if (parts == null || index < 0 || index >= parts.Length)
            {
                return 0;
            }

            return int.TryParse(parts[index], out int value) ? value : 0;
        }

        private struct VersionParts
        {
            public VersionParts(int major, int minor, int patch)
            {
                Major = major;
                Minor = minor;
                Patch = patch;
            }

            public int Major { get; set; }
            public int Minor { get; set; }
            public int Patch { get; set; }

            public string ToVersionString()
            {
                return $"{Major}.{Minor}.{Patch}";
            }
        }
    }
}



