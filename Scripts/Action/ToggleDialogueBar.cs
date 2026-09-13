using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class ToggleDialogueBar : INarrativeAction
    {
        public bool setActive;

        public string Label =>
            setActive ? "ToggleDialogueBar (Show)" : "ToggleDialogueBar (Hide)";

        public UniTask ExecuteAsync(CancellationToken ct = default)
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
