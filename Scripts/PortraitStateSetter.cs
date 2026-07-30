using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    [Serializable]
    public class PortraitStateSetter
    {
        [HorizontalGroup("Row")]
        [BoxGroup("Row/Position")]
        [HideLabel]
        public PositionSetter positionSetter;

        [BoxGroup("Row/Mirror")]
        [HideLabel]
        public MirrorSetter mirrorSetter;

        [BoxGroup("Row/Size")]
        [HideLabel]
        public SizeSetter sizeSetter;

        [FoldoutGroup("Parts")]
        [TableList(AlwaysExpanded = true, IsReadOnly = true, HideToolbar = true)]
        public List<PartStateSetter> parts;

#if UNITY_EDITOR
        [FoldoutGroup("Parts/Set All Part Color")]
        [SerializeField]
        Color color = Color.white;

        [FoldoutGroup("Parts/Set All Part Color")]
        [Button]
        void SetAllPartColor()
        {
            SetAllPartColor(color);
        }

        [BoxGroup("Parts")]
        public void SetAllPartColor(Color color)
        {
            foreach (var part in parts)
            {
                part.colorSetter.color = color;
            }
        }

        [SerializeField]
        [ReadOnly]
        [HideInInspector]
        DialogueActorSO actorSO;

        public void SetupEditor(DialogueActorSO actorSO, bool hideUseTween = false)
        {
            this.actorSO = actorSO;

            positionSetter ??= new PositionSetter();
            positionSetter.HideUseTween = hideUseTween;

            mirrorSetter ??= new MirrorSetter();
            mirrorSetter.HideUseTween = hideUseTween;

            sizeSetter ??= new SizeSetter();
            sizeSetter.SetupEditor(actorSO, hideUseTween);

            var oldParts = parts;
            var newParts = new List<PartStateSetter>();

            foreach (var part in actorSO.fullScreenPortraitParts)
            {
                var existing = oldParts?.Find(p => p.partId == part.partId);
                var partSetter = existing ?? new PartStateSetter();
                partSetter.SetupEditor(part.partId, part.AllPortraitIds, hideUseTween);

                newParts.Add(partSetter);
            }

            parts = newParts;
        }
#endif
    }

    [Serializable]
    public class PartStateSetter
    {
        [TableColumnWidth(75, false)]
        [ReadOnly]
        public string partId;

        public PortraitSetter portraitSetter;
        public ColorSetter colorSetter;

#if UNITY_EDITOR
        public void SetupEditor(string partId, List<string> ids, bool hideUseTween = false)
        {
            this.partId = partId;

            portraitSetter ??= new PortraitSetter();
            portraitSetter.SetupEditor(ids);

            colorSetter ??= new ColorSetter();
            colorSetter.HideUseTween = hideUseTween;
        }
#endif
    }

    [Serializable]
    public class PortraitSetter
    {
        [ToggleLeft]
        public bool setPortrait;

        [HideLabel]
        [ValueDropdown("Ids")]
        [ShowIf("@setPortrait")]
        public string portraitId;

#if UNITY_EDITOR
        [SerializeField]
        [ReadOnly]
        [HideInInspector]
        List<string> Ids;

        public void SetupEditor(List<string> ids)
        {
            this.Ids = ids;

            if (!string.IsNullOrEmpty(portraitId) && (ids == null || !ids.Contains(portraitId)))
                portraitId = null;
        }
#endif
    }

    [Serializable]
    public class ColorSetter
    {
        [ToggleLeft]
        public bool setColor;

        [ToggleLeft]
        [ShowIf("@setColor && !HideUseTween")]
        public bool useTween = true;

        [ToggleLeft]
        [ShowIf("@setColor && useTween && !HideUseTween")]
        public bool customTweenDuration;

        [ShowIf("@setColor && useTween && customTweenDuration && !HideUseTween")]
        [LabelText("Duration")]
        [MinValue(0)]
        public float tweenDuration = 0.3f;

        [HideLabel]
        [ShowIf("@setColor")]
        public Color color = Color.white;

#if UNITY_EDITOR
        [HideInInspector]
        public bool HideUseTween;
#endif
    }

    [Serializable]
    public class PositionSetter
    {
        [ToggleLeft]
        public bool setPosition;

        [ToggleLeft]
        [ShowIf("@setPosition && !HideUseTween")]
        public bool useTween = true;

        [ToggleLeft]
        [ShowIf("@setPosition && useTween && !HideUseTween")]
        public bool customTweenDuration;

        [ShowIf("@setPosition && useTween && customTweenDuration && !HideUseTween")]
        [LabelText("Duration")]
        [MinValue(0)]
        public float tweenDuration = 0.3f;

        [HideLabel]
        [ShowIf("@setPosition")]
        [PropertyRange(-DialoguePortrait.MAX_SEGMENT, DialoguePortrait.MAX_SEGMENT)]
        public int position;

#if UNITY_EDITOR
        [HideInInspector]
        public bool HideUseTween;
#endif
    }

    [Serializable]
    public class MirrorSetter
    {
        [ToggleLeft]
        public bool setMirror;

        [ToggleLeft]
        [ShowIf("@setMirror && !HideUseTween")]
        public bool useTween = true;

        [ToggleLeft]
        [ShowIf("@setMirror && useTween && !HideUseTween")]
        public bool customTweenDuration;

        [ShowIf("@setMirror && useTween && customTweenDuration && !HideUseTween")]
        [LabelText("Duration")]
        [MinValue(0)]
        public float tweenDuration = 0.3f;

        [ToggleLeft]
        [ShowIf("@setMirror")]
        public bool mirrored;

#if UNITY_EDITOR
        [HideInInspector]
        public bool HideUseTween;
#endif
    }

    [Serializable]
    public class SizeSetter
    {
        [HorizontalGroup]
        [ToggleLeft]
        public bool setSize;

        [ToggleLeft]
        [ShowIf("@setSize && !HideUseTween")]
        public bool useTween = true;

        [ToggleLeft]
        [ShowIf("@setSize && useTween && !HideUseTween")]
        public bool customTweenDuration;

        [ShowIf("@setSize && useTween && customTweenDuration && !HideUseTween")]
        [LabelText("Duration")]
        [MinValue(0)]
        public float tweenDuration = 0.3f;

        [HideLabel]
        [ShowIf("@setSize")]
        [PropertyRange(0, "MaxSizeIndex")]
        public int sizeIndex;

#if UNITY_EDITOR
        [HorizontalGroup]
        [ShowInInspector]
        [ShowIf("@setSize")]
        [DisplayAsString]
        [HideLabel]
        string SizePreview
        {
            get
            {
                var list = actorSO?.fullScreenPortraitSizes;
                if (list == null || sizeIndex < 0 || sizeIndex >= list.Count)
                    return "—";

                var entry = list[sizeIndex];
                if (entry == null)
                    return "—";

                return $"Y {entry.yPos:0.##} · Scale {entry.portraitScale:0.##}";
            }
        }

        [HideInInspector]
        public bool HideUseTween;

        [HideInInspector]
        public int MaxSizeIndex;

        [SerializeField]
        [HideInInspector]
        DialogueActorSO actorSO;

        public void SetupEditor(DialogueActorSO actorSO, bool hideUseTween = false)
        {
            this.actorSO = actorSO;
            HideUseTween = hideUseTween;

            int count = actorSO?.fullScreenPortraitSizes?.Count ?? 0;
            MaxSizeIndex = Mathf.Max(0, count - 1);
            sizeIndex = Mathf.Clamp(sizeIndex, 0, MaxSizeIndex);
        }
#endif
    }
}
