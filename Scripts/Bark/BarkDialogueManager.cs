using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RAXY.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RAXY.Narrative
{
    public enum BarkAutoNextMode
    {
        Line = 0,
        CollectionChar = 1,
    }

    [DisallowMultipleComponent]
    public class BarkDialogueManager : Singleton<BarkDialogueManager>
    {
        const float DefaultAutoNextDuration = 5f;

        [TitleGroup("Dialogue Collection")]
        [Min(0f)]
        public float collectionCharDelay = 0.03f;

        [FormerlySerializedAs("collectionEndDelay")]
        [Min(0f)]
        [Tooltip("Default delay added after each line finishes typing before advancing or hiding.")]
        public float lineEndDelay = 0.5f;

        readonly Dictionary<BarkBubble, CancellationTokenSource> _playCtsByBubble = new();
        readonly Dictionary<string, BarkBubble> _bubblesById = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, BarkBubble> RegisteredBubbles => _bubblesById;

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
                SyncActiveBubbles();
        }

        protected override void OnDestroy()
        {
            foreach (var cts in _playCtsByBubble.Values)
                cts.Cancel();
            _playCtsByBubble.Clear();
            _bubblesById.Clear();
            base.OnDestroy();
        }

        public bool RegisterBubble(string id, BarkBubble bubble)
        {
            if (string.IsNullOrWhiteSpace(id) || bubble == null)
                return false;

            id = id.Trim();
            if (_bubblesById.TryGetValue(id, out var existing) && existing != null && existing != bubble)
            {
                Debug.LogWarning(
                    $"[BarkDialogueManager] Bubble ID '{id}' is already registered by '{existing.name}'. Ignoring '{bubble.name}'.",
                    bubble);
                return false;
            }

            _bubblesById[id] = bubble;
            return true;
        }

        public void UnregisterBubble(string id, BarkBubble bubble)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            id = id.Trim();
            if (_bubblesById.TryGetValue(id, out var existing) && existing == bubble)
                _bubblesById.Remove(id);
        }

        public bool TryGetBubble(string id, out BarkBubble bubble)
        {
            bubble = null;
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var normalized = id.Trim();
            if (!_bubblesById.TryGetValue(normalized, out bubble))
                return false;

            if (bubble == null)
            {
                _bubblesById.Remove(normalized);
                return false;
            }

            return true;
        }

        void SyncActiveBubbles()
        {
            var bubbles = FindObjectsByType<BarkBubble>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var bubble in bubbles)
            {
                if (bubble != null && bubble.isActiveAndEnabled)
                    bubble.EnsureRegistered();
            }
        }

        public void PlayDialogue(
            string bubbleId,
            IDialogueCollection collection,
            BarkAutoNextMode autoNextMode = BarkAutoNextMode.Line,
            float? customLineEndDelay = null)
        {
            PlayDialogueAsync(bubbleId, collection, autoNextMode, customLineEndDelay).Forget();
        }

        public UniTask PlayDialogueAsync(
            string bubbleId,
            IDialogueCollection collection,
            BarkAutoNextMode autoNextMode,
            CancellationToken ct)
        {
            return PlayDialogueAsync(bubbleId, collection, autoNextMode, null, ct);
        }

        public async UniTask PlayDialogueAsync(
            string bubbleId,
            IDialogueCollection collection,
            BarkAutoNextMode autoNextMode = BarkAutoNextMode.Line,
            float? customLineEndDelay = null,
            CancellationToken ct = default)
        {
            if (!TryGetBubble(bubbleId, out var target))
            {
                Debug.LogWarning($"[BarkDialogueManager] No active BarkBubble registered for ID '{bubbleId}'.", this);
                return;
            }

            await PlayDialogueAsync(target, collection, autoNextMode, customLineEndDelay, ct);
        }

        public void EndDialogue(string bubbleId)
        {
            if (!TryGetBubble(bubbleId, out var target))
                return;

            EndDialogue(target);
        }

        public void PlayDialogue(
            BarkBubble target,
            IDialogueCollection collection,
            BarkAutoNextMode autoNextMode = BarkAutoNextMode.Line,
            float? customLineEndDelay = null)
        {
            PlayDialogueAsync(target, collection, autoNextMode, customLineEndDelay).Forget();
        }

        public UniTask PlayDialogueAsync(
            BarkBubble target,
            IDialogueCollection collection,
            BarkAutoNextMode autoNextMode,
            CancellationToken ct)
        {
            return PlayDialogueAsync(target, collection, autoNextMode, null, ct);
        }

        public async UniTask PlayDialogueAsync(
            BarkBubble target,
            IDialogueCollection collection,
            BarkAutoNextMode autoNextMode = BarkAutoNextMode.Line,
            float? customLineEndDelay = null,
            CancellationToken ct = default)
        {
            if (target == null)
            {
                Debug.LogWarning("[BarkDialogueManager] BarkBubble target is null.", this);
                return;
            }

            if (collection?.DialogueLines == null || collection.DialogueLines.Count == 0)
            {
                Debug.LogWarning("[BarkDialogueManager] Dialogue collection is empty.", this);
                return;
            }

            EndDialogue(target);

            var playCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken);
            _playCtsByBubble[target] = playCts;

            try
            {
                await target.ShowAsync(playCts.Token);

                foreach (var line in collection.DialogueLines)
                {
                    playCts.Token.ThrowIfCancellationRequested();

                    if (line == null)
                        continue;

                    var text = line.lineProvider.String;
                    await target.PlayDialogueTextAsync(text, playCts.Token);

                    var duration = autoNextMode == BarkAutoNextMode.CollectionChar
                        ? GetCollectionWait(text, customLineEndDelay)
                        : customLineEndDelay.HasValue
                            ? Mathf.Max(0f, customLineEndDelay.Value)
                            : GetAutoNextDuration(line);
                    if (duration > 0f)
                        await UniTask.WaitForSeconds(duration, cancellationToken: playCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Swallow — EndDialogue or a new PlayDialogue cancelled this run.
            }
            finally
            {
                if (_playCtsByBubble.TryGetValue(target, out var activeCts) && activeCts == playCts)
                {
                    _playCtsByBubble.Remove(target);
                    playCts.Dispose();
                    await target.HideAsync(destroyCancellationToken);
                    target.ClearDialogueText();
                }
            }
        }

        public void EndDialogue(BarkBubble target)
        {
            if (target == null)
                return;

            if (!_playCtsByBubble.TryGetValue(target, out var cts))
                return;

            _playCtsByBubble.Remove(target);
            cts.Cancel();
            cts.Dispose();

            HideThenClearAsync(target).Forget();
        }

        async UniTaskVoid HideThenClearAsync(BarkBubble target)
        {
            await target.HideAsync(destroyCancellationToken);

            // A new dialogue may have started on this bubble during the fade; don't wipe its text.
            if (target != null && !_playCtsByBubble.ContainsKey(target))
                target.ClearDialogueText();
        }

        float GetCollectionWait(string text, float? customLineEndDelay = null)
        {
            var charCount = string.IsNullOrEmpty(text) ? 0 : text.Length;
            var endDelay = customLineEndDelay.HasValue ? Mathf.Max(0f, customLineEndDelay.Value) : lineEndDelay;
            return charCount * collectionCharDelay + endDelay;
        }

        static float GetAutoNextDuration(DialogueLine line)
        {
            if (line is DialogueLineWithSpeaker speakerLine && speakerLine.autoNextDuration > 0f)
                return speakerLine.autoNextDuration;

            return DefaultAutoNextDuration;
        }
    }
}
