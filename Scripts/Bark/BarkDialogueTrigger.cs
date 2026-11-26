using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace RAXY.Narrative
{
    public enum BarkDialogueSourceType
    {
        DialogueCollectionSO = 0,
        BanterDialogue = 1,
        FullscreenDialogueCollection = 2,
    }

    [DisallowMultipleComponent]
    public class BarkDialogueTrigger : MonoBehaviour
    {
        [TitleGroup("Target")]
        [ValueDropdown(nameof(GetBarkBubbleIds), AppendNextDrawer = true)]
        public string bubbleId = "Hero";

        [TitleGroup("Dialogue Source")]
        public BarkDialogueSourceType sourceType = BarkDialogueSourceType.DialogueCollectionSO;

        [TitleGroup("Dialogue Source")]
        [ShowIf("@sourceType == BarkDialogueSourceType.BanterDialogue")]
        public BanterDialogueDataSO banterDialogueDataSO;

        [TitleGroup("Dialogue Source")]
        [ShowIf("@sourceType == BarkDialogueSourceType.FullscreenDialogueCollection")]
        public FullscreenDialogueDataSO fullscreenDialogueDataSO;

        [TitleGroup("Dialogue Source")]
        [ShowIf("@sourceType == BarkDialogueSourceType.FullscreenDialogueCollection")]
        [ValueDropdown(nameof(GetCollectionIds), AppendNextDrawer = true)]
        [Tooltip("Collection ID inside FullscreenDialogueDataSO (same as NarrativeTrigger.collectionId).")]
        public string collectionId;

        [TitleGroup("Dialogue Source")]
        [ShowIf("@sourceType == BarkDialogueSourceType.DialogueCollectionSO")]
        public DialogueCollectionSO dialogueCollectionSO;

        [TitleGroup("Delay Settings")]
        [Tooltip("Override line end delay. If disabled, uses BarkDialogueManager default.")]
        public bool overrideLineEndDelay;

        [TitleGroup("Delay Settings")]
        [ShowIf(nameof(overrideLineEndDelay))]
        [Min(0f)]
        [Tooltip("Delay in seconds applied after each line finishes typing.")]
        public float lineEndDelay = 0.5f;

        [TitleGroup("Events")]
        public UnityEvent onComplete;

        public void Trigger()
        {
            TriggerAsync().Forget();
        }

        public void EndBark()
        {
            if (string.IsNullOrWhiteSpace(bubbleId))
                return;

            BarkDialogueManager.Instance?.EndDialogue(bubbleId);
        }

        async UniTaskVoid TriggerAsync()
        {
            if (string.IsNullOrWhiteSpace(bubbleId))
            {
                Debug.LogWarning("[BarkDialogueTrigger] bubbleId is empty.", this);
                return;
            }

            var collection = ResolveDialogueCollection();
            if (collection == null)
            {
                Debug.LogWarning("[BarkDialogueTrigger] Dialogue collection could not be resolved.", this);
                return;
            }

            var manager = BarkDialogueManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[BarkDialogueTrigger] BarkDialogueManager is not available.", this);
                return;
            }

            var autoNextMode = sourceType == BarkDialogueSourceType.DialogueCollectionSO
                ? BarkAutoNextMode.CollectionChar
                : BarkAutoNextMode.Line;

            float? customDelay = overrideLineEndDelay ? lineEndDelay : null;
            await manager.PlayDialogueAsync(bubbleId, collection, autoNextMode, customDelay, destroyCancellationToken);
            onComplete?.Invoke();
        }

        IDialogueCollection ResolveDialogueCollection()
        {
            switch (sourceType)
            {
                case BarkDialogueSourceType.BanterDialogue:
                    return banterDialogueDataSO != null ? banterDialogueDataSO.dialogueCollection : null;

                case BarkDialogueSourceType.FullscreenDialogueCollection:
                    if (fullscreenDialogueDataSO == null || string.IsNullOrWhiteSpace(collectionId))
                        return null;

                    return fullscreenDialogueDataSO.dialogueCollections?.Find(
                        c => c != null && c.DialogueCollectionId == collectionId);

                case BarkDialogueSourceType.DialogueCollectionSO:
                    return dialogueCollectionSO != null ? dialogueCollectionSO.dialogueCollection : null;

                default:
                    return null;
            }
        }

#if UNITY_EDITOR
        IEnumerable<string> GetBarkBubbleIds() => BarkBubbleEditorIdProvider.GetDropdownIds();

        IEnumerable<ValueDropdownItem<string>> GetCollectionIds()
        {
            if (fullscreenDialogueDataSO?.dialogueCollections == null)
                yield break;

            foreach (var collection in fullscreenDialogueDataSO.dialogueCollections)
            {
                if (collection == null || string.IsNullOrWhiteSpace(collection.DialogueCollectionId))
                    continue;

                var id = collection.DialogueCollectionId;
                yield return new ValueDropdownItem<string>(id, id);
            }
        }
#else
        IEnumerable<string> GetBarkBubbleIds() => null;
        IEnumerable<ValueDropdownItem<string>> GetCollectionIds() => null;
#endif
    }
}
