using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class Wait : NarrativeActionBase
    {
        [MinValue(0f)]
        [SuffixLabel("s", true)]
        [Tooltip("Duration to wait in seconds.")]
        public float duration;

        public override string Label => $"Wait ({duration}s)";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            if (duration <= 0f)
                return UniTask.CompletedTask;

            return UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: ct);
        }
    }
}
