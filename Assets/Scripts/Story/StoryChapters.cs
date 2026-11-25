using UnityEngine;
using UnityEngine.SceneManagement;

namespace LeftBehind.Story
{
    /// <summary>
    /// Starting points for the main menu: a new game, or a loop already reached (Chapters).
    /// Starting at a later loop fills in what the player would have found before it,
    /// so the gates, objectives and carried items are as if the earlier loops had been played.
    /// </summary>
    public static class StoryChapters
    {
        public const string MenuScene = "MainMenu";

        public static readonly string[] Scenes = { "SampleScene", "Loop2_YearsEarlier", "Loop3_TheAftermath" };
        // Numbers only: the player must not learn from the menu when each chapter takes place.
        public static readonly string[] Names = { "Chapter I", "Chapter II", "Chapter III" };

        /// <summary>Loop 1, 2 or 3.</summary>
        public static void Start(int loop)
        {
            loop = Mathf.Clamp(loop, 1, Scenes.Length);
            StoryStateManager.ResetAll();
            if (loop >= 2)
            {
                foreach (var f in new[] { "introSeen", "triedFrontDoor", "sawFamilyPhoto", "sawEmptyTable", "hasFlashlight", "loop1Complete" })
                    StoryStateManager.Set(f);
                StoryStateManager.AddItem("flashlight");
            }
            if (loop >= 3)
            {
                foreach (var f in new[] { "loop2Arrived", "sawTableForSix", "readFridgeNote", "sawDarkLanding", "hasFuse",
                             "powerRestored", "hasStudyKey", "studyBricked", "loop2Complete" })
                    StoryStateManager.Set(f);
                StoryStateManager.AddItem("studyKey");
            }
            LoadGameScene(Scenes[loop - 1]);
        }

        public static void LoadGameScene(string scene)
        {
            StoryUI.CloseAll();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(scene);
        }

        public static void ToMenu()
        {
            StoryUI.CloseAll();
            Time.timeScale = 1f;
            StoryStateManager.ResetAll();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(MenuScene);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
