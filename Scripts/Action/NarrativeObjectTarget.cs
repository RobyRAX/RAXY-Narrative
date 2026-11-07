using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    public enum NarrativeTargetMode
    {
        ActorId,
        GameObjectName
    }

    [Serializable]
    public class NarrativeObjectTarget
    {
        [HorizontalGroup("Target", LabelWidth = 90)]
        [LabelText("Target Type")]
        public NarrativeTargetMode targetMode = NarrativeTargetMode.ActorId;

        [HorizontalGroup("Target")]
        [ShowIf(nameof(IsActorMode))]
        [HideLabel]
        [Tooltip("Actor ID matching NarrativeActor.actorId on a scene object.")]
        public string actorId;

        [HorizontalGroup("Target")]
        [ShowIf(nameof(IsGameObjectMode))]
        [HideLabel]
        [Tooltip("Exact name of the active GameObject in the scene.")]
        public string gameObjectName;

        public string TargetDescription =>
            targetMode == NarrativeTargetMode.ActorId
                ? $"ActorId '{actorId}'"
                : $"GameObject '{gameObjectName}'";

        public Transform Resolve()
        {
            switch (targetMode)
            {
                case NarrativeTargetMode.ActorId:
                    if (string.IsNullOrWhiteSpace(actorId))
                    {
                        Debug.LogWarning("[NarrativeObjectTarget] actorId is empty or not specified.");
                        return null;
                    }
                    return NarrativeActor.FindActorTransform(actorId);

                case NarrativeTargetMode.GameObjectName:
                    if (string.IsNullOrWhiteSpace(gameObjectName))
                    {
                        Debug.LogWarning("[NarrativeObjectTarget] gameObjectName is empty or not specified.");
                        return null;
                    }
                    var go = GameObject.Find(gameObjectName);
                    if (go == null)
                    {
                        Debug.LogWarning($"[NarrativeObjectTarget] No active GameObject found with name '{gameObjectName}'.");
                        return null;
                    }
                    return go.transform;

                default:
                    Debug.LogWarning($"[NarrativeObjectTarget] Unsupported targetMode: {targetMode}.");
                    return null;
            }
        }

        bool IsActorMode => targetMode == NarrativeTargetMode.ActorId;
        bool IsGameObjectMode => targetMode == NarrativeTargetMode.GameObjectName;
    }
}
