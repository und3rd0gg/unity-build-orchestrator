using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BuildOrchestrator.Editor
{
    public static class BuildPackagingService
    {
        private const string BurstDoNotShipSuffix = "BurstDebugInformation_DoNotShip";

        public static void EnsureBuildDirectory(string buildDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(buildDirectoryPath))
            {
                throw new ArgumentException("Build directory path is empty.");
            }

            DeleteDirectoryIfExists(buildDirectoryPath);
            Directory.CreateDirectory(buildDirectoryPath);
        }

        public static void DeleteZipIfExists(string zipPath)
        {
            if (string.IsNullOrWhiteSpace(zipPath))
            {
                return;
            }

            if (!File.Exists(zipPath))
            {
                return;
            }

            File.SetAttributes(zipPath, FileAttributes.Normal);
            File.Delete(zipPath);
        }

        public static int RemoveDirectoriesByName(string rootDirectoryPath, IEnumerable<string> targetNames)
        {
            if (!Directory.Exists(rootDirectoryPath) || targetNames == null)
            {
                return 0;
            }

            var names = new HashSet<string>(
                targetNames.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()),
                StringComparer.OrdinalIgnoreCase);

            if (names.Count == 0)
            {
                return 0;
            }

            List<string> matches = Directory
                .EnumerateDirectories(rootDirectoryPath, "*", SearchOption.AllDirectories)
                .Where(path => MatchesNameOrSuffix(Path.GetFileName(path), names))
                .OrderByDescending(path => path.Length)
                .ToList();

            foreach (string path in matches)
            {
                NormalizeAttributes(path);
                Directory.Delete(path, true);
            }

            return matches.Count;
        }

        public static int RemoveBurstDebugInformationDirectories(string rootDirectoryPath)
        {
            if (!Directory.Exists(rootDirectoryPath))
            {
                return 0;
            }

            List<string> matches = Directory
                .EnumerateDirectories(rootDirectoryPath, "*", SearchOption.AllDirectories)
                .Where(path => Path.GetFileName(path)
                    .EndsWith(BurstDoNotShipSuffix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(path => path.Length)
                .ToList();

            foreach (string path in matches)
            {
                NormalizeAttributes(path);
                Directory.Delete(path, true);
            }

            return matches.Count;
        }

        public static void CreateZip(string sourceDirectoryPath, string zipPath)
        {
            if (!Directory.Exists(sourceDirectoryPath))
            {
                throw new DirectoryNotFoundException($"Build directory not found: {sourceDirectoryPath}");
            }

            string zipDirectory = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrWhiteSpace(zipDirectory))
            {
                Directory.CreateDirectory(zipDirectory);
            }

            DeleteZipIfExists(zipPath);

            ZipFile.CreateFromDirectory(
                sourceDirectoryPath,
                zipPath,
                CompressionLevel.Optimal,
                includeBaseDirectory: false);
        }

        private static void DeleteDirectoryIfExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            NormalizeAttributes(directoryPath);
            Directory.Delete(directoryPath, true);
        }

        private static void NormalizeAttributes(string rootDirectoryPath)
        {
            if (!Directory.Exists(rootDirectoryPath))
            {
                return;
            }

            foreach (string filePath in Directory.GetFiles(rootDirectoryPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            foreach (string directoryPath in Directory.GetDirectories(rootDirectoryPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(directoryPath, FileAttributes.Normal);
            }

            File.SetAttributes(rootDirectoryPath, FileAttributes.Normal);
        }

        private static bool MatchesNameOrSuffix(string directoryName, HashSet<string> targetNames)
        {
            if (string.IsNullOrWhiteSpace(directoryName) || targetNames == null || targetNames.Count == 0)
            {
                return false;
            }

            if (targetNames.Contains(directoryName))
            {
                return true;
            }

            foreach (string target in targetNames)
            {
                if (directoryName.EndsWith(target, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}



