using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class PlayDialogue : INarrativeAction
    {
        public FullscreenDialogueDataSO dialogueSO;

        [ValueDropdown(nameof(CollectionIds))]
        public string collectionId;

        public string Label =>
            dialogueSO != null
                ? $"PlayDialogue ({dialogueSO.name})"
                : "PlayDialogue";

        public async UniTask ExecuteAsync(CancellationToken ct = default)
        {
            if (dialogueSO == null)
            {
                Debug.LogWarning("[PlayDialogue] dialogueSO is not set.");
                return;
            }

            var hub = NarrativeHubManager.Instance;
            if (hub == null)
            {
                Debug.LogWarning("[PlayDialogue] NarrativeHubManager.Instance is null.");
                return;
            }

            // Jangan teruskan token playthrough lama.
            // Nested PlayAsync akan cancel _playCts outer; kalau token itu di-link ke play baru,
            // play baru langsung cancelled dan stuck.
            await hub.PlayFullscreenDialogueAsync(dialogueSO, collectionId);
        }

#if UNITY_EDITOR
        List<string> CollectionIds => dialogueSO?.CollectionIds;
#else
        List<string> CollectionIds => null;
#endif
    }
}
