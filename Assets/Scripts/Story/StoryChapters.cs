using UnityEngine;
using UnityEngine.SceneManagement;

namespace LeftBehind.Story
{
    /// <summary>
    /// Starting points for the main menu: a new game, or a loop already reached (Chapters).
    /// Starting at a later loop fills in what the player would have found before it,
    /// so the gates, objectives and Pockets are as if the earlier loops had been played.
    /// </summary>
    public static class StoryChapters
    {
        public const string MenuScene = "MainMenu";

        public static readonly string[] Scenes = { "SampleScene", "Loop2_YearsEarlier", "Loop3_TheAftermath" };
        // Numbers only: the player must not learn from the menu when each chapter takes place.
        public static readonly string[] Names = { "Chapter I", "Chapter II", "Chapter III" };

        private static readonly PocketItem Flashlight = new PocketItem
        {
            id = "flashlight",
            name = "Flashlight",
            description = "Ruth bought it for the winter power cuts. She always kept spare batteries in the kitchen drawer.",
        };

        private static readonly PocketItem StudyKey = new PocketItem
        {
            id = "studyKey",
            name = "Study key",
            description = "A small brass key, wrapped in thread at the bottom of Ruth's sewing box. She kept it from me.",
        };

        /// <summary>Loop 1, 2 or 3.</summary>
        public static void Start(int loop)
        {
            loop = Mathf.Clamp(loop, 1, Scenes.Length);
            StoryStateManager.ResetAll();
            if (loop >= 2)
            {
                foreach (var f in new[] { "introSeen", "triedFrontDoor", "sawFamilyPhoto", "sawEmptyTable", "hasFlashlight", "loop1Complete" })
                    StoryStateManager.Set(f);
                StoryStateManager.AddItem(Flashlight);
            }
            if (loop >= 3)
            {
                foreach (var f in new[] { "loop2Arrived", "sawTableForSix", "readFridgeNote", "sawDarkLanding", "hasFuse",
                             "powerRestored", "hasStudyKey", "studyBricked", "loop2Complete" })
                    StoryStateManager.Set(f);
                StoryStateManager.AddItem(StudyKey);
                StoryStateManager.AddNote(new NoteEntry
                {
                    id = "fridgeNote",
                    title = "Note on the fridge",
                    body = "RUTH TOOK THE KIDS\nTO HER MOTHER'S.\n\nDON'T CALL.\n\n— W.",
                });
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
