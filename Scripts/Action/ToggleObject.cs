using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class ToggleObject : NarrativeActionBase
    {
        [HideLabel]
        public NarrativeObjectTarget target = new();

        public bool setActive;

        public override string Label =>
            $"ToggleObject ({(target != null ? target.TargetDescription : "None")}: {(setActive ? "Active" : "Inactive")})";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var t = target?.Resolve();
            if (t == null)
            {
                Debug.LogWarning("[ToggleObject] Skipping toggle because target could not be resolved.");
                return UniTask.CompletedTask;
            }

            t.gameObject.SetActive(setActive);
            return UniTask.CompletedTask;
        }
    }
}
