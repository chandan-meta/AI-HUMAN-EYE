using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace VisionLab {
    [Serializable] public class EyeState {
        public Vector3 position, scale, nikoPosition;
        public Quaternion rotation;
        public string[] visited;
        public string telemetrySessionId;
    }
    public static class SceneHandoff {
        public static EyeState Current { get; private set; }
        public static event Action<EyeState> ToScene3Requested;
        public static void ToScene3(EyeState state) {
            Current = state; ToScene3Requested?.Invoke(state);
            // Scene 3 is a separate deliverable; preserve the live core and transform.
            if (Application.CanStreamedLevelBeLoaded("S03_InsideEye")) Load();
        }
        static async void Load() {
            var load = SceneManager.LoadSceneAsync("S03_InsideEye", LoadSceneMode.Additive);
            while (!load.isDone) await System.Threading.Tasks.Task.Yield();
            var chapter = SceneManager.GetSceneByName("S02_MeetNiko");
            if (chapter.isLoaded) _ = SceneManager.UnloadSceneAsync(chapter);
        }
    }
}
