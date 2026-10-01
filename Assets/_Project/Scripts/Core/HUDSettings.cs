using UnityEngine;
using UnityEngine.UI;
namespace VisionLab {
    public class HUDSettings : MonoBehaviour {
        public Button settingsButton,aboutButton;
        public GameObject panel;
        public TextAsset credits;
        void Start(){settingsButton.onClick.AddListener(()=>panel.SetActive(!panel.activeSelf));aboutButton.onClick.AddListener(()=>{var status=FindFirstObjectByType<S02_UIBinder>().status;status.text=credits?credits.text:"Niko: Quaternius (CC0). Anatomy: Z-Anatomy / BodyParts3D (CC BY-SA). Supplied eye: project archive.";});}
    }
}
