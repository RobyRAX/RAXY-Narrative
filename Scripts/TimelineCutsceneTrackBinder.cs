using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace RAXY.Narrative
{
    public class TimelineCutsceneTrackBinder : MonoBehaviour
    {
        [TitleGroup("Track Ref")]
        [ShowIf("@IsAnimationTrackBind")]
        public Animator animator;

        [TitleGroup("Track Ref")]
        [ShowIf("@IsCinemachineBrainTrackBind")]
        public CinemachineBrain cinemachineBrain;

        [TitleGroup("Track Ref")]
        [ShowIf("@IsColorOverlayManagerTrackBind")]
        public ColorOverlayManager colorOverlayManager;

        [TitleGroup("Track Ref")]
        [ShowIf("@IsTextOverlayManagerTrackBind")]
        public TextOverlayManager textOverlayManager;

        [TitleGroup("Track Ref")]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = true)]
        public List<TrackBindEntry> trackBinds;

        [TitleGroup("Cutscene Events")]
        [FoldoutGroup("Cutscene Events/Events")]
        [Tooltip("Invoked when this binder is applied for a playing cutscene (before component disables). Wire game-side suspend here.")]
        public UnityEvent onCutsceneBindingApplied = new();

        [FoldoutGroup("Cutscene Events/Events")]
        [Tooltip("Invoked when cutscene binding is restored (after component enables are restored). Wire game-side resume here.")]
        public UnityEvent onCutsceneBindingRestored = new();

        [TitleGroup("Disable During Cutscene")]
        [Tooltip("Components disabled while a cutscene is playing (Behaviour or Collider, including CharacterController). Previous enabled state is restored on end. Animator used for Animation track binding is always skipped.")]
        [ListDrawerSettings(ShowFoldout = true)]
        [FormerlySerializedAs("behavioursToDisableDuringCutscene")]
        public List<Component> componentsToDisableDuringCutscene = new();

        [TitleGroup("Disable During Cutscene")]
        [Button("Select All Toggleable Components")]
        void SelectAllToggleableComponents()
        {
            componentsToDisableDuringCutscene ??= new List<Component>();
            componentsToDisableDuringCutscene.Clear();

            var components = GetComponentsInChildren<Component>(true);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                    continue;

                if (component is TimelineCutsceneTrackBinder)
                    continue;

                if (component is Animator)
                    continue;

                if (component is not Behaviour && component is not Collider)
                    continue;

                componentsToDisableDuringCutscene.Add(component);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public bool IsAnimationTrackBind
        {
            get
            {
                bool containAnimation = false;
                foreach (var entry in trackBinds)
                {
                    if (entry.trackBindType == TrackBindType.Animation)
                    {
                        containAnimation = true;
                        break;
                    }
                }

                return containAnimation;
            }
        }

        public bool IsCinemachineBrainTrackBind
        {
            get
            {
                bool containCinemachineBrain = false;
                foreach (var entry in trackBinds)
                {
                    if (entry.trackBindType == TrackBindType.CinemachineBrain)
                    {
                        containCinemachineBrain = true;
                        break;
                    }
                }

                return containCinemachineBrain;
            }
        }

        public bool IsColorOverlayManagerTrackBind
        {
            get
            {
                bool containColorOverlayManager = false;
                foreach (var entry in trackBinds)
                {
                    if (entry.trackBindType == TrackBindType.ColorOverlayManager)
                    {
                        containColorOverlayManager = true;
                        break;
                    }
                }

                return containColorOverlayManager;
            }
        }

        public bool IsTextOverlayManagerTrackBind
        {
            get
            {
                bool containTextOverlayManager = false;
                foreach (var entry in trackBinds)
                {
                    if (entry.trackBindType == TrackBindType.TextOverlayManager)
                    {
                        containTextOverlayManager = true;
                        break;
                    }
                }

                return containTextOverlayManager;
            }
        }

        readonly List<ComponentToggleState> _savedToggleStates = new();
        bool _togglesApplied;

        public void ApplyComponentToggles()
        {
            if (_togglesApplied)
                return;

            onCutsceneBindingApplied?.Invoke();

            _savedToggleStates.Clear();

            if (componentsToDisableDuringCutscene == null)
            {
                _togglesApplied = true;
                return;
            }

            for (var i = 0; i < componentsToDisableDuringCutscene.Count; i++)
            {
                var component = componentsToDisableDuringCutscene[i];
                if (component == null)
                    continue;

                if (animator != null && component == animator)
                    continue;

                if (!TryGetEnabled(component, out var wasEnabled))
                {
                    Debug.LogWarning(
                        $"[TimelineCutsceneTrackBinder] '{component.GetType().Name}' tidak punya property enabled yang didukung (butuh Behaviour atau Collider).",
                        component);
                    continue;
                }

                _savedToggleStates.Add(new ComponentToggleState(component, wasEnabled));
                SetEnabled(component, false);
            }

            _togglesApplied = true;
        }

        public void RestoreComponentToggles()
        {
            if (!_togglesApplied)
                return;

            for (var i = 0; i < _savedToggleStates.Count; i++)
            {
                var state = _savedToggleStates[i];
                if (state.Component == null)
                    continue;

                SetEnabled(state.Component, state.WasEnabled);
            }

            _savedToggleStates.Clear();
            _togglesApplied = false;

            onCutsceneBindingRestored?.Invoke();
        }

        void OnDestroy()
        {
            RestoreComponentToggles();
        }

        static bool TryGetEnabled(Component component, out bool enabled)
        {
            if (component is Behaviour behaviour)
            {
                enabled = behaviour.enabled;
                return true;
            }

            if (component is Collider collider)
            {
                enabled = collider.enabled;
                return true;
            }

            enabled = false;
            return false;
        }

        static void SetEnabled(Component component, bool enabled)
        {
            if (component is Behaviour behaviour)
            {
                behaviour.enabled = enabled;
                return;
            }

            if (component is Collider collider)
                collider.enabled = enabled;
        }

        struct ComponentToggleState
        {
            public Component Component;
            public bool WasEnabled;

            public ComponentToggleState(Component component, bool wasEnabled)
            {
                Component = component;
                WasEnabled = wasEnabled;
            }
        }
    }

    [Serializable]
    public class TrackBindEntry
    {
        public string trackName;
        public TrackBindType trackBindType;
    }

    public enum TrackBindType
    {
        Animation,
        CinemachineBrain,
        ColorOverlayManager,
        TextOverlayManager
    }
}
