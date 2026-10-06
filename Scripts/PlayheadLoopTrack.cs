using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    [TrackColor(0.2f, 0.65f, 0.45f)]
    [TrackClipType(typeof(PlayheadLoopClip))]
    public class PlayheadLoopTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<PlayheadLoopMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
