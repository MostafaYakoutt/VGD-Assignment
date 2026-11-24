using UnityEditor;
using UnityEditor.SceneManagement;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// Pressing Play in the Editor starts the game at the main menu, as the built game does,
    /// whichever scene is open. Turn it off to play the open scene directly (for testing one loop):
    /// Left Behind > Play From Main Menu.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMainMenu
    {
        private const string MenuPath = "Left Behind/Play From Main Menu";
        private const string PrefKey = "LeftBehind.PlayFromMainMenu";
        private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";

        static PlayFromMainMenu() => EditorApplication.delayCall += Apply;

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScene) : null;
        }
    }
}
