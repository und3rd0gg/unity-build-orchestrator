using UnityEngine;

namespace BuildOrchestrator.Editor
{
    public sealed class BuildLogContextAction : IBuildAction
    {
        public string Id => "log-context";
        public string Description => "Логирует текущий этап, профиль, версию и путь вывода.";

        public void Execute(BuildActionContext context)
        {
            string message =
                $"[Build Pipeline][Action:{Id}] Stage={context.Stage}, " +
                $"Profile={context.Profile?.Id}, Version={context.VersionAfter}, " +
                $"BuildName={context.BuildName}, Output={context.BuildDirectoryPath}";

            Debug.Log(message);
            context.LogInfo?.Invoke(message);
        }
    }
}



