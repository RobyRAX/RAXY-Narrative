using System;

namespace RAXY.Narrative
{
    [Serializable]
    public class PlayheadRegionStatus
    {
        public double start;
        public double end;
        public CutsceneDialogueMode mode;
        public CutsceneDialogueTriggerTime triggerTime;
        public float startOffset;
        public FullscreenDialogueDataSO dialogueSO;
        public string dialogueCollectionId;
        public bool isDialogueRegion = true;
        public bool isPlayed;

        public double GetTriggerTime()
        {
            return triggerTime switch
            {
                CutsceneDialogueTriggerTime.Middle => (start + end) * 0.5,
                CutsceneDialogueTriggerTime.End => end,
                _ => Math.Max(0d, start - startOffset)
            };
        }
    }
}
