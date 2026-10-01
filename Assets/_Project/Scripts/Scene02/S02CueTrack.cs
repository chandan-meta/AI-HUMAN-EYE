using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace VisionLab {
    [TrackClipType(typeof(S02Cue)), TrackBindingType(typeof(S02_Director))]
    public class S02CueTrack : TrackAsset {
        public override Playable CreateTrackMixer(PlayableGraph graph,GameObject go,int inputCount)=>ScriptPlayable<S02CueMixer>.Create(graph,inputCount);
    }
}
