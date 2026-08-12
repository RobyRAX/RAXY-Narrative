using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

namespace RAXY.Narrative
{
    public class TimelineCutsceneRunner : MonoBehaviour
    {
        [TitleGroup("Runtime")]
        [ShowInInspector]
        public TimelineCutscene CurrentCutscene;
        public PlayableDirector CurrentPlayableDirector => CurrentCutscene != null ? CurrentCutscene.PlayableDirector : null;

        [TitleGroup("Runtime")]
        [ShowInInspector]
        public List<TimelineCutsceneTrackBinder> TrackBinders { get; set; }

        public event Action<TimelineCutscene> OnCutsceneStart;
        public event Action<TimelineCutscene> OnCutsceneEnd;

        TimelineCutscene _subscribedCutscene;
        bool _ownsCurrentCutsceneInstance;

        void OnDisable()
        {
            CleanupCurrentCutscene(completed: false);
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void PlayCutscene(TimelineCutscene cutscene)
            => PlayCutscene(cutscene, null);

        public void PlayCutscene(TimelineCutscene cutscene, string timelineId)
        {
            if (cutscene == null)
            {
                Debug.LogWarning("[TimelineCutsceneRunner] PlayCutscene cutscene null.", this);
                return;
            }

            CleanupCurrentCutscene(completed: false);

            CurrentCutscene = ResolveCutsceneInstance(cutscene);
            if (CurrentCutscene == null)
            {
                Debug.LogWarning("[TimelineCutsceneRunner] Gagal resolve TimelineCutscene instance.", this);
                return;
            }

            if (!CurrentCutscene.gameObject.activeInHierarchy)
                CurrentCutscene.gameObject.SetActive(true);

            SubscribeCurrentCutscene();

            if (!string.IsNullOrEmpty(timelineId))
                CurrentCutscene.Play(timelineId);
            else
                CurrentCutscene.PlayFromStart();
        }

        TimelineCutscene ResolveCutsceneInstance(TimelineCutscene cutscene)
        {
            // Scene instance: pakai langsung. Prefab/asset: Instantiate supaya Update jalan.
            if (cutscene.gameObject.scene.IsValid() && cutscene.gameObject.scene.isLoaded)
            {
                _ownsCurrentCutsceneInstance = false;
                return cutscene;
            }

            var temp = Instantiate(cutscene);
            temp.name = cutscene.name;
            _ownsCurrentCutsceneInstance = true;
            return temp;
        }

        [TitleGroup("Debug Functions")]
        [Button]
        public void FindAllBinders()
        {
            TrackBinders = FindObjectsByType<TimelineCutsceneTrackBinder>(FindObjectsSortMode.None).ToList();
        }

        void SubscribeCurrentCutscene()
        {
            if (CurrentCutscene == null)
                return;

            _subscribedCutscene = CurrentCutscene;
            _subscribedCutscene.OnStarted += HandleCutsceneStarted;
            _subscribedCutscene.OnEnded += HandleCutsceneEnded;
        }

        void UnsubscribeCurrentCutscene()
        {
            if (_subscribedCutscene == null)
                return;

            _subscribedCutscene.OnStarted -= HandleCutsceneStarted;
            _subscribedCutscene.OnEnded -= HandleCutsceneEnded;
            _subscribedCutscene = null;
        }

        void CleanupCurrentCutscene(bool completed)
        {
            UnsubscribeCurrentCutscene();

            TimelineCutscene.RestoreAllTrackBinderComponentToggles();

            if (CurrentCutscene != null)
            {
                if (CurrentCutscene.PlayableDirector != null)
                    CurrentCutscene.PlayableDirector.Stop();

                // Complete: honor DestroyOnComplete.
                // Interrupt / disable: always destroy runner-spawned instances to avoid leaks.
                var shouldDestroy = completed
                    ? CurrentCutscene.DestroyOnComplete
                    : _ownsCurrentCutsceneInstance;

                if (shouldDestroy)
                    Destroy(CurrentCutscene.gameObject);
            }

            CurrentCutscene = null;
            _ownsCurrentCutsceneInstance = false;
        }

        void HandleCutsceneStarted(TimelineCutscene cutscene)
            => OnCutsceneStart?.Invoke(cutscene);

        void HandleCutsceneEnded(TimelineCutscene cutscene)
        {
            OnCutsceneEnd?.Invoke(cutscene);
            CleanupCurrentCutscene(completed: true);
        }
    }
}
