using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using TMPro;
namespace VisionLab.Editor {
    public static class S02Builder {
        const string Root="Assets/_Project/";
        static TMP_FontAsset font;
        static Material fontMaterial;
        static Material white, skin, muscle, bone, brain, cyan, navy, brass, holo;
        static S02_UIBinder view;
        static S02_Director director;
        static SignalAsset completeSignal;
        static readonly string[] Parts={"sclera","cornea","iris","pupil"};
        [MenuItem("Vision Lab/Build Scene 2 module")]
        public static void Build() {
            foreach(var folder in new[]{"Scenes","Prefabs","Materials","Timeline/S02","Data/S02","Docs","Audio/SFX","Audio/Amb"})Directory.CreateDirectory(Root+folder);
            AssetDatabase.Refresh();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(!font)throw new Exception("TMP essential resources are missing.");
            fontMaterial=new Material(font.material);fontMaterial.shader=Shader.Find("VisionLab/UI Distance Field");fontMaterial.name="UI_Font";SaveAsset(fontMaterial,Root+"Materials/UI_Font.mat");
            SetLayer();
            white=Mat("Sclera",Color.white);skin=Mat("Skin",new Color(.72f,.49f,.37f));
            muscle=Mat("Muscles",new Color(.55f,.12f,.13f));bone=Mat("Skull",new Color(.76f,.81f,.83f));brain=Mat("Brain",new Color(.75f,.51f,.56f));
            cyan=Mat("Cyan",new Color(.05f,.65f,.85f));navy=Mat("Navy",new Color(.018f,.035f,.07f));brass=Mat("Brass",new Color(.42f,.3f,.13f));
            holo=Mat("Niko_Holo",new Color(.05f,.7f,1),"VisionLab/Holo");
            var core=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);core.name="Core_VisionLab";
            var root=new GameObject("VisionLab_Core");root.AddComponent<CoreVisionLab>();view=root.AddComponent<S02_UIBinder>();
            var quality=root.AddComponent<QualityTier>();
            var sourcePipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            quality.pc=UnityEngine.Object.Instantiate(sourcePipeline);quality.pc.name="PC_High";SaveAsset(quality.pc,Root+"Data/S02/PC_High.asset");
            quality.tablet=UnityEngine.Object.Instantiate(sourcePipeline);quality.tablet.name="Tablet_Mid";quality.tablet.renderScale=.8f;quality.tablet.msaaSampleCount=2;quality.tablet.shadowDistance=15;quality.tablet.maxAdditionalLightsCount=1;SaveAsset(quality.tablet,Root+"Data/S02/Tablet_Mid.asset");
            view.telemetry=root.AddComponent<TelemetryEmitter>();
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(CinemachineBrain));camera.tag="MainCamera";
            camera.GetComponent<Camera>().backgroundColor=new Color(.012f,.025f,.05f);camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().allowHDR=true;camera.GetComponent<Camera>().nearClipPlane=.01f;
            camera.GetComponent<CinemachineBrain>().DefaultBlend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut,1);
            BuildEnvironment(root.transform);
            BuildHuman(root.transform);BuildEye(root.transform);BuildNiko(root.transform);BuildUI(root.transform);
            view.heroPoint=new GameObject("EyeHeroPoint").transform;view.heroPoint.SetParent(root.transform);view.heroPoint.position=new Vector3(0,1.55f,-1.5f);
            view.pointer=root.AddComponent<PointerInputRouter>();view.pointer.eye=view.eye;
            view.voice=root.AddComponent<VoiceoverPlayer>();view.voice.source=root.AddComponent<AudioSource>();view.voice.source.playOnAwake=false;
            view.voice.ambience=root.AddComponent<AudioSource>();view.voice.ambience.loop=true;view.voice.ambience.playOnAwake=true;view.voice.ambience.volume=.22f;
            view.voice.subtitle=GameObject.Find("Subtitles").GetComponent<TMP_Text>();
            var vo=ScriptableObject.CreateInstance<VOManifest>();JsonUtility.FromJsonOverwrite(File.ReadAllText(Root+"Data/S02/VOManifest_S02.json"),vo);
            foreach(var line in vo.lines)line.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/VO/S02/VO_2_"+line.id.ToString("00")+"_niko.wav");
            SaveAsset(vo,Root+"Data/S02/VOManifest_S02.asset");view.voice.manifest=vo;
            view.sfx=root.AddComponent<AudioSource>();view.sfx.playOnAwake=false;
            var effectNames=new[]{"hologram_in","scan_hum_loop","eye_ping","pinch_whoosh","snap","inside_view_unlock","ui_tap","part_ping","layer_reveal"};
            view.effects=effectNames.Select((n,i)=>Tone(n,i==1?6:.3f,160+i*70)).ToArray();
            view.voice.ambience.clip=Tone("observatory_room_tone",4,48,true);
            PrefabUtility.SaveAsPrefabAsset(GameObject.Find("HUD"),Root+"Prefabs/HUD.prefab");
            EditorSceneManager.SaveScene(core,Root+"Scenes/Core_VisionLab.unity");
            var chapter=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);chapter.name="S02_MeetNiko";
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(chapter);
            var obj=new GameObject("S02_Director");director=obj.AddComponent<S02_Director>();director.timeline=obj.AddComponent<PlayableDirector>();director.timeline.playOnAwake=false;
            completeSignal=ScriptableObject.CreateInstance<SignalAsset>();SaveAsset(completeSignal,Root+"Data/S02/TimelineComplete.signal");
            var receiver=obj.AddComponent<SignalReceiver>();var reaction=new UnityEngine.Events.UnityEvent();UnityEditor.Events.UnityEventTools.AddPersistentListener(reaction,director.OnTimelineEnd);receiver.AddReaction(completeSignal,reaction);
            director.scanner=obj.AddComponent<ScanSequenceController>();
            var plane=Geometry("ScanPlane",new Vector3(0,1,0),new Vector3(1.2f,.008f,.5f),cyan);director.scanner.plane=plane.transform;plane.SetActive(false);
            var cameras=new GameObject("S02_Cameras").transform;
            director.cameraAnimator=cameras.gameObject.AddComponent<Animator>();
            var socket=new Vector3(.03f,1.63f,-.08f);var hero=new Vector3(0,1.55f,-1.5f);
            var cams=new List<CinemachineCamera>();
            cams.Add(Cam("VC_Establish",new Vector3(0,1.65f,-6),new Vector3(0,1.05f,0),24,cameras));
            cams.Add(Cam("VC_Niko",new Vector3(-.8f,1.4f,-2.7f),new Vector3(-.8f,1.05f,0),40,cameras));
            var noise=ScriptableObject.CreateInstance<NoiseSettings>();noise.OrientationNoise=new[]{new NoiseSettings.TransformNoiseParams { X=new NoiseSettings.NoiseParams{Frequency=.2f,Amplitude=.15f},Y=new NoiseSettings.NoiseParams{Frequency=.3f,Amplitude=.15f} }};SaveAsset(noise,Root+"Data/S02/NikoHandheld.asset");var perlin=cams[1].gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();perlin.NoiseProfile=noise;perlin.AmplitudeGain=.3f;
            cams.Add(Cam("VC_Human",new Vector3(0,1.2f,-3.5f),new Vector3(0,.95f,0),35,cameras));
            cams.Add(Cam("VC_NikoHuman",new Vector3(-.25f,1.3f,-3.5f),new Vector3(-.25f,1,0),35,cameras));
            cams.Add(Cam("VC_ScanFront",new Vector3(0,1.2f,-4.7f),new Vector3(0,1,0),50,cameras));
            cams.Add(Cam("VC_EyeInSkull",socket+new Vector3(.13f,.055f,-.6f),socket,85,cameras));
            cams.Add(Cam("VC_NikoCU",new Vector3(-.8f,1.4f,-2),new Vector3(-.8f,1.35f,0),70,cameras));
            cams.Add(Cam("VC_EyeHero",hero+new Vector3(0,0,-1.2f),hero,50,cameras));
            for(int i=0;i<Parts.Length;i++)cams.Add(Cam("VC_Part_"+Parts[i],hero+new Vector3(i==0?.08f:i==1?.12f:0,.02f,i==0?-1.5f:-.75f),hero, i==0?70:i==3?100:85,cameras));
            director.cameras=cams.ToArray();
            director.intro=Timeline("Intro",31,new[]{(0d,"B01","establish",0),(8d,"B02","niko",0),(10d,"B03","",1),(14.5d,"","",2),(18.5d,"","",3),(22d,"B04","human",0),(24d,"","",4),(28d,"B05","prompt_scan",5)});
            director.scanTimeline=Timeline("Scan",16.5,new[]{(0d,"B06","scan",0),(6.5d,"B07","located",6),(8d,"B08","socket",0),(8.5d,"","",7),(13d,"B09","pinch",0),(14d,"","hero",0)});
            director.reveal=Timeline("EyeReveal",15,new[]{(0d,"B10","hero",0),(.3d,"","",8),(3d,"","",9),(7.5d,"B11","snap",0),(10.5d,"B12","",10)});
            director.transition=Timeline("Transition",12,new[]{(0d,"B17","transition",15),(6.5d,"","",16)});
            director.focusTimelines=new TimelineAsset[4];
            for(int i=0;i<4;i++){
                var duration=vo.Find(11+i).clip?vo.Find(11+i).clip.length:vo.Find(11+i).text.Length*.06;
                director.focusTimelines[i]=Timeline("Part_"+Parts[i],duration+2.1,new[]{(0d,"B"+(13+i),"focus",11+i),(duration+.8,"","return",0)});
            }
            EditorSceneManager.SaveScene(chapter,Root+"Scenes/S02_MeetNiko.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"Scenes/Core_VisionLab.unity",true),new EditorBuildSettingsScene(Root+"Scenes/S02_MeetNiko.unity",true)};
            PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;PlayerSettings.runInBackground=true;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.iOS.targetOSVersionString="15.0";
            AssetDatabase.SaveAssets();
            foreach(var asset in new[]{director.intro,director.scanTimeline,director.reveal,director.transition}.Concat(director.focusTimelines)){
                var path=AssetDatabase.GetAssetPath(asset);if(AssetDatabase.LoadAllAssetsAtPath(path).OfType<TrackAsset>().Count()<3)throw new Exception("Timeline tracks were not persisted: "+path);
            }
            AssetAudit.Run();Debug.Log("S02_MODULE_BUILT");
        }
        static void SetLayer(){var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");if(Enumerable.Range(8,24).All(i=>layers.GetArrayElementAtIndex(i).stringValue!="EyePart")){for(int i=8;i<32;i++)if(layers.GetArrayElementAtIndex(i).stringValue==""){layers.GetArrayElementAtIndex(i).stringValue="EyePart";break;}tags.ApplyModifiedProperties();}}
        static void SaveAsset(UnityEngine.Object obj,string path){if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)){AssetDatabase.DeleteAsset(path);}AssetDatabase.CreateAsset(obj,path);}
        static Material Mat(string name,Color colour,string shader="VisionLab/Highlight") {
            var path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",colour);if(m.HasProperty("_Alpha"))m.SetFloat("_Alpha",1);return m;
        }
        static GameObject Model(string path,string name,Transform parent){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"SourceAssets/"+path);if(!asset)throw new Exception("Missing model: "+path);var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset);obj.name=name;obj.transform.SetParent(parent,false);return obj;}
        static Bounds Bounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
        static void Apply(GameObject go,Material material){foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();}
        static void BuildHuman(Transform parent){
            var human=new GameObject("Human_Layered");human.transform.SetParent(parent);view.human=human.transform;
            var skinObj=Model("Zip/Extra/FitnessMan.fbx","Skin",human.transform);var bounds=Bounds(skinObj);float scale=1.75f/bounds.size.y;skinObj.transform.localScale=Vector3.one*scale;skinObj.transform.localPosition=new Vector3(0,-bounds.min.y*scale,0);
            foreach(var r in skinObj.GetComponentsInChildren<Renderer>()){
                var name=r.name.ToLowerInvariant();
                if(name.Contains("cornea")||name.Contains("iris")||name.Contains("pupil")||name.Contains("vitreous")||name.Contains("choroid")||name.Contains("retina")||name.Contains("lense")||name.Contains("vain")||name.Contains("muscle")||name.Contains("ligement")||name.Contains("eye_ball")){r.gameObject.SetActive(false);continue;}
                var material=name.Contains("body")?Mat("HumanBody",Color.white):name.Contains("short")||name.Contains("tshirt")?navy:name.Contains("hair")?Mat("Hair",new Color(.08f,.045f,.025f)):skin;
                if(name.Contains("body"))material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"SourceAssets/Zip/Arun/eyeparts/FM_Body_Diff_01.jpg"));
                r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
            }
            var sourceAnimator=skinObj.GetComponentInChildren<Animator>();if(sourceAnimator)sourceAnimator.enabled=false;
            view.humanAnimator=human.AddComponent<Animator>();
            var breathing=new AnimationClip();breathing.name="Idle_Breathing";breathing.SetCurve("",typeof(Transform),"m_LocalScale.y",new AnimationCurve(new Keyframe(0,1),new Keyframe(2,1.002f),new Keyframe(4,1)));
            var breathingSettings=AnimationUtility.GetAnimationClipSettings(breathing);breathingSettings.loopTime=true;AnimationUtility.SetAnimationClipSettings(breathing,breathingSettings);SaveAsset(breathing,Root+"Data/S02/Idle_Breathing.anim");
            var breathingPath=Root+"Data/S02/Human.controller";if(File.Exists(breathingPath))AssetDatabase.DeleteAsset(breathingPath);var breathingController=AnimatorController.CreateAnimatorControllerAtPath(breathingPath);breathingController.layers[0].stateMachine.AddState("Idle_Breathing").motion=breathing;view.humanAnimator.runtimeAnimatorController=breathingController;
            var muscles=Model("Anatomy/Muscles.fbx","Muscles",human.transform);Apply(muscles,muscle);
            var skull=Model("Anatomy/Skull.fbx","Skull",human.transform);Apply(skull,bone);
            var brainObj=Model("Anatomy/Brain.fbx","Brain",human.transform);Apply(brainObj,brain);
            human.transform.rotation=Quaternion.Euler(0,180,0);
            view.layers=new[]{skinObj,muscles,skull,brainObj};foreach(var layer in view.layers.Skip(1))layer.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(human,Root+"Prefabs/Human_Layered.prefab");
        }
        static void BuildEye(Transform parent){
            var eye=Model("Zip/Eye_Hero.fbx","Eye_Hero",parent);view.eye=eye.transform;
            var sphere=eye.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("Sclera"));float scale=.024f/Mathf.Max(sphere.bounds.size.x,sphere.bounds.size.y);eye.transform.localScale=Vector3.one*scale;eye.transform.position=new Vector3(.03f,1.63f,-.08f);
            var cornea=eye.GetComponentsInChildren<Renderer>(true).First(r=>r.name.StartsWith("Cornea"));
            var forward=cornea.bounds.center-sphere.bounds.center;
            Debug.Log("EYE_ALIGNMENT sclera="+sphere.bounds+" cornea="+cornea.bounds+" front="+forward);
            eye.transform.rotation=Quaternion.FromToRotation(forward.normalized,Vector3.back)*eye.transform.rotation;
            view.parts=new EyePart[4];
            foreach(var renderer in eye.GetComponentsInChildren<Renderer>(true)){
                var partName=renderer.name.Split('.')[0];var id=Array.FindIndex(Parts,p=>renderer.name.StartsWith(p,StringComparison.OrdinalIgnoreCase));
                if(id>=0)partName=char.ToUpper(Parts[id][0])+Parts[id].Substring(1);
                var colour=partName=="Iris"?new Color(.12f,.35f,.28f):partName=="Pupil"?new Color(.004f,.006f,.01f):Color.white;
                var mat=Mat(partName,colour);renderer.sharedMaterials=Enumerable.Repeat(mat,renderer.sharedMaterials.Length).ToArray();
                if(partName=="Sclera"){var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"SourceAssets/Zip/Arun/eyeparts/Eye_Diff.jpg");mat.SetTexture("_BaseMap",texture);mat.SetFloat("_Whiten",.9f);}
                if(partName=="Iris")mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"SourceAssets/Zip/Arun/eyeparts/FM_iris_Diff.png"));
                if(id<0){renderer.gameObject.SetActive(false);continue;}
                var part=renderer.gameObject.AddComponent<EyePart>();part.Id=Parts[id];part.VoiceId=11+id;part.surfaces=new Renderer[]{renderer};view.parts[id]=part;
                renderer.gameObject.layer=LayerMask.NameToLayer("EyePart");var collider=renderer.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=renderer is SkinnedMeshRenderer skinned?skinned.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
                mat.renderQueue=id==1?3000:2000;mat.SetFloat("_ZWrite",id==1?0:1);
                mat.SetFloat("_Cull",id==2||id==3?0:2);
                if(id==1)mat.SetFloat("_Alpha",.16f);
            }
            if(view.parts.Any(p=>p==null))throw new Exception("Eye must have all four independently selectable meshes. Imported: "+string.Join(", ",eye.GetComponentsInChildren<Renderer>(true).Select(r=>r.name))+"; missing: "+string.Join(", ",Parts.Where((p,i)=>!view.parts[i])));
            var second=UnityEngine.Object.Instantiate(eye,parent);second.name="Eye_L";second.transform.position=new Vector3(-.03f,1.63f,-.08f);foreach(var p in second.GetComponentsInChildren<EyePart>())UnityEngine.Object.DestroyImmediate(p);foreach(var c in second.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            second.transform.SetParent(view.human,true);
            PrefabUtility.SaveAsPrefabAsset(view.human.gameObject,Root+"Prefabs/Human_Layered.prefab");
            PrefabUtility.SaveAsPrefabAsset(eye,Root+"Prefabs/Eye_Hero.prefab");
        }
        static void BuildNiko(Transform parent){
            var niko=Model("Niko/Robot.fbx","Niko",parent);view.niko=niko.transform;Apply(niko,holo);
            var b=Bounds(niko);niko.transform.localScale=Vector3.one*(1.45f/b.size.y);niko.transform.position=new Vector3(-.8f,-b.min.y*niko.transform.localScale.y,0);
            view.nikoAnimator=niko.GetComponent<Animator>();if(!view.nikoAnimator)view.nikoAnimator=niko.AddComponent<Animator>();
            var controllerPath=Root+"Data/S02/Niko.controller";if(File.Exists(controllerPath))AssetDatabase.DeleteAsset(controllerPath);
            var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Talking",AnimatorControllerParameterType.Bool);
            var clips=AssetDatabase.LoadAllAssetsAtPath(Root+"SourceAssets/Niko/Robot.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
            foreach(var name in new[]{"Idle","Greet","Talk","RaiseHand","Pinch","Snap","Point","Turn"}){var state=controller.layers[0].stateMachine.AddState(name);state.motion=clips.FirstOrDefault(c=>c.name.IndexOf(name=="Greet"?"wave":name=="Idle"?"idle":name=="Talk"?"yes":name=="Turn"?"no":"thumb",StringComparison.OrdinalIgnoreCase)>=0)??clips.FirstOrDefault();if(name=="Idle")controller.layers[0].stateMachine.defaultState=state;}
            var states=controller.layers[0].stateMachine.states.Select(s=>s.state).ToArray();var idle=states.First(s=>s.name=="Idle");var talk=states.First(s=>s.name=="Talk");
            var toTalk=idle.AddTransition(talk);toTalk.hasExitTime=false;toTalk.duration=.25f;toTalk.AddCondition(AnimatorConditionMode.If,0,"Talking");
            var toIdle=talk.AddTransition(idle);toIdle.hasExitTime=false;toIdle.duration=.25f;toIdle.AddCondition(AnimatorConditionMode.IfNot,0,"Talking");
            foreach(var state in states.Where(s=>s!=idle&&s!=talk)){var t=state.AddTransition(idle);t.hasExitTime=true;t.exitTime=.9f;t.duration=.25f;}
            view.nikoAnimator.runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(niko,Root+"Prefabs/Niko.prefab");
        }
        static CinemachineCamera Cam(string name,Vector3 position,Vector3 target,float mm,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;go.transform.LookAt(target);var c=go.AddComponent<CinemachineCamera>();var lens=c.Lens;lens.FieldOfView=2*Mathf.Atan(24/(2*mm))*Mathf.Rad2Deg;lens.NearClipPlane=.005f;lens.FarClipPlane=100;c.Lens=lens;c.Priority=0;return c;}
        static TimelineAsset Timeline(string name,double duration,(double time,string beat,string action,int vo)[] cues){
            var asset=ScriptableObject.CreateInstance<TimelineAsset>();asset.durationMode=TimelineAsset.DurationMode.FixedLength;asset.fixedDuration=duration;
            SaveAsset(asset,Root+"Timeline/S02/TL_S02_"+name+".playable");
            var track=asset.CreateTrack<S02CueTrack>(null,"Beats and subtitles");
            for(int i=0;i<cues.Length;i++){
                var cue=cues[i];var clip=track.CreateClip<S02Cue>();clip.start=cue.Item1;clip.duration=(i+1<cues.Length?cues[i+1].time:duration)-cue.time;clip.displayName=cue.Item2+" "+cue.Item3;
                var data=((S02Cue)clip.asset).cue;data.beat=cue.Item2;data.action=cue.Item3;data.voice=cue.Item4;
            }
            var signals=asset.CreateTrack<SignalTrack>(null,"Completion gate");var signal=signals.CreateMarker<SignalEmitter>(duration-.05);signal.asset=completeSignal;signal.emitOnce=true;signal.retroactive=true;
            director.timeline.SetGenericBinding(signals,director.GetComponent<SignalReceiver>());
            var audio=asset.CreateTrack<AudioTrack>(null,"Niko VO");
            foreach(var cue in cues.Where(c=>c.vo>0)){var line=view.voice.manifest.Find(cue.vo);if(!line.clip)continue;var clip=audio.CreateClip<AudioPlayableAsset>();clip.start=cue.time;clip.duration=line.clip.length;clip.displayName="VO_2_"+cue.vo.ToString("00");((AudioPlayableAsset)clip.asset).clip=line.clip;}
            if(name is "Intro" or "Scan"){
                var animation=asset.CreateTrack<AnimationTrack>(null,"Camera motion");var clip=animation.CreateClip<AnimationPlayableAsset>();clip.start=0;clip.duration=name=="Intro"?8:6.5;
                var motion=new AnimationClip();motion.name=name+"_CameraMotion";motion.SetCurve(name=="Intro"?"VC_Establish":"VC_ScanFront",typeof(Transform),"m_LocalPosition.z",AnimationCurve.EaseInOut(0,name=="Intro"?-6:-4.7f,(float)clip.duration,name=="Intro"?-5.4f:-4.3f));SaveAsset(motion,Root+"Data/S02/"+motion.name+".anim");((AnimationPlayableAsset)clip.asset).clip=motion;
            }
            var shots=asset.CreateTrack<CinemachineTrack>(null,"Cinemachine shots");
            var shotCues=cues.Where(c=>CameraFor(c.action)!=null).ToArray();
            for(int i=0;i<shotCues.Length;i++){
                var c=shotCues[i];var clip=shots.CreateClip<CinemachineShot>();clip.start=c.time;
                clip.duration=(i+1<shotCues.Length?shotCues[i+1].time+BlendFor(shotCues[i+1].action):duration)-c.time;
                var shot=(CinemachineShot)clip.asset;shot.DisplayName=CameraFor(c.action);
                shot.VirtualCamera=new ExposedReference<CinemachineVirtualCameraBase>{exposedName=CameraFor(c.action)};
                if(i>0)clip.blendInDuration=BlendFor(c.action);
            }
            EditorUtility.SetDirty(asset);return asset;
        }
        static double BlendFor(string action)=>action switch{"niko"=>1.2,"human"=>1.5,"socket"=>1.5,"pinch"=>.8,"hero"=>2,_=>1};
        static string CameraFor(string action)=>action switch{
            "establish"=>"VC_Establish","niko"=>"VC_Niko","human"=>"VC_Human","prompt_scan"=>"VC_NikoHuman","scan"=>"VC_ScanFront","socket"=>"VC_EyeInSkull","pinch"=>"VC_NikoCU","hero"=>"VC_EyeHero","return"=>"VC_EyeHero","transition"=>"VC_EyeHero",_=>null};
        static AudioClip Tone(string name,float seconds,float frequency,bool ambient=false){
            var path=Root+(ambient?"Audio/Amb/":"Audio/SFX/")+name+".wav";
            int count=(int)(48000*seconds);using(var w=new BinaryWriter(File.Create(path))){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(48000);w.Write(96000);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);for(int i=0;i<count;i++){float t=i/48000f;float envelope=ambient?.16f:Mathf.Sin(Mathf.PI*i/count)*.3f;w.Write((short)(Mathf.Sin(t*frequency*Mathf.PI*2)*envelope*short.MaxValue));}}
            AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static GameObject Geometry(string name,Vector3 position,Vector3 size,Material material){
            // Generated architectural box mesh, saved as a real asset; no runtime placeholder primitives.
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));var mesh=new Mesh();var v=new List<Vector3>();var tris=new List<int>();
            var corners=new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
            var faces=new[]{0,3,2,1,4,5,6,7,0,4,7,3,1,2,6,5,0,1,5,4,3,7,6,2};
            for(int f=0;f<6;f++){for(int i=0;i<4;i++)v.Add(Vector3.Scale(corners[faces[f*4+i]],size*.5f));int n=f*4;tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();var path=Root+"Data/S02/"+name+".asset";SaveAsset(mesh,path);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.transform.position=position;return go;
        }
        static void BuildEnvironment(Transform parent){
            var env=new GameObject("Observatory_of_Vision").transform;env.SetParent(parent);
            Geometry("ObservatoryFloor",new Vector3(0,-.12f,0),new Vector3(16,.2f,16),navy).transform.SetParent(env);
            Geometry("Platform",new Vector3(0,-.015f,0),new Vector3(2.5f,.08f,2.5f),brass).transform.SetParent(env);
            Dome(env);
            for(int i=0;i<24;i++){
                float a=i*Mathf.PI*2/24;var rib=Geometry("DomeRib_"+i,new Vector3(Mathf.Cos(a)*6,2.5f,Mathf.Sin(a)*6),new Vector3(.07f,5,.07f),brass);rib.transform.SetParent(env);
            }
            for(int i=0;i<8;i++){var p=Model("Zip/Extra/"+(i%2==0?"ConvexLens":"Concave_Lens")+".fbx","OpticalLens_"+i,env);var b=Bounds(p);p.transform.localScale=Vector3.one*(.45f/Mathf.Max(b.size.x,b.size.y,b.size.z));p.transform.position=new Vector3(i<4?-2.6f:2.6f,.8f,(i%4)*1.1f-.8f);Apply(p,cyan);Geometry("LensStand_"+i,p.transform.position+Vector3.down*.5f,new Vector3(.07f,.6f,.07f),brass).transform.SetParent(env);}
            var beamMat=Mat("Starlight",new Color(.08f,.45f,.65f),"VisionLab/Beam");beamMat.SetFloat("_Alpha",.08f);
            Tube("StarlightBeam",env,new Vector3(0,2.9f,0),.4f,30,beamMat);
            var dust=new GameObject("BeamDust",typeof(ParticleSystem)).GetComponent<ParticleSystem>();dust.transform.SetParent(env);dust.transform.position=new Vector3(0,2.9f,0);
            var main=dust.main;main.startLifetime=8;main.startSize=.009f;main.startSpeed=.05f;main.maxParticles=2000;main.startColor=new Color(.45f,.8f,1,.25f);
            var emission=dust.emission;emission.rateOverTime=30;var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(30,.8f,.8f);dust.GetComponent<ParticleSystemRenderer>().sharedMaterial=beamMat;
            Light("Key",new Vector3(-3,4,-3),new Vector3(30,35,0),Color.white,2,LightType.Directional);
            Light("EyeFill",new Vector3(-.8f,2,-2),Vector3.zero,new Color(.4f,.7f,1),4,LightType.Point);
            var volume=new GameObject("Post",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;var profile=ScriptableObject.CreateInstance<VolumeProfile>();SaveAsset(profile,Root+"Data/S02/Post.asset");profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);var bloom=profile.Add<Bloom>();bloom.threshold.Override(1.1f);bloom.intensity.Override(.6f);profile.Add<Vignette>().intensity.Override(.25f);foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(profile);volume.sharedProfile=profile;
            var probe=new GameObject("Hero Reflection",typeof(ReflectionProbe)).GetComponent<ReflectionProbe>();probe.transform.position=new Vector3(0,1.55f,-1.5f);probe.size=Vector3.one*5;probe.resolution=128;
        }
        static void Dome(Transform parent){
            int segments=64,rings=16;var mesh=new Mesh();var vs=new List<Vector3>();var ts=new List<int>();
            for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++){float elevation=y*Mathf.PI*.5f/rings,a=x*Mathf.PI*2/segments;vs.Add(new Vector3(Mathf.Cos(a)*Mathf.Cos(elevation)*7,Mathf.Sin(elevation)*6,Mathf.Sin(a)*Mathf.Cos(elevation)*7));}
            for(int y=0;y<rings;y++)for(int x=0;x<segments;x++){int n=y*(segments+1)+x;ts.AddRange(new[]{n,n+segments+1,n+1,n+1,n+segments+1,n+segments+2});}
            mesh.SetVertices(vs);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();SaveAsset(mesh,Root+"Data/S02/ObservatoryDome.asset");var dome=new GameObject("ObservatoryDome",typeof(MeshFilter),typeof(MeshRenderer));dome.transform.SetParent(parent);dome.GetComponent<MeshFilter>().sharedMesh=mesh;dome.GetComponent<MeshRenderer>().sharedMaterial=navy;
            var random=new System.Random(2);for(int i=0;i<90;i++){float a=(float)random.NextDouble()*Mathf.PI*2,e=.25f+(float)random.NextDouble()*1.1f;var p=new Vector3(Mathf.Cos(a)*Mathf.Cos(e)*6.8f,Mathf.Sin(e)*5.8f,Mathf.Sin(a)*Mathf.Cos(e)*6.8f);Geometry("Star_"+i,p,Vector3.one*.025f,holo).transform.SetParent(parent);}
        }
        static void Tube(string name,Transform parent,Vector3 position,float radius,float length,Material material){
            var mesh=new Mesh();var vs=new List<Vector3>();var ts=new List<int>();for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;vs.Add(new Vector3(-length*.5f,Mathf.Cos(a)*radius,Mathf.Sin(a)*radius));vs.Add(new Vector3(length*.5f,Mathf.Cos(a)*radius,Mathf.Sin(a)*radius));}for(int i=0;i<32;i++){int n=i*2;ts.AddRange(new[]{n,n+1,n+2,n+1,n+3,n+2});}mesh.SetVertices(vs);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();SaveAsset(mesh,Root+"Data/S02/"+name+".asset");var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent);obj.transform.position=position;obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        static void Light(string name,Vector3 position,Vector3 rotation,Color colour,float intensity,LightType type){var l=new GameObject(name,typeof(Light)).GetComponent<Light>();l.transform.position=position;l.transform.eulerAngles=rotation;l.type=type;l.color=colour;l.intensity=intensity;l.range=8;}
        static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax){var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=offsetMin;rt.offsetMax=offsetMax;return rt;}
        static TMP_Text Text(string name,Transform parent,string text,float size){var rt=Rect(name,parent,Vector2.zero,Vector2.one,new Vector2(10,5),new Vector2(-10,-5));var label=rt.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSharedMaterial=fontMaterial;label.text=text;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.color=Color.white;label.raycastTarget=false;return label;}
        static Image Panel(RectTransform rt,Color colour){var image=rt.gameObject.AddComponent<Image>();image.color=colour;return image;}
        static Button Button(string name,Transform parent,string text){var rt=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Panel(rt,new Color(.035f,.12f,.18f,.95f));var b=rt.gameObject.AddComponent<Button>();var colors=b.colors;colors.highlightedColor=new Color(.3f,.7f,1);colors.selectedColor=colors.highlightedColor;b.colors=colors;Text(name+"Label",rt,text,24);return b;}
        static void At(RectTransform rt,Vector2 min,Vector2 max,Vector2 lo,Vector2 hi){rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=lo;rt.offsetMax=hi;}
        static void Row(Transform parent){var layout=parent.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=10;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=true;layout.childForceExpandWidth=true;}
        static Toggle Toggle(string name,Transform parent,string label,bool value){var rt=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Panel(rt,new Color(.04f,.16f,.23f));var t=rt.gameObject.AddComponent<Toggle>();var check=Rect("Check",rt,new Vector2(0,0),new Vector2(0,1),new Vector2(5,15),new Vector2(35,-15));t.graphic=Panel(check,Color.cyan);t.isOn=value;Text(name+"Label",rt,label,22);return t;}
        static Slider Slider(string name,Transform parent){var rt=Rect(name,parent,Vector2.zero,Vector2.one,new Vector2(15,15),new Vector2(-15,-15));Panel(rt,new Color(.04f,.16f,.23f));var s=rt.gameObject.AddComponent<Slider>();var fill=Rect("Fill",rt,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Panel(fill,Color.cyan);s.fillRect=fill;s.value=1;return s;}
        static void BuildUI(Transform parent){
            var canvas=new GameObject("HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(parent);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var safe=Rect("SafeArea",canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);safe.gameObject.AddComponent<SafeAreaFitter>();
            var title=Rect("Title",safe,new Vector2(0,.88f),new Vector2(.4f,1),new Vector2(30,0),new Vector2(0,-25));Text("TitleText",title,"OBSERVATORY OF VISION\n02 / MEET NIKO",26);
            var prompt=Rect("Prompt",safe,new Vector2(.25f,.8f),new Vector2(.75f,.89f),Vector2.zero,Vector2.zero);view.prompt=Text("PromptText",prompt,"",32);
            var subtitle=Rect("Subtitle",safe,new Vector2(.05f,.19f),new Vector2(.95f,.34f),Vector2.zero,Vector2.zero);Panel(subtitle,new Color(0,0,0,.4f));Text("Subtitles",subtitle,"",34);
            var chips=Rect("PartChips",safe,new Vector2(.24f,.11f),new Vector2(.76f,.18f),Vector2.zero,Vector2.zero);Row(chips);view.chipBar=chips.gameObject;view.chips=Parts.Select(p=>Button(p, chips,char.ToUpper(p[0])+p.Substring(1))).ToArray();
            var progress=Rect("Progress",safe,new Vector2(.4f,.05f),new Vector2(.6f,.1f),Vector2.zero,Vector2.zero);view.progress=Text("ProgressText",progress,"0 / 4 explored",22);
            var mainSlot=Rect("MainAction",safe,new Vector2(.81f,.03f),new Vector2(.98f,.11f),Vector2.zero,Vector2.zero);view.scan=Button("Scan",mainSlot,"SCAN");view.inside=Button("InsideView",mainSlot,"INSIDE VIEW");
            var fallback=Rect("ContinueSlot",safe,new Vector2(.64f,.03f),new Vector2(.79f,.11f),Vector2.zero,Vector2.zero);view.continueButton=Button("Continue",fallback,"Continue");
            var ask=Rect("AskNiko",safe,new Vector2(.02f,.03f),new Vector2(.38f,.11f),Vector2.zero,Vector2.zero);Panel(ask,new Color(.02f,.09f,.14f,.96f));view.askBar=ask.gameObject;
            var input=Rect("Question",ask,Vector2.zero,new Vector2(.77f,1),new Vector2(10,4),new Vector2(-10,-4));var text=Text("InputText",input,"",24);view.question=input.gameObject.AddComponent<TMP_InputField>();view.question.textViewport=input;view.question.textComponent=(TextMeshProUGUI)text;view.question.characterLimit=240;view.question.placeholder=Text("Placeholder",input,"Ask Niko about a part…",22);((TMP_Text)view.question.placeholder).color=new Color(.7f,.8f,.84f);
            var send=Rect("SendSlot",ask,new Vector2(.78f,0),Vector2.one,Vector2.zero,Vector2.zero);view.send=Button("Send",send,"Send");
            var controls=Rect("Controls",safe,new Vector2(.47f,.91f),new Vector2(.98f,.98f),Vector2.zero,Vector2.zero);Row(controls);view.skip=Button("Skip",controls,"Skip");view.replay=Button("Replay",controls,"Replay");view.pause=Button("Pause",controls,"Pause");var settings=Button("Settings",controls,"Settings");
            var settingsPanel=Rect("SettingsPanel",safe,new Vector2(.54f,.38f),new Vector2(.97f,.87f),Vector2.zero,Vector2.zero);Panel(settingsPanel,new Color(.02f,.05f,.09f,.98f));view.settings=settingsPanel.gameObject;
            var list=settingsPanel.gameObject.AddComponent<VerticalLayoutGroup>();list.spacing=8;list.padding=new RectOffset(15,15,15,15);list.childControlHeight=true;list.childForceExpandHeight=true;list.childForceExpandWidth=true;
            view.captions=Toggle("Captions",settingsPanel,"Captions",true);view.reducedMotion=Toggle("ReducedMotion",settingsPanel,"Reduced motion",false);
            Text("FontLabel",settingsPanel,"Subtitle size  S — M — L",22);view.fontSize=Slider("FontSize",settingsPanel);view.fontSize.maxValue=2;view.fontSize.wholeNumbers=true;view.fontSize.value=1;
            Text("VOLabel",settingsPanel,"Voice volume",22);view.voVolume=Slider("VoiceVolume",settingsPanel);Text("AmbLabel",settingsPanel,"Ambience volume",22);view.ambienceVolume=Slider("AmbienceVolume",settingsPanel);view.ambienceVolume.value=.22f;
            var about=Button("About",settingsPanel,"About / credits");about.onClick.AddListener(()=>{});
            settings.onClick.AddListener(()=>{}); // Runtime listener is installed by HUDSettings below.
            var hud=canvas.AddComponent<HUDSettings>();hud.settingsButton=settings;hud.panel=settingsPanel.gameObject;hud.aboutButton=about;
            settingsPanel.gameObject.SetActive(false);
            var pausePanel=Rect("Paused",safe,new Vector2(.33f,.43f),new Vector2(.67f,.57f),Vector2.zero,Vector2.zero);Panel(pausePanel,new Color(0,0,0,.8f));Text("PausedLabel",pausePanel,"PAUSED\nPress Pause to resume",30);view.pauseOverlay=pausePanel.gameObject;pausePanel.gameObject.SetActive(false);
            var status=Rect("Status",safe,new Vector2(.2f,.36f),new Vector2(.8f,.43f),Vector2.zero,Vector2.zero);view.status=Text("StatusText",status,"",26);
            var fade=Rect("EntryFade",canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);view.fade=Panel(fade,Color.white);view.fade.raycastTarget=false;
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        }
        public static void BuildAll(){Build();BuildWindows();}
        public static void BuildAndroid(){
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.Vulkan});
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.metabookxr.humaneye");
            EditorUserBuildSettings.androidBuildSubtarget=MobileTextureSubtarget.ASTC;
            var result=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),"Builds/Android/MeetNiko.apk",BuildTarget.Android,BuildOptions.Development);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Android build failed: "+result.summary.result);
        }
        public static void BuildWindows(){var result=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),"Builds/Windows/MeetNiko.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed: "+result.summary.result);}
    }
}
