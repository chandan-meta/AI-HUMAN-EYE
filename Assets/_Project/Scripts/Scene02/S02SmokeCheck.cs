using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace VisionLab {
    public class S02SmokeCheck : MonoBehaviour {
        int step;
        S02_Director director;
        S02_UIBinder view;
        float next, deadline;
        string screenshotDirectory;
        bool cinematic;
        bool capturing;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--s02-smoke"))new GameObject("S02 smoke check").AddComponent<S02SmokeCheck>();}
        void Start(){cinematic=Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--s02-cinematic");deadline=Time.realtimeSinceStartup+180;screenshotDirectory=Path.Combine(Application.dataPath,"../Builds/Checks");Directory.CreateDirectory(screenshotDirectory);}
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        void Update(){
            try{
                if(Time.realtimeSinceStartup>deadline)throw new Exception("Smoke check timed out at step "+step);
                if(capturing||Time.realtimeSinceStartup<next)return;
                director??=FindFirstObjectByType<S02_Director>();view??=FindFirstObjectByType<S02_UIBinder>();
                if(!director||!view||director.State==S02_States.Enter)return;
                switch(step){
                    case 0:
                        Check(new ScriptedNikoBrain().Resolve("What is the colourful iris?")=="iris","Question resolution failed");
                        Check(new ScriptedNikoBrain().Resolve("conspiracy")==null,"Question matched inside another word");
                        Check(view.parts.Length==4,"Parts missing");Check(view.voice.manifest.lines.Length==20,"VO manifest incomplete");
                        Capture("01_intro.png",()=>{if(!cinematic)director.Skip();Next();});break;
                    case 1:if(director.State!=S02_States.AwaitScan)return;Check(view.scan.gameObject.activeSelf,"Scan wait UI missing");director.Scan();Next();break;
                    case 2:Check(director.State==S02_States.Scan,"Scan did not start");director.TogglePause();Check(director.Paused&&Time.timeScale==1,"Pause changed timeScale");director.TogglePause();if(!cinematic)director.Skip();Next();break;
                    case 3:if(director.State!=S02_States.EyeReveal)return;Check(!view.human.gameObject.activeSelf,"Human did not dissolve");Check(Vector3.Distance(view.eye.position,view.heroPoint.position)<.001f,"Eye isolation failed");if(!cinematic)director.Skip();Next();break;
                    case 4:if(director.State!=S02_States.Explore)return;Check(view.fade.color.a==0,"Entry fade survived skip");foreach(var part in view.parts){var block=new MaterialPropertyBlock();part.surfaces[0].GetPropertyBlock(block);Debug.Log("EYE_CHECK "+part.Id+" alpha="+block.GetFloat("_Alpha")+" base="+part.surfaces[0].sharedMaterial.GetColor("_BaseColor")+" bounds="+part.surfaces[0].bounds);}Capture("02_explore.png",()=>{director.Ask("What does the white layer do?");Next();});break;
                    case 5:Check(director.State==S02_States.PartFocus,"Ask did not focus sclera");Capture("03_sclera_focus.png",()=>{if(!cinematic)director.Skip();Next();});break;
                    case 6:if(director.State!=S02_States.Explore)return;Check(view.parts[0].Visited,"Sclera was not marked visited");director.Select("cornea","touch");Next();break;
                    case 7:Check(director.State==S02_States.PartFocus,"Touch selection failed");director.Skip();Next();break;
                    case 8:if(director.State!=S02_States.Explore)return;director.Select("iris","mouse");Next();break;
                    case 9:director.Skip();Next();break;
                    case 10:if(director.State!=S02_States.Explore)return;director.Select("pupil","chip");Next();break;
                    case 11:director.Skip();Next();break;
                    case 12:if(director.State!=S02_States.Transition)return;Check(Array.TrueForAll(view.parts,p=>p.Visited),"Visited set incomplete");director.Skip();Next();break;
                    case 13:if(director.State!=S02_States.AwaitInsideView)return;Check(view.inside.gameObject.activeSelf,"Inside View did not unlock");var before=view.eye.position;director.Inside();Check(SceneHandoff.Current!=null&&SceneHandoff.Current.visited.Length==4,"Handoff incomplete");Check(view.eye.position==before,"Handoff moved the eye");File.WriteAllText(Path.Combine(screenshotDirectory,"SMOKE_RESULT.txt"),"PASS: intro gate, scan gate, pause, eye isolation, questions, four selection sources, visited set, transition, transform-preserving handoff. Physical device input and frame rate remain unverified.");Finish(0);break;
                }
            }catch(Exception e){File.WriteAllText(Path.Combine(screenshotDirectory,"SMOKE_RESULT.txt"),"FAIL: "+e);Debug.LogException(e);Finish(1);}
        }
        void Next(){step++;next=Time.realtimeSinceStartup+2f;}
        void Capture(string name,Action after){capturing=true;StartCoroutine(CaptureFrame(name,after));}
        IEnumerator CaptureFrame(string name,Action after){
            var camera=Camera.main;var canvas=FindFirstObjectByType<Canvas>();var mode=canvas.renderMode;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.1f;Canvas.ForceUpdateCanvases();
            yield return null;
            foreach(var label in canvas.GetComponentsInChildren<TMPro.TMP_Text>())label.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            var target=RenderTexture.GetTemporary(1600,900,24);var previous=RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(screenshotDirectory,name),texture.EncodeToPNG());Destroy(texture);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);canvas.renderMode=mode;
            capturing=false;after();
        }
        static void Finish(int code){
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
            #else
            Application.Quit(code);
            #endif
        }
    }
}
