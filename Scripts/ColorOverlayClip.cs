using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    public class ColorOverlayClip : PlayableAsset, ITimelineClipAsset
    {
        public Color color = Color.black;
        public float fadeIn = 0.5f;
        public float fadeOut = 0.5f;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ColorOverlayBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.color = color;
            behaviour.fadeIn = fadeIn;
            behaviour.fadeOut = fadeOut;
            return playable;
        }
    }
}
