using System.Collections.Generic;
using _Project.Core.Configs;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace _Project.Editor.Configs
{
    public class GameConfigBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            GameConfigCheck check = new GameConfigCheck();
            IReadOnlyList<ConfigError> errors = check.Run();

            check.Report(errors);

            if (errors.Count > 0)
            {
                throw new BuildFailedException(nameof(GameConfig));
            }
        }
    }
}
