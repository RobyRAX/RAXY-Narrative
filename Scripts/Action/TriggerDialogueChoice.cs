using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class TriggerDialogueChoice : INarrativeAction
    {
        [ListDrawerSettings(ShowIndexLabels = true, Expanded = true, ListElementLabelName = "Label")]
        public List<DialogueChoiceEntry> choiceEntries = new();

        public string Label => "TriggerDialogueChoice";

        public async UniTask ExecuteAsync(CancellationToken ct = default)
        {
            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[TriggerDialogueChoice] NarrativeHubManager.Instance is null.");
                return;
            }

            if (hub.DialogueChoiceView == null)
            {
                Debug.LogWarning("[TriggerDialogueChoice] DialogueChoiceView belum di-assign — di-skip.");
                return;
            }

            if (choiceEntries == null || choiceEntries.Count == 0)
            {
                Debug.LogWarning("[TriggerDialogueChoice] choiceEntries kosong.");
                return;
            }

            int selectedIndex = await hub.PlayDialogueChoiceAsync(choiceEntries, ct);
            if (selectedIndex < 0 || selectedIndex >= choiceEntries.Count)
                return;

            var choice = choiceEntries[selectedIndex];
            if (choice?.narrativeActions == null || choice.narrativeActions.Count == 0)
            {
                Debug.LogWarning(
                    $"[TriggerDialogueChoice] Choice index {selectedIndex} tidak punya narrativeActions.");
                return;
            }

            // Jangan ikat ke token dialogue outer — nested PlayTimelineCutscene bisa
            // memicu PlayFullscreenDialogue baru yang cancel _playCts lama.
            await hub.Process_NarrativeActionsAsync(choice.narrativeActions, CancellationToken.None);
        }
    }
}
