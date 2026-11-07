using System.Collections.Generic;
using UnityEngine;

namespace RAXY.Narrative
{
    [DisallowMultipleComponent]
    public class NarrativeActor : MonoBehaviour
    {
        [SerializeField]
        string actorId;

        public string ActorId
        {
            get => actorId;
            set => actorId = value;
        }

        static readonly List<NarrativeActor> activeActors = new();

        public static IReadOnlyList<NarrativeActor> ActiveActors => activeActors;

        void OnEnable()
        {
            if (!activeActors.Contains(this))
                activeActors.Add(this);
        }

        void OnDisable()
        {
            activeActors.Remove(this);
        }

        void OnDestroy()
        {
            activeActors.Remove(this);
        }

        public static Transform FindActorTransform(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("[NarrativeActor] Actor ID is null or empty.");
                return null;
            }

            NarrativeActor found = null;
            int count = 0;

            for (int i = 0; i < activeActors.Count; i++)
            {
                var actor = activeActors[i];
                if (actor != null && actor.ActorId == id)
                {
                    found = actor;
                    count++;
                }
            }

            if (count == 0)
            {
                var all = Object.FindObjectsByType<NarrativeActor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    var actor = all[i];
                    if (actor != null && actor.ActorId == id)
                    {
                        found = actor;
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                Debug.LogWarning($"[NarrativeActor] No active NarrativeActor found with actorId '{id}'.");
                return null;
            }

            if (count > 1)
            {
                Debug.LogWarning($"[NarrativeActor] Multiple NarrativeActors found with actorId '{id}' ({count} found). Skipping resolution to avoid ambiguous mutation.");
                return null;
            }

            return found != null ? found.transform : null;
        }
    }
}
