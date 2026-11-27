using LeftBehind.Loop;
using LeftBehind.Story;
using UnityEditor;
using UnityEngine;

namespace LeftBehind.EditorTools
{
    /// <summary>
    /// The game remembers two things between runs (PlayerPrefs): the ending reached, which shows the
    /// family's faces on the main menu photo after "Remember.", and the furthest loop reached, which
    /// opens chapters in the Chapters menu. Testing the ending in the Editor saves it too, so these
    /// put things back to a first playthrough.
    /// Menu: Left Behind > Saved Progress > Forget The Ending / Forget Everything.
    /// </summary>
    public static class SavedProgressMenu
    {
        [MenuItem("Left Behind/Saved Progress/Forget The Ending (blur the menu photo)")]
        private static void ForgetEnding()
        {
            PlayerPrefs.DeleteKey(EndingDirector.EndingKey);
            PlayerPrefs.Save();
            Debug.Log("Saved progress: the ending is forgotten; the main menu photo is blurred again.");
        }

        [MenuItem("Left Behind/Saved Progress/Forget Everything (ending and chapters)")]
        private static void ForgetEverything()
        {
            PlayerPrefs.DeleteKey(EndingDirector.EndingKey);
            PlayerPrefs.DeleteKey(LoopEnvironment.ReachedKey);
            PlayerPrefs.Save();
            Debug.Log("Saved progress: the ending and the chapters reached are forgotten, as on a first playthrough.");
        }
    }
}
