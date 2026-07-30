using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace RAXY.Narrative
{
    public class DialoguePortrait : MonoBehaviour
    {
        // Posisi terluar (di luar layar). Segmen dalam layar = maxPosition - 1, jadi maxPosition = ujung + 1.
        public const int MAX_SEGMENT = 6;

        public DialogueActorSO actorSO;

        [SerializeField]
        [LabelText("Off Screen Offset")]
        [Tooltip("Dorongan ekstra (dalam satuan canvas) buat segmen terakhir biar Portrait bener-bener keluar layar.")]
        float offScreenOffset = 300;

        Tween _positionTween;
        Tween _mirrorTween;
        Tween _sizeYTween;
        Tween _sizeScaleTween;

        float _positionTweenDuration = 0.3f;
        float _mirrorTweenDuration = 0.3f;
        float _colorTweenDuration = 0.3f;
        float _sizeTweenDuration = 0.3f;

        public void ConfigureTweenDurations(float position, float mirror, float color, float size)
        {
            _positionTweenDuration = position;
            _mirrorTweenDuration = mirror;
            _colorTweenDuration = color;
            _sizeTweenDuration = size;
        }

        Canvas cachedCanvas;
        Canvas Canvas
        {
            get
            {
                if (cachedCanvas == null)
                    cachedCanvas = GetComponentInParent<Canvas>();

                return cachedCanvas;
            }
        }

        [TitleGroup("Setup")]
        [TableList(AlwaysExpanded = true)]
        [ShowIf("@actorSO != null")]
        public List<PortraitPart> portraitParts;

        public List<PortraitPartEntry> PortraitPartEntries => actorSO?.fullScreenPortraitParts;

        [TitleGroup("Setup")]
        [ShowIf("@actorSO != null")]
#if UNITY_EDITOR
        [ListDrawerSettings(
            Expanded = true,
            CustomAddFunction = nameof(AddPortraitSize),
            CustomRemoveIndexFunction = nameof(RemovePortraitSizeAt))]
        [OnValueChanged(nameof(OnPortraitSizesInspectorChanged), IncludeChildren = true)]
#else
    [ListDrawerSettings(Expanded = true)]
#endif
        public List<PortraitSize> portraitSizes;

#if UNITY_EDITOR
        [SerializeField, HideInInspector] int _lastSyncedSoSizeCount = -1;
        [SerializeField, HideInInspector] int _lastSyncedPortraitSizeCount = -1;
        bool _syncingPortraitSizes;
#endif

        [TitleGroup("Setup")]
        [Button]
        [ShowIf("@actorSO != null")]
        public void Setup()
        {
            var newParts = new List<PortraitPart>();

            void AddPart(string partId)
            {
                if (string.IsNullOrEmpty(partId) || newParts.Exists(p => p.partId == partId))
                    return;

                var existing = portraitParts?.Find(p => p.partId == partId);
                newParts.Add(existing ?? new PortraitPart { partId = partId });
            }

            foreach (var entry in actorSO.fullScreenPortraitParts)
                AddPart(entry.partId);

            portraitParts = newParts;

            RebuildPortraitSizesFromSO();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            SyncPortraitSizesWithSO();
        }
#endif

        /// <summary>
        /// Sync list size Portrait ↔ ActorSO. Di editor juga dipanggil dari ActorSO saat SO berubah.
        /// </summary>
        public void SyncPortraitSizesWithSO()
        {
#if UNITY_EDITOR
            if (actorSO == null || _syncingPortraitSizes)
                return;

            _syncingPortraitSizes = true;
            try
            {
                actorSO.fullScreenPortraitSizes ??= new List<PortraitSizeEntry>();
                portraitSizes ??= new List<PortraitSize>();

                var soList = actorSO.fullScreenPortraitSizes;
                int soCount = soList.Count;
                int portraitCount = portraitSizes.Count;

                bool soCountChanged = soCount != _lastSyncedSoSizeCount;
                bool portraitCountChanged = portraitCount != _lastSyncedPortraitSizeCount;

                if (portraitCount != soCount)
                {
                    // List structure beda: Portrait yang berubah → push ke SO; selain itu SO menang.
                    if (portraitCountChanged && !soCountChanged)
                        PushPortraitSizesToSO();
                    else
                        RebuildPortraitSizesFromSO();
                }
                else
                {
                    bool anyUnbound = false;
                    bool orderDiffers = false;
                    for (int i = 0; i < soCount; i++)
                    {
                        var entry = portraitSizes[i]?.sizeEntry;
                        if (entry == null)
                        {
                            anyUnbound = true;
                            break;
                        }

                        if (!ReferenceEquals(entry, soList[i]))
                            orderDiffers = true;
                    }

                    if (anyUnbound)
                    {
                        RebuildPortraitSizesFromSO();
                    }
                    else if (orderDiffers)
                    {
                        bool allEntriesKnown = true;
                        for (int i = 0; i < portraitCount; i++)
                        {
                            if (!soList.Contains(portraitSizes[i].sizeEntry))
                            {
                                allEntriesKnown = false;
                                break;
                            }
                        }

                        // Reorder di Portrait → push; copy/stale refs → SO menang.
                        if (allEntriesKnown && !soCountChanged)
                            PushPortraitSizesToSO();
                        else
                            RebuildPortraitSizesFromSO();
                    }
                    else
                    {
                        // Sudah sync: cukup refresh transform/dirty target.
                        for (int i = 0; i < portraitCount; i++)
                            portraitSizes[i].SetupEditor(transform, actorSO);
                    }
                }

                _lastSyncedSoSizeCount = actorSO.fullScreenPortraitSizes.Count;
                _lastSyncedPortraitSizeCount = portraitSizes.Count;
            }
            finally
            {
                _syncingPortraitSizes = false;
            }
#endif
        }

#if UNITY_EDITOR
        void OnPortraitSizesInspectorChanged()
        {
            if (actorSO == null || _syncingPortraitSizes)
                return;

            _syncingPortraitSizes = true;
            try
            {
                PushPortraitSizesToSO();
                _lastSyncedSoSizeCount = actorSO.fullScreenPortraitSizes.Count;
                _lastSyncedPortraitSizeCount = portraitSizes.Count;
            }
            finally
            {
                _syncingPortraitSizes = false;
            }
        }

        void AddPortraitSize()
        {
            if (actorSO == null)
                return;

            UnityEditor.Undo.RecordObject(actorSO, "Add Portrait Size");
            actorSO.fullScreenPortraitSizes ??= new List<PortraitSizeEntry>();
            actorSO.fullScreenPortraitSizes.Add(new PortraitSizeEntry());
            UnityEditor.EditorUtility.SetDirty(actorSO);
            RebuildPortraitSizesFromSO();
            _lastSyncedSoSizeCount = actorSO.fullScreenPortraitSizes.Count;
            _lastSyncedPortraitSizeCount = portraitSizes.Count;
        }

        void RemovePortraitSizeAt(int index)
        {
            if (actorSO == null)
                return;

            var soList = actorSO.fullScreenPortraitSizes;
            if (soList == null || index < 0 || index >= soList.Count)
                return;

            UnityEditor.Undo.RecordObject(actorSO, "Remove Portrait Size");
            soList.RemoveAt(index);
            UnityEditor.EditorUtility.SetDirty(actorSO);
            RebuildPortraitSizesFromSO();
            _lastSyncedSoSizeCount = soList.Count;
            _lastSyncedPortraitSizeCount = portraitSizes.Count;
        }

        void PushPortraitSizesToSO()
        {
            var soList = actorSO.fullScreenPortraitSizes ??= new List<PortraitSizeEntry>();
            portraitSizes ??= new List<PortraitSize>();

            UnityEditor.Undo.RecordObject(actorSO, "Sync Portrait Sizes");

            var newSoList = new List<PortraitSizeEntry>(portraitSizes.Count);
            foreach (var portraitSize in portraitSizes)
            {
                if (portraitSize?.sizeEntry != null)
                    newSoList.Add(portraitSize.sizeEntry);
                else
                    newSoList.Add(new PortraitSizeEntry());
            }

            soList.Clear();
            soList.AddRange(newSoList);
            UnityEditor.EditorUtility.SetDirty(actorSO);
            RebuildPortraitSizesFromSO();
        }
#endif

        public void RebuildPortraitSizesFromSO()
        {
            if (actorSO == null)
                return;

#if UNITY_EDITOR
            bool wasSyncing = _syncingPortraitSizes;
            _syncingPortraitSizes = true;
#endif
            try
            {
                actorSO.fullScreenPortraitSizes ??= new List<PortraitSizeEntry>();
                var soList = actorSO.fullScreenPortraitSizes;

                portraitSizes = new List<PortraitSize>(soList.Count);
                foreach (var sizeEntry in soList)
                {
                    var newSizeSet = new PortraitSize();
                    newSizeSet.sizeEntry = sizeEntry;
                    newSizeSet.SetupEditor(transform, actorSO);
                    portraitSizes.Add(newSizeSet);
                }

#if UNITY_EDITOR
                _lastSyncedSoSizeCount = soList.Count;
                _lastSyncedPortraitSizeCount = portraitSizes.Count;
#endif
            }
#if UNITY_EDITOR
            finally
            {
                _syncingPortraitSizes = wasSyncing;
            }
#endif
        }

#if UNITY_EDITOR
        [TitleGroup("Test")]
        [FoldoutGroup("Test/Set Portrait")]
        [SerializeField]
        [LabelText("Part Id")]
        [ValueDropdown("PartIds")]
        string testPartId_setPortrait;

        [FoldoutGroup("Test/Set Portrait")]
        [SerializeField]
        [LabelText("Portrait Id")]
        [ValueDropdown("PortraitIds")]
        string testPortraitId_setPortrait;

        [FoldoutGroup("Test/Set Portrait")]
        [Button("Set Portrait")]
        void TestSetPortrait()
        {
            SetPortrait(testPortraitId_setPortrait, testPartId_setPortrait);
        }

        [FoldoutGroup("Test/Set Color")]
        [SerializeField]
        [LabelText("All Part")]
        bool testAllPart_SetColor;

        [FoldoutGroup("Test/Set Color")]
        [SerializeField]
        [LabelText("Part Id")]
        [ValueDropdown("PartIds")]
        [HideIf("@testAllPart_SetColor")]
        string testPartId_setColor;

        [FoldoutGroup("Test/Set Color")]
        [SerializeField]
        [LabelText("Color")]
        Color testColor_setColor;

        [FoldoutGroup("Test/Set Color")]
        [Button("Set Color")]
        void TestSetColor()
        {
            if (testAllPart_SetColor)
                SetColor(testColor_setColor, default);
            else
                SetColor(testColor_setColor, testPartId_setColor);
        }

        [FoldoutGroup("Test/Set Position")]
        [SerializeField]
        [LabelText("Position")]
        [PropertyRange(-MAX_SEGMENT, MAX_SEGMENT)]
        int testPosition_setPosition;

        [FoldoutGroup("Test/Set Position")]
        [Button("Set Position")]
        void TestSetPosition()
        {
            SetPosition(testPosition_setPosition);
        }

        [FoldoutGroup("Test/Set Scale")]
        [SerializeField]
        [LabelText("Scale")]
        float testScale_setScale = 1f;

        [FoldoutGroup("Test/Set Scale")]
        [Button("Set Scale")]
        void TestSetScale()
        {
            SetScale(testScale_setScale);
        }

        [FoldoutGroup("Test/Set Mirror")]
        [SerializeField]
        [LabelText("Mirror")]
        bool testMirror_setMirror;

        [FoldoutGroup("Test/Set Mirror")]
        [Button("Set Mirror")]
        void TestSetMirror()
        {
            SetMirror(testMirror_setMirror);
        }

        List<string> PartIds => PortraitPartEntries.Select(x => x.partId).ToList();
        List<string> PortraitIds
        {
            get
            {
                var selectedPartEntry = PortraitPartEntries.Find(x => x.partId == testPartId_setPortrait);
                if (selectedPartEntry == null)
                    return null;

                var tempEntries = selectedPartEntry.portraitEntries.Select(x => x.portraitId).ToList();
                return tempEntries;
            }
        }
#endif

        [TitleGroup("Debug Functions")]
        [Button]
        public void SetColor(Color color, string partId = default, bool useTween = false, float? duration = null)
        {
            if (partId != default)
            {
                Image portraitImg = portraitParts.Find(x => x.partId == partId).partImg;
                ApplyColor(portraitImg, color, useTween, duration);
            }
            else
            {
                foreach (var part in portraitParts)
                {
                    ApplyColor(part.partImg, color, useTween, duration);
                }
            }
        }

        void ApplyColor(Image img, Color color, bool useTween, float? duration = null)
        {
            if (img == null)
                return;

            img.DOKill();

            if (useTween)
                img.DOColor(color, duration ?? _colorTweenDuration);
            else
                img.color = color;
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void SetPortrait(string portraitId, string partId)
        {
            var selectedPartEntry = PortraitPartEntries.Find(x => x.partId == partId);
            var selectedPortrait = selectedPartEntry.portraitEntries.Find(x => x.portraitId == portraitId);

            Image portraitImg = portraitParts.Find(x => x.partId == partId).partImg;
            portraitImg.sprite = selectedPortrait.spriteProvider.Asset;
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void SetPosition(int xPosition, bool useTween = false, float? duration = null)
        {
            var rt = (RectTransform)transform;

            // Pakai lebar canvas (satuannya sama dengan anchoredPosition) biar angkanya akurat.
            var canvasRt = (RectTransform)Canvas.rootCanvas.transform;
            float halfScreen = canvasRt.rect.width * 0.5f;

            // Setengah layar dibagi (maxPosition - 1) segmen. (maxPosition - 1) = ujung layar, maxPosition = di luar.
            float segment = halfScreen / (MAX_SEGMENT - 1);
            float x = xPosition * segment;

            // Khusus segmen terakhir: dorong tambahan sesuai offScreenOffset biar bener-bener keluar layar.
            if (Mathf.Abs(xPosition) == MAX_SEGMENT)
                x += Mathf.Sign(xPosition) * offScreenOffset;

            _positionTween?.Kill();

            if (useTween)
            {
                _positionTween = rt.DOAnchorPosX(x, duration ?? _positionTweenDuration);
            }
            else
            {
                var pos = rt.anchoredPosition;
                pos.x = x;
                rt.anchoredPosition = pos;
            }
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void SetScale(float scale)
        {
            // Pertahankan tanda X biar state mirrored dari SetMirror nggak ke-reset.
            float abs = Mathf.Abs(scale);
            float signX = transform.localScale.x < 0f ? -1f : 1f;
            transform.localScale = new Vector3(abs * signX, abs, abs);
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void SetMirror(bool mirrored, bool useTween = false, float? duration = null)
        {
            // Cuma flip tanda X, besar skalanya dibiarkan biar nggak nabrak SetScale.
            var scale = transform.localScale;
            float targetX = Mathf.Abs(scale.x) * (mirrored ? -1f : 1f);

            _mirrorTween?.Kill();

            if (useTween)
            {
                _mirrorTween = transform.DOScaleX(targetX, duration ?? _mirrorTweenDuration);
            }
            else
            {
                scale.x = targetX;
                transform.localScale = scale;
            }
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void SetSize(int sizeIndex, bool useTween = false, float? duration = null)
        {
            if (actorSO?.fullScreenPortraitSizes == null
                || sizeIndex < 0
                || sizeIndex >= actorSO.fullScreenPortraitSizes.Count)
                return;

            var entry = actorSO.fullScreenPortraitSizes[sizeIndex];
            if (entry == null)
                return;

            float abs = Mathf.Abs(entry.portraitScale);
            float signX = transform.localScale.x < 0f ? -1f : 1f;
            var targetScale = new Vector3(abs * signX, abs, abs);
            float tweenDuration = duration ?? _sizeTweenDuration;

            _sizeYTween?.Kill();
            _sizeScaleTween?.Kill();

            if (transform is RectTransform rt)
            {
                if (useTween)
                {
                    _sizeYTween = rt.DOAnchorPosY(entry.yPos, tweenDuration);
                    _sizeScaleTween = transform.DOScale(targetScale, tweenDuration);
                }
                else
                {
                    var pos = rt.anchoredPosition;
                    pos.y = entry.yPos;
                    rt.anchoredPosition = pos;
                    transform.localScale = targetScale;
                }
            }
            else
            {
                if (useTween)
                {
                    _sizeYTween = transform.DOLocalMoveY(entry.yPos, tweenDuration);
                    _sizeScaleTween = transform.DOScale(targetScale, tweenDuration);
                }
                else
                {
                    var pos = transform.localPosition;
                    pos.y = entry.yPos;
                    transform.localPosition = pos;
                    transform.localScale = targetScale;
                }
            }
        }

        public void ProcessPortraitStateSetter(PortraitStateSetter setter, bool forceInstant = false)
        {
            if (setter == null)
                return;

            foreach (var part in setter.parts)
            {
                if (part.portraitSetter.setPortrait)
                    SetPortrait(part.portraitSetter.portraitId, part.partId);

                if (part.colorSetter.setColor)
                {
                    bool useTween = !forceInstant && part.colorSetter.useTween;
                    SetColor(part.colorSetter.color, part.partId, useTween,
                        useTween && part.colorSetter.customTweenDuration
                            ? part.colorSetter.tweenDuration
                            : null);
                }
            }

            if (setter.positionSetter != null && setter.positionSetter.setPosition)
            {
                bool useTween = !forceInstant && setter.positionSetter.useTween;
                SetPosition(setter.positionSetter.position, useTween,
                    useTween && setter.positionSetter.customTweenDuration
                        ? setter.positionSetter.tweenDuration
                        : null);
            }

            if (setter.mirrorSetter != null && setter.mirrorSetter.setMirror)
            {
                bool useTween = !forceInstant && setter.mirrorSetter.useTween;
                SetMirror(setter.mirrorSetter.mirrored, useTween,
                    useTween && setter.mirrorSetter.customTweenDuration
                        ? setter.mirrorSetter.tweenDuration
                        : null);
            }

            if (setter.sizeSetter != null && setter.sizeSetter.setSize)
            {
                bool useTween = !forceInstant && setter.sizeSetter.useTween;
                SetSize(setter.sizeSetter.sizeIndex, useTween,
                    useTween && setter.sizeSetter.customTweenDuration
                        ? setter.sizeSetter.tweenDuration
                        : null);
            }
        }
    }

    [Serializable]
    public class PortraitPart
    {
        [TableColumnWidth(100, false)]
        public string partId;
        public Image partImg;
    }

    [Serializable]
    public class PortraitSize
    {
        [HorizontalGroup]
        [HideLabel]
        public PortraitSizeEntry sizeEntry;
        Transform transform;
        UnityEngine.Object dirtyTarget;

        [HorizontalGroup(0.2f)]
        [Button(ButtonHeight = 45)]
        public void Preview()
        {
            if (transform == null || sizeEntry == null)
                return;

            if (transform is RectTransform rt)
            {
                var pos = rt.anchoredPosition;
                pos.y = sizeEntry.yPos;
                rt.anchoredPosition = pos;
            }
            else
            {
                var pos = transform.localPosition;
                pos.y = sizeEntry.yPos;
                transform.localPosition = pos;
            }

            // Pertahankan tanda X biar mirror state nggak ke-reset.
            float abs = Mathf.Abs(sizeEntry.portraitScale);
            float signX = transform.localScale.x < 0f ? -1f : 1f;
            transform.localScale = new Vector3(abs * signX, abs, abs);
        }

        [HorizontalGroup(0.2f)]
        [Button(ButtonHeight = 45)]
        public void Store()
        {
            if (transform == null || sizeEntry == null)
                return;

            // RectTransform: pakai anchoredPosition biar konsisten sama anchor (bukan localPosition).
            if (transform is RectTransform rt)
                sizeEntry.yPos = rt.anchoredPosition.y;
            else
                sizeEntry.yPos = transform.localPosition.y;

            var avg = (Mathf.Abs(transform.localScale.x) +
                        Mathf.Abs(transform.localScale.y) +
                        Mathf.Abs(transform.localScale.z)) / 3;

            sizeEntry.portraitScale = avg;

#if UNITY_EDITOR
            if (dirtyTarget != null)
                UnityEditor.EditorUtility.SetDirty(dirtyTarget);
#endif
        }



        public void SetupEditor(Transform ownerTransform, UnityEngine.Object dirtyTarget = null)
        {
            transform = ownerTransform;
            this.dirtyTarget = dirtyTarget;
        }
    }
}
