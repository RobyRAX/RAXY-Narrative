using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    [TrackColor(0.855f, 0.862f, 0.870f)]
    [TrackClipType(typeof(CutsceneDialogueClip))]
    public class CutsceneDialogueTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<CutsceneDialogueMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
