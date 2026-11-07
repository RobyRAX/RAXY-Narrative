using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RAXY.Event;
using Sirenix.OdinInspector;

namespace RAXY.Narrative
{
    [Serializable]
    public class TriggerEventSO : NarrativeActionBase
    {
        [HideLabel]
        [HideReferenceObjectPicker]
        public EventSoRaiser eventRaiser;

        public override string Label => "TriggerEventSO";

        public override UniTask ExecuteAsync(CancellationToken ct = default)
        {
            eventRaiser?.Raise();
            return UniTask.CompletedTask;
        }
    }
}
