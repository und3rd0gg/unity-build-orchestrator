using System;
using System.Collections.Generic;

namespace BuildOrchestrator.Editor
{
    public sealed class BuildExecutionRequest
    {
        public string ProfileId { get; set; } = string.Empty;
        public Dictionary<string, bool> FlagOverrides { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public bool ForceZipAfterBuild { get; set; }
    }

    public sealed class BuildExecutionResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public string BuildName { get; set; } = string.Empty;
        public string VersionBefore { get; set; } = string.Empty;
        public string VersionAfter { get; set; } = string.Empty;
        public string BuildDirectoryPath { get; set; } = string.Empty;
        public string ZipPath { get; set; } = string.Empty;
        public int RemovedDoNotShipDirectoryCount { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public sealed class BuildPreviewData
    {
        public string BuildName { get; set; } = string.Empty;
        public string VersionBefore { get; set; } = string.Empty;
        public string VersionAfter { get; set; } = string.Empty;
        public string BuildDirectoryPath { get; set; } = string.Empty;
        public string ZipPath { get; set; } = string.Empty;
        public bool IncrementVersion { get; set; }
        public bool ApplyDefines { get; set; }
        public bool ZipAfterBuild { get; set; }
        public bool RemoveDoNotShip { get; set; }
        public string DefinesPreview { get; set; } = string.Empty;
    }

    internal sealed class ResolvedBuildOptions
    {
        public BuildPipelineConfig Config { get; set; }
        public BuildProfileConfig Profile { get; set; }
        public Dictionary<string, bool> FlagStates { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool IncrementVersion { get; set; }
        public bool ApplyDefines { get; set; }
        public bool ZipAfterBuild { get; set; }
        public bool RemoveDoNotShipDirectories { get; set; }

        public string VersionBefore { get; set; } = string.Empty;
        public string VersionAfter { get; set; } = string.Empty;

        public string BuildName { get; set; } = string.Empty;
        public string OutputRootPath { get; set; } = string.Empty;
        public string BuildDirectoryPath { get; set; } = string.Empty;
        public string ZipPath { get; set; } = string.Empty;

        public List<string> EffectiveDefines { get; } = new();
    }
}



