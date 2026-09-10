using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    public class TextOverlayClip : PlayableAsset, ITimelineClipAsset
    {
        public string text = "";
        public Color color = Color.white;
        public float fadeIn = 0.5f;
        public float fadeOut = 0.5f;

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TextOverlayBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.text = text;
            behaviour.color = color;
            behaviour.fadeIn = fadeIn;
            behaviour.fadeOut = fadeOut;
            return playable;
        }
    }
}
