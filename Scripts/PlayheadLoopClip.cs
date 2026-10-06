using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    [Serializable]
    public class PlayheadLoopClip : PlayableAsset, ITimelineClipAsset
    {
        public CutsceneDialogueMode mode = CutsceneDialogueMode.Repeat;
        PlayheadLoopBehaviour template = new PlayheadLoopBehaviour();

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<PlayheadLoopBehaviour>.Create(graph, template);
        }
    }
}
