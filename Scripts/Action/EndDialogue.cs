using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class EndDialogue : NarrativeActionBase
    {
        public override string Label => "EndDialogue";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            var hub = NarrativeHubManager.Instance;
            if (hub?.FullscreenDialogueView == null)
            {
                Debug.LogWarning("[EndDialogue] FullscreenDialogueView is not available.");
                return UniTask.CompletedTask;
            }

            hub.FullscreenDialogueView.EndDialogue();
            return UniTask.CompletedTask;
        }
    }
}
