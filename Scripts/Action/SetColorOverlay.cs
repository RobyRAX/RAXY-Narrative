using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class SetColorOverlay : NarrativeActionBase
    {
        public Color color = Color.black;

        [MinValue(0f)]
        [SuffixLabel("s", true)]
        public float fadeDuration = 0.33f;

        public bool force;

        [Tooltip("Wait for the fade duration before starting the next action.")]
        public bool waitForFade = true;

        public override string Label => $"SetColorOverlay ({color}, {fadeDuration}s)";

        public override async UniTask ExecuteAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var manager = ColorOverlayManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[SetColorOverlay] ColorOverlayManager.Instance is null.");
                return;
            }

            float duration = Mathf.Max(0f, fadeDuration);
            manager.SetColorOverlay(color, duration, force);

            if (waitForFade && duration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(duration), true, cancellationToken: ct);
        }
    }
}
