using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class SetTextOverlay : NarrativeActionBase
    {
        public string text = "";

        public Color color = Color.white;

        [MinValue(0f)]
        [SuffixLabel("s", true)]
        public float fadeDuration = 0.33f;

        public bool force;

        [Tooltip("Wait for the fade duration before starting the next action.")]
        public bool waitForFade = true;

        public override string Label => $"SetTextOverlay ({text}, {fadeDuration}s)";

        public override async UniTask ExecuteAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var manager = TextOverlayManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[SetTextOverlay] TextOverlayManager.Instance is null.");
                return;
            }

            float duration = Mathf.Max(0f, fadeDuration);
            manager.SetTextOverlay(text, color, duration, force);

            if (waitForFade && duration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(duration), true, cancellationToken: ct);
        }
    }
}
