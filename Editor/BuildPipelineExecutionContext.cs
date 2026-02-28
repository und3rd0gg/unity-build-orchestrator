using System;

namespace BuildOrchestrator.Editor
{
    internal static class BuildPipelineExecutionContext
    {
        private static int _scopeDepth;

        public static bool IsInternalBuild => _scopeDepth > 0;

        public static IDisposable BeginScope()
        {
            _scopeDepth++;
            return new ScopeHandle();
        }

        private sealed class ScopeHandle : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _scopeDepth = _scopeDepth > 0 ? _scopeDepth - 1 : 0;
            }
        }
    }
}



