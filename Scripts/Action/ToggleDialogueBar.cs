using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class ToggleDialogueBar : NarrativeActionBase
    {
        public bool setActive;

        public override string Label =>
            setActive ? "ToggleDialogueBar (Show)" : "ToggleDialogueBar (Hide)";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            var hub = NarrativeHubManager.Instance;
            if (hub?.FullscreenDialogueView == null)
            {
                Debug.LogWarning("[ToggleDialogueBar] FullscreenDialogueView is not available.");
                return UniTask.CompletedTask;
            }

            if (setActive)
                hub.FullscreenDialogueView.ShowDialogueBar();
            else
                hub.FullscreenDialogueView.HideDialogueBar();

            return UniTask.CompletedTask;
        }
    }
}
