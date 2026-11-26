using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RAXY.Narrative
{
    [Serializable]
    [MovedFrom(true, "", "Assembly-CSharp", "PlayBarkDialogue")]
    public class PlayBarkDialogue : NarrativeActionBase
    {
        [TitleGroup("Target")]
        [ValueDropdown(nameof(GetBarkBubbleIds), AppendNextDrawer = true)]
        public string bubbleId = "Hero";

        [TitleGroup("Dialogue Source")]
        public BarkDialogueSourceType sourceType = BarkDialogueSourceType.DialogueCollectionSO;

        [TitleGroup("Dialogue Source")]
        [ShowIf("@sourceType == BarkDialogueSourceType.DialogueCollectionSO")]
        public DialogueCollectionSO dialogueCollectionSO;

        [TitleGroup("Dialogue Source")]
        [ShowIf("@sourceType == BarkDialogueSourceType.BanterDialogue")]
        public BanterDialogueDataSO banterDialogueDataSO;

        [TitleGroup("Dialogue Source")]
        [ShowIf("@sourceType == BarkDialogueSourceType.FullscreenDialogueCollection")]
        public FullscreenDialogueDataSO fullscreenDialogueDataSO;

        [TitleGroup("Dialogue Source")]
        [ShowIf("@sourceType == BarkDialogueSourceType.FullscreenDialogueCollection")]
        [ValueDropdown(nameof(GetCollectionIds), AppendNextDrawer = true)]
        public string collectionId;

        [TitleGroup("Delay Settings")]
        [Tooltip("Override line end delay. If disabled, uses BarkDialogueManager default.")]
        public bool overrideLineEndDelay;

        [TitleGroup("Delay Settings")]
        [ShowIf(nameof(overrideLineEndDelay))]
        [Min(0f)]
        [Tooltip("Delay in seconds applied after each line finishes typing.")]
        public float lineEndDelay = 0.5f;

        public override string Label => sourceType switch
        {
            BarkDialogueSourceType.BanterDialogue => banterDialogueDataSO != null
                ? $"PlayBarkDialogue ({bubbleId}: {banterDialogueDataSO.name})"
                : $"PlayBarkDialogue ({bubbleId}: Banter)",
            BarkDialogueSourceType.FullscreenDialogueCollection => !string.IsNullOrWhiteSpace(collectionId)
                ? $"PlayBarkDialogue ({bubbleId}: {collectionId})"
                : $"PlayBarkDialogue ({bubbleId}: Fullscreen Collection)",
            _ => dialogueCollectionSO != null
                ? $"PlayBarkDialogue ({bubbleId}: {dialogueCollectionSO.name})"
                : $"PlayBarkDialogue ({bubbleId}: Dialogue Collection)"
        };

        public override async UniTask ExecuteAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var manager = BarkDialogueManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[PlayBarkDialogue] BarkDialogueManager is not available.");
                return;
            }

            if (string.IsNullOrWhiteSpace(bubbleId))
            {
                Debug.LogWarning("[PlayBarkDialogue] bubbleId is empty.");
                return;
            }

            var collection = ResolveDialogueCollection();
            if (collection == null)
            {
                Debug.LogWarning("[PlayBarkDialogue] Dialogue collection could not be resolved.");
                return;
            }

            var autoNextMode = sourceType == BarkDialogueSourceType.DialogueCollectionSO
                ? BarkAutoNextMode.CollectionChar
                : BarkAutoNextMode.Line;

            float? customDelay = overrideLineEndDelay ? lineEndDelay : null;
            await manager.PlayDialogueAsync(bubbleId, collection, autoNextMode, customDelay, ct);
        }

        IDialogueCollection ResolveDialogueCollection()
        {
            switch (sourceType)
            {
                case BarkDialogueSourceType.DialogueCollectionSO:
                    return dialogueCollectionSO != null ? dialogueCollectionSO.dialogueCollection : null;

                case BarkDialogueSourceType.BanterDialogue:
                    return banterDialogueDataSO != null ? banterDialogueDataSO.dialogueCollection : null;

                case BarkDialogueSourceType.FullscreenDialogueCollection:
                    if (fullscreenDialogueDataSO == null || string.IsNullOrWhiteSpace(collectionId))
                        return null;

                    return fullscreenDialogueDataSO.dialogueCollections?.Find(
                        collection => collection != null && collection.DialogueCollectionId == collectionId);

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
