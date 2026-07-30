using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    public enum CutsceneDialogueMode
    {
        Pause,
        PingPong,
        Repeat
    }

    public enum CutsceneDialogueTriggerTime 
    {
        Start,
        Middle,
        End
    }

    [Serializable]
    public class CutsceneDialogueClip : PlayableAsset, ITimelineClipAsset
    {
        public FullscreenDialogueDataSO dialogueSO;
        public string dialogueCollectionId;
        public CutsceneDialogueMode mode = CutsceneDialogueMode.Repeat;
        public CutsceneDialogueTriggerTime triggerTime = CutsceneDialogueTriggerTime.Start;
        CutsceneDialogueBehaviour template = new CutsceneDialogueBehaviour();

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<CutsceneDialogueBehaviour>.Create(graph, template);
        }
    }
}
