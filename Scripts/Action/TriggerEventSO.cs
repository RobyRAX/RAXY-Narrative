using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using RAXY.Event;
using Sirenix.OdinInspector;

namespace RAXY.Narrative
{
    [Serializable]
    public class TriggerEventSO : INarrativeAction
    {
        [HideLabel]
        [HideReferenceObjectPicker]
        public EventSoRaiser eventRaiser;

        public string Label => "TriggerEventSO";

        public UniTask ExecuteAsync(CancellationToken ct = default)
        {
            eventRaiser?.Raise();
            return UniTask.CompletedTask;
        }
    }
}
