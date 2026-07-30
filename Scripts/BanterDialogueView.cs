using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using RAXY.Core.Addressable;
using RAXY.UI;
using RAXY.Utility;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RAXY.Narrative
{
    public class BanterDialogueView : MonoBehaviour
    {
        [TitleGroup("UI Ref")]
        [SerializeField]
        Canvas canvas;

        [TitleGroup("UI Ref")]
        [SerializeField]
        Image banterPortraitImg;

        [TitleGroup("UI Ref")]
        [SerializeField]
        TextMeshProUGUI speakerNameTmp;

        [TitleGroup("UI Ref")]
        [SerializeField]
        TextMeshProUGUI dialogueLineTmp;

        [TitleGroup("Settings")]
        [SerializeField]
        bool useTextTyper;

        [TitleGroup("Settings")]
        [SerializeField]
        [Range(0.001f, 0.1f)]
        [ShowIf("@useTextTyper")]
        float charInterval = 0.03f;

        [TitleGroup("Settings")]
        [SerializeField]
        [PropertySpace(5, 0)]
        BanterDialogueTransition transitionType;

        [TitleGroup("Settings")]
        [SerializeField]
        [ShowIf("@transitionType == BanterDialogueTransition.Fade")]
        float fadeDuration = 0.25f;

        [TitleGroup("Settings")]
        [SerializeField]
        [ShowIf("@transitionType == BanterDialogueTransition.Animation")]
        bool setCanvasOpacityToOne;

        [TitleGroup("Settings")]
        [SerializeField]
        [ShowIf("@transitionType == BanterDialogueTransition.Animation")]
        AnimationClip enterAnimationClip;

        [TitleGroup("Settings")]
        [SerializeField]
        [ShowIf("@transitionType == BanterDialogueTransition.Animation")]
        AnimationClip exitAnimationClip;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        CanvasGroup _cg;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        TextTyper _textTyper;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        Animation _anim;

        CancellationTokenSource _playCts;

        public event Action<BanterDialogueDataSO> OnDialogueStart;
        public event Action<BanterDialogueDataSO> OnDialogueEnd;

        void Awake()
        {
            _cg = canvas.GetOrAddComponent<CanvasGroup>();
            _cg.alpha = 0;

            _textTyper = dialogueLineTmp.GetOrAddComponent<TextTyper>();
            _anim = gameObject.GetOrAddComponent<Animation>();
        }

        void OnDestroy()
        {
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;

            StopTransitions();
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void Play(BanterDialogueDataSO data)
        {
            PlayAsync(data).Forget();
        }

        public async UniTask PlayAsync(BanterDialogueDataSO data, CancellationToken ct = default)
        {
            if (data == null || data.dialogueCollection == null)
                return;

            // Batalkan playthrough sebelumnya biar loop lama berhenti menyentuh state bersama.
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _playCts.Token;

            bool started = false;
            try
            {
                started = true;
                OnDialogueStart?.Invoke(data);

                var lines = data.dialogueCollection.DialogueLineWithBanters;
                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        if (line == null)
                            continue;

                        if (speakerNameTmp != null)
                            speakerNameTmp.text = line.SpeakerName;

                        await ApplyBanterPortraitAsync(data, line);

                        string text = await GetLineTextAsync(line);
                        ShowLineText(text);

                        await PlayEnterAsync(token);
                        await WaitLineAsync(line, token);
                        await PlayExitAsync(token);
                    }
                }

                if (dialogueLineTmp != null)
                    dialogueLineTmp.text = string.Empty;
            }
            finally
            {
                if (started)
                    OnDialogueEnd?.Invoke(data);
            }
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void EndDialogue()
        {
            EndDialogueAsync().Forget();
        }

        public async UniTask EndDialogueAsync(CancellationToken ct = default)
        {
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;

            StopTransitions();
            await PlayExitAsync(ct);

            if (dialogueLineTmp != null)
                dialogueLineTmp.text = string.Empty;
        }

        void ShowLineText(string text)
        {
            if (useTextTyper && _textTyper != null)
            {
                _textTyper.charInterval = charInterval;
                _textTyper.StartTyping(text);
            }
            else if (dialogueLineTmp != null)
            {
                dialogueLineTmp.text = text;
                // Pastikan teks tampil penuh walau TextTyper sempat memangkas maxVisibleCharacters.
                dialogueLineTmp.maxVisibleCharacters = int.MaxValue;
            }
        }

        async UniTask WaitLineAsync(DialogueLineWithBanter line, CancellationToken ct)
        {
            // Banter tidak menunggu klik: tunggu typing selesai, lalu jalankan auto next timer.
            if (useTextTyper && _textTyper != null)
                await UniTask.WaitWhile(() => _textTyper.IsTyping, cancellationToken: ct);

            if (line.autoNextDuration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(line.autoNextDuration), cancellationToken: ct);
        }

        async UniTask PlayEnterAsync(CancellationToken ct)
        {
            if (transitionType == BanterDialogueTransition.Animation)
                await PlayAnimationClipAsync(enterAnimationClip, ct);
            else
                await FadeCanvasGroupAsync(1f, ct);
        }

        async UniTask PlayExitAsync(CancellationToken ct)
        {
            if (transitionType == BanterDialogueTransition.Animation)
                await PlayAnimationClipAsync(exitAnimationClip, ct);
            else
                await FadeCanvasGroupAsync(0f, ct);
        }

        async UniTask PlayAnimationClipAsync(AnimationClip clip, CancellationToken ct)
        {
            if (clip == null || _anim == null)
                return;

            if (setCanvasOpacityToOne)
                _cg.alpha = 1;

            if (_anim.GetClip(clip.name) == null)
                _anim.AddClip(clip, clip.name);

            _anim.Play(clip.name);
            await UniTask.WaitWhile(() => _anim != null && _anim.isPlaying, cancellationToken: ct);
        }

        async UniTask ApplyBanterPortraitAsync(BanterDialogueDataSO data, DialogueLineWithBanter line)
        {
            if (banterPortraitImg == null)
                return;

            Sprite sprite = null;

            if (line.useCustomBanterPortrait)
            {
                if (line.banterPortraitProvider != null)
                    sprite = await AddressableService.ResolveAsync(line.banterPortraitProvider);
            }
            else
            {
                var actor = data.actors?.Find(a => a != null && a.ActorId == line.BanterActorId);
                var entry = actor?.banterPortraits?.Find(p => p != null && p.portraitId == line.banterPortraitId);
                if (entry?.spriteProvider != null)
                    sprite = await AddressableService.ResolveAsync(entry.spriteProvider);
            }

            banterPortraitImg.sprite = sprite;
            banterPortraitImg.enabled = sprite != null;
        }

        async UniTask<string> GetLineTextAsync(DialogueLine line)
        {
            if (line?.lineProvider == null)
                return string.Empty;

            return await line.lineProvider.GetStringAsync();
        }

        async UniTask FadeCanvasGroupAsync(float targetAlpha, CancellationToken ct = default)
        {
            if (_cg == null)
                return;

            DOTween.Kill(_cg);

            if (fadeDuration <= 0f)
            {
                _cg.alpha = targetAlpha;
                return;
            }

            var tween = DOTween.To(() => _cg.alpha, x => _cg.alpha = x, targetAlpha, fadeDuration)
                               .SetTarget(_cg);
            await UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete(), cancellationToken: ct);
        }

        void StopTransitions()
        {
            if (_cg != null)
                DOTween.Kill(_cg);

            if (_anim != null && _anim.isPlaying)
                _anim.Stop();
        }
    }

    public enum BanterDialogueTransition
    {
        Fade,
        Animation
    }
}
