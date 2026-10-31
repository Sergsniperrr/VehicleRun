using UnityEditor;

namespace _Project.Editor.Configs
{
    public class GameConfigMenu
    {
        private const string ValidateMenuPath = "_Project/Validate Game Config";

        [MenuItem(ValidateMenuPath)]
        private static void Validate()
        {
            GameConfigCheck check = new GameConfigCheck();
            check.Report(check.Run());
        }
    }
}
