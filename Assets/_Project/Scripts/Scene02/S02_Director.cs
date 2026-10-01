using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;
namespace VisionLab {
    public class S02_Director : MonoBehaviour {
        public S02_States State { get; private set; }
        public bool Paused { get; private set; }
        public bool ReducedMotion { get; private set; }
        public bool CanExplore => State == S02_States.Explore;
        public TelemetryEmitter telemetry => view.telemetry;
        public TimelineAsset intro, scanTimeline, reveal, transition;
        public TimelineAsset[] focusTimelines;
        public CinemachineCamera[] cameras;
        public PlayableDirector timeline;
        public ScanSequenceController scanner;
        public Animator cameraAnimator;
        S02_UIBinder view;
        readonly HashSet<string> visited = new();
        readonly Dictionary<string,int> repetitions = new();
        readonly INikoBrain brain = new ScriptedNikoBrain();
        EyePart selected;
        float idle, exploreTime, stateTime;
        bool nudged, timelineEnded, idleLogged;
        Vector3 socketPosition, socketScale;
        Quaternion socketRotation;
        string lastBeat;
        void Start() {
            view = FindFirstObjectByType<S02_UIBinder>();
            if (!view) { Debug.LogError("Load Core_VisionLab before S02_MeetNiko."); enabled = false; return; }
            scanner.view = view;
            view.Bind(this); view.pointer.heroCamera = CameraNamed("VC_EyeHero");
            socketPosition = view.eye.position; socketScale = view.eye.localScale; socketRotation = view.eye.rotation;
            view.niko.gameObject.SetActive(false); view.fade.color = Color.white;
            Begin(S02_States.Intro, intro);
        }
        CinemachineCamera CameraNamed(string name) => Array.Find(cameras, c => c.name == name);
        void CameraTo(string name, float blend = 1) {
            var cam = CameraNamed(name); if (!cam) return;
            var main = Camera.main.GetComponent<CinemachineBrain>();
            main.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, ReducedMotion ? .4f : blend);
            foreach (var c in cameras) c.Priority = c == cam ? 100 : 0;
        }
        void Change(S02_States state) {
            if (lastBeat != null) { telemetry.Emit("beat_end", lastBeat); lastBeat = null; }
            telemetry.Emit("state_end", State.ToString()); State = state; stateTime = idle = 0; nudged = idleLogged = false;
            telemetry.Emit("state_start", state.ToString());
            if(state!=S02_States.Intro)view.fade.color=Color.clear;
            view.scan.gameObject.SetActive(state == S02_States.AwaitScan);
            view.inside.gameObject.SetActive(state == S02_States.AwaitInsideView);
            view.askBar.SetActive(state == S02_States.Explore);
            view.chipBar.SetActive(state is S02_States.Explore or S02_States.PartFocus or S02_States.AwaitInsideView);
            foreach (var chip in view.chips) chip.interactable = state == S02_States.Explore;
            view.skip.interactable = state is S02_States.Intro or S02_States.Scan or S02_States.EyeReveal or S02_States.PartFocus or S02_States.Transition;
            view.continueButton.gameObject.SetActive(false);
            var action = Touchscreen.current != null ? "Tap" : "Click";
            view.prompt.text = state switch {
                S02_States.AwaitScan => action + " the SCAN button.",
                S02_States.Explore => action + " a glowing part, or ask Niko.",
                S02_States.AwaitInsideView => action + " INSIDE VIEW to continue.",
                _ => ""
            };
            if (state is S02_States.Explore or S02_States.AwaitScan or S02_States.AwaitInsideView) telemetry.Emit("prompt_shown", state.ToString());
        }
        void Begin(S02_States state, TimelineAsset asset) {
            Change(state); timelineEnded = false; timeline.playableAsset = asset;
            foreach (var camera in cameras) timeline.SetReferenceValue(camera.name, camera);
            foreach (var track in asset.GetOutputTracks()) {
                if (track is S02CueTrack) timeline.SetGenericBinding(track, this);
                if (track is CinemachineTrack) timeline.SetGenericBinding(track, Camera.main.GetComponent<CinemachineBrain>());
                if (track is AudioTrack) timeline.SetGenericBinding(track, view.voice.source);
                if (track is AnimationTrack) { track.muted=ReducedMotion;timeline.SetGenericBinding(track,cameraAnimator); }
            }
            timeline.time = 0; timeline.Play();
            if (Paused) timeline.Pause();
        }
        public void Cue(string beat, string action, int voice) {
            if (!string.IsNullOrEmpty(beat)) {
                if (lastBeat != null) telemetry.Emit("beat_end", lastBeat);
                lastBeat = beat; telemetry.Emit("beat_start", beat);
            }
            if (voice > 0) { view.voice.Play(voice,"",false); telemetry.Emit("voice", "VO_2_" + voice.ToString("00")); }
            switch (action) {
                case "establish": CameraTo("VC_Establish"); break;
                case "niko": view.niko.gameObject.SetActive(true); CameraTo("VC_Niko",1.2f); Gesture("Greet"); view.PlayEffect(0); break;
                case "human": CameraTo("VC_Human",1.5f); break;
                case "prompt_scan": CameraTo("VC_NikoHuman"); Gesture("RaiseHand"); break;
                case "scan": CameraTo("VC_ScanFront"); view.PlayEffect(1); break;
                case "located": scanner.Finish(); foreach (var p in view.parts) p.Highlight(Color.cyan,1,1); view.PlayEffect(2); break;
                case "socket": CameraTo("VC_EyeInSkull",1.5f); break;
                case "pinch": Gesture("Pinch"); CameraTo("VC_NikoCU",.8f); if(view.humanAnimator) view.humanAnimator.speed=0; view.PlayEffect(3); break;
                case "hero": CameraTo("VC_EyeHero",2); break;
                case "snap": Gesture("Snap"); view.PlayEffect(4); break;
                case "focus": CameraTo("VC_Part_" + selected.Id,1.2f); Gesture("Point"); break;
                case "return": CameraTo("VC_EyeHero"); break;
                case "transition": CameraTo("VC_EyeHero"); Gesture("Turn"); break;
                case "end": OnBeatComplete(beat); break;
            }
        }
        public void OnBeatComplete(string beatId) { timelineEnded = true; telemetry.Emit("timeline_complete", beatId); }
        public void OnTimelineEnd(){OnBeatComplete(timeline.playableAsset.name);}
        void Gesture(string name) { if (view.nikoAnimator) view.nikoAnimator.CrossFade(name,.25f); }
        void Update() {
            if (!view || Paused) return;
            var dt = Time.unscaledDeltaTime; stateTime += dt; idle += dt;
            if (view.nikoAnimator) view.nikoAnimator.SetBool("Talking", view.voice.IsPlaying);
            if (State == S02_States.Intro) view.fade.color = new Color(1,1,1,1-Mathf.Clamp01(stateTime/1.5f));
            if (State == S02_States.Scan) {
                if (stateTime < 6.5f) scanner.Sample(ReducedMotion ? Mathf.Min(6,stateTime*12) : stateTime, ReducedMotion);
                if (stateTime >= 13) {
                    var t = Mathf.SmoothStep(0,1,(stateTime-13)/2);
                    view.eye.position = Vector3.Lerp(socketPosition,view.heroPoint.position,t);
                    view.eye.localScale = Vector3.Lerp(socketScale,socketScale*15,t);
                    view.eye.rotation = socketRotation;
                    foreach(var r in view.human.GetComponentsInChildren<Renderer>()) {
                        var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block); block.SetFloat("_Alpha",1-Mathf.Clamp01(stateTime-13)); r.SetPropertyBlock(block);
                    }
                    if(stateTime>=14) view.human.gameObject.SetActive(false);
                    view.niko.position=Vector3.Lerp(new Vector3(-.8f,0,0),new Vector3(-.8f,0,-1.5f),t);
                }
            }
            if (State == S02_States.EyeReveal && stateTime >= 7.5f && stateTime < 10.5f) {
                var index = Mathf.Clamp((int)((stateTime-7.5f)/.6f),0,3);
                for(int i=0;i<view.parts.Length;i++) view.parts[i].Highlight(i==index ? Color.cyan : Color.white,1,i==index ? 1 : .1f);
            }
            if(State==S02_States.EyeReveal && !ReducedMotion)view.eye.Rotate(Vector3.up,6*dt,Space.World);
            if (CanExplore) {
                exploreTime += dt;
                view.continueButton.gameObject.SetActive(visited.Count>=2 && exploreTime>=90);
                foreach(var part in view.parts) part.Highlight(part.Visited ? Color.white : Color.cyan, part.Id == "cornea" ? .2f : 1, ReducedMotion ? .35f : .3f + .15f*Mathf.Sin(Time.unscaledTime*7.54f));
            }
            if(!idleLogged && idle>10 && State is S02_States.Explore or S02_States.AwaitScan or S02_States.AwaitInsideView){idleLogged=true;telemetry.Emit("idle_gap",State+":"+idle);}
            if (!nudged && (State==S02_States.AwaitScan && idle>=15 || CanExplore && idle>=20)) {
                nudged=true; telemetry.Emit("idle_nudge", State+":"+idle); view.voice.Play(State==S02_States.AwaitScan?17:18);
            }
            if (!timelineEnded || view.voice.IsPlaying) return;
            timelineEnded=false; timeline.Stop();
            switch(State) {
                case S02_States.Intro: Change(S02_States.AwaitScan); break;
                case S02_States.Scan: FinishScan(); Begin(S02_States.EyeReveal,reveal); break;
                case S02_States.EyeReveal: view.eye.rotation=socketRotation;CameraTo("VC_EyeHero"); Change(S02_States.Explore); break;
                case S02_States.PartFocus:
                    selected.Visited=true; visited.Add(selected.Id); RestoreParts(); CameraTo("VC_EyeHero");
                    view.progress.text=visited.Count+" / 4 explored"; telemetry.Emit("visited_count",visited.Count.ToString());
                    if(visited.Count==4) Begin(S02_States.Transition,transition); else Change(S02_States.Explore); break;
                case S02_States.Transition: view.PlayEffect(5); Change(S02_States.AwaitInsideView); break;
            }
        }
        void FinishScan() { scanner.Finish();view.human.gameObject.SetActive(false);view.eye.position=view.heroPoint.position;view.eye.localScale=socketScale*15;view.eye.rotation=socketRotation;view.niko.position=new Vector3(-.8f,0,-1.5f); }
        void RestoreParts() { foreach(var part in view.parts)part.Highlight(part.Visited?Color.white:Color.cyan,part.Id=="cornea"?.2f:1,.3f); }
        void ActionTaken(string name) { telemetry.Emit("time_to_action",name+":"+stateTime); idle=0;nudged=idleLogged=false;view.PlayEffect(6); }
        public void Scan() { if(State!=S02_States.AwaitScan || Paused)return;ActionTaken("scan");telemetry.Emit("scan_pressed");Begin(S02_States.Scan,scanTimeline); }
        public void Select(string id,string source) {
            if (!CanExplore || Paused)return;
            selected=Array.Find(view.parts,p=>p.Id==id);if(!selected)return;
            view.eye.rotation=socketRotation;
            ActionTaken("part"); repetitions.TryGetValue(id,out var count); repetitions[id]=++count;
            telemetry.Emit("part_selected",id+":"+source+":"+count);
            foreach(var p in view.parts)p.Highlight(p==selected?new Color(1,.78f,.34f):Color.white,p==selected?1:.15f,p==selected?1:0);
            Begin(S02_States.PartFocus,focusTimelines[Array.IndexOf(view.parts,selected)]);
            if(count==3) StartCoroutine(RepeatExplanation(id));
        }
        IEnumerator RepeatExplanation(string id) {
            yield return null; while(State==S02_States.PartFocus && (Paused || view.voice.IsPlaying))yield return null;
            if(State==S02_States.PartFocus) { view.voice.Play(19,ScriptedNikoBrain.Alternative(id));telemetry.Emit("repeat_explanation",id); }
        }
        public void Ask(string question) {
            if(!CanExplore || Paused || string.IsNullOrWhiteSpace(question))return;
            question=question.Trim();if(question.Length>240)question=question.Substring(0,240);
            var id=brain.Resolve(question);telemetry.Emit("question",question+" => "+(id??"unresolved"));
            view.question.text="";if(id!=null)Select(id,"ask");else {ActionTaken("question");view.voice.Play(20);}
        }
        public void Continue() { if(CanExplore && visited.Count>=2 && exploreTime>=90 && !Paused)Begin(S02_States.Transition,transition); }
        public void Inside() {
            if(State!=S02_States.AwaitInsideView || Paused)return;
            ActionTaken("inside");telemetry.Emit("inside_view_pressed");Change(S02_States.Handoff);
            SceneHandoff.ToScene3(new EyeState {position=view.eye.position,rotation=view.eye.rotation,scale=view.eye.localScale,nikoPosition=view.niko.position,visited=new List<string>(visited).ToArray(),telemetrySessionId=telemetry.SessionId});
            view.status.text=Application.CanStreamedLevelBeLoaded("S03_InsideEye")?"Opening inside view…":"Inside view is not available in this build.";
        }
        public void Skip() {
            if(!view.skip.interactable || Paused)return;telemetry.Emit("skip",State.ToString());view.voice.Stop();
            if(State==S02_States.Intro){view.niko.gameObject.SetActive(true);view.fade.color=Color.clear;CameraTo("VC_NikoHuman");}
            if(State==S02_States.Scan)FinishScan();
            timeline.time=Math.Max(0,timeline.duration-.01);timeline.Evaluate();timelineEnded=true;
        }
        public void Replay() {
            if(Paused)return;telemetry.Emit("replay");
            if(CanExplore && selected)Select(selected.Id,"replay");else if(view.voice.LastId>0)view.voice.Play(view.voice.LastId);
        }
        public void TogglePause() {
            Paused=!Paused;telemetry.Emit("pause",Paused.ToString());view.pauseOverlay.SetActive(Paused);view.voice.SetPaused(Paused);
            if(Paused)timeline.Pause();else if(State is S02_States.Intro or S02_States.Scan or S02_States.EyeReveal or S02_States.PartFocus or S02_States.Transition)timeline.Resume();
            if(view.nikoAnimator)view.nikoAnimator.speed=Paused?0:1;
            if(view.humanAnimator)view.humanAnimator.speed=Paused || State>=S02_States.EyeReveal?0:1;
            if(Paused)view.sfx.Pause();else view.sfx.UnPause();
            if(view.voice.ambience){if(Paused)view.voice.ambience.Pause();else view.voice.ambience.UnPause();}
            foreach(var particles in view.GetComponentsInChildren<ParticleSystem>()){if(Paused)particles.Pause();else particles.Play();}
        }
        public void SetReducedMotion(bool reduced) { ReducedMotion=reduced;telemetry.Emit("reduced_motion",reduced.ToString());foreach(var particles in view.GetComponentsInChildren<ParticleSystem>(true))particles.gameObject.SetActive(!reduced);foreach(var noise in GetComponentsInChildren<CinemachineBasicMultiChannelPerlin>())noise.enabled=!reduced; }
    }
}
