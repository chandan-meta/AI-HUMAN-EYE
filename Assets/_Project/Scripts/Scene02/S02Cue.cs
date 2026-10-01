using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace VisionLab {
    [Serializable] public class S02CueBehaviour : PlayableBehaviour {
        public string beat, action;
        public int voice;
        bool fired;
        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            if(playerData is S02_Director director)Fire(director);
        }
        public void Fire(S02_Director director){if(fired || !Application.isPlaying)return;fired=true;director.Cue(beat,action,voice);}
    }
    public class S02Cue : PlayableAsset {
        public S02CueBehaviour cue = new();
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => ScriptPlayable<S02CueBehaviour>.Create(graph, cue);
    }
    public class S02CueMixer : PlayableBehaviour {
        public override void ProcessFrame(Playable playable,FrameData info,object playerData){
            if(!(playerData is S02_Director director))return;
            for(int i=0;i<playable.GetInputCount();i++)if(playable.GetInputWeight(i)>0)((ScriptPlayable<S02CueBehaviour>)playable.GetInput(i)).GetBehaviour().Fire(director);
        }
    }
}
