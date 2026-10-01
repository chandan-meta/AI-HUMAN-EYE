using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace VisionLab {
    public class S02_UIBinder : MonoBehaviour {
        public Transform eye, human, niko, heroPoint;
        public GameObject[] layers;
        public EyePart[] parts;
        public VoiceoverPlayer voice;
        public TelemetryEmitter telemetry;
        public TMP_Text prompt, progress, status;
        public TMP_InputField question;
        public Button scan, inside, skip, replay, pause, send, continueButton;
        public Button[] chips;
        public GameObject askBar, chipBar, pauseOverlay, settings;
        public Toggle captions, reducedMotion;
        public Slider fontSize, voVolume, ambienceVolume;
        public Image fade;
        public Animator nikoAnimator, humanAnimator;
        public AudioSource sfx;
        public AudioClip[] effects;
        public PointerInputRouter pointer;
        public void Bind(S02_Director director) {
            scan.onClick.AddListener(director.Scan);
            inside.onClick.AddListener(director.Inside);
            skip.onClick.AddListener(director.Skip);
            replay.onClick.AddListener(director.Replay);
            pause.onClick.AddListener(director.TogglePause);
            send.onClick.AddListener(() => director.Ask(question.text));
            question.onSubmit.AddListener(director.Ask);
            continueButton.onClick.AddListener(director.Continue);
            for (int i=0; i<chips.Length; i++) { var id = parts[i].Id; chips[i].onClick.AddListener(() => director.Select(id, "chip")); }
            captions.onValueChanged.AddListener(voice.SetCaptions);
            reducedMotion.onValueChanged.AddListener(director.SetReducedMotion);
            fontSize.onValueChanged.AddListener(v => { voice.subtitle.fontSize = 28 + v * 6; prompt.fontSize = 30 + v * 6; });
            voVolume.onValueChanged.AddListener(v => voice.source.volume = v);
            ambienceVolume.onValueChanged.AddListener(voice.SetAmbienceVolume);
            pointer.director = director;
        }
        public void PlayEffect(int index) { if (index >= 0 && index < effects.Length && effects[index]) sfx.PlayOneShot(effects[index]); }
    }
}
