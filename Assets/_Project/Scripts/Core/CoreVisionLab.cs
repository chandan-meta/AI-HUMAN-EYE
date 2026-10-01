using UnityEngine;
using UnityEngine.SceneManagement;
namespace VisionLab {
    public class CoreVisionLab : MonoBehaviour {
        void Start() {
            if (!SceneManager.GetSceneByName("S02_MeetNiko").isLoaded)
                SceneManager.LoadSceneAsync("S02_MeetNiko", LoadSceneMode.Additive);
            Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
        }
    }
}
