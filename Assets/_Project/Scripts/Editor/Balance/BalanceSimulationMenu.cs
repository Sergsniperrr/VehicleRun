using UnityEditor;

namespace _Project.Editor.Balance
{
    public class BalanceSimulationMenu
    {
        private const string RunMenuPath = "_Project/Balance/Run Simulation";

        [MenuItem(RunMenuPath)]
        private static void Run()
        {
            new BalanceSimulationLauncher().Launch();
        }
    }
}
