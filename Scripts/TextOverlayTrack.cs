using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RAXY.Narrative
{
    [TrackColor(0.85f, 0.85f, 0.9f)]
    [TrackClipType(typeof(TextOverlayClip))]
    [TrackBindingType(typeof(TextOverlayManager))]
    public class TextOverlayTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<TextOverlayMixerBehaviour>.Create(graph, inputCount);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
#if UNITY_EDITOR
            var binding = director.GetGenericBinding(this) as TextOverlayManager;
            if (binding != null && binding.TextTmp != null)
                driver.AddFromName<TextMeshProUGUI>(binding.TextTmp.gameObject, "m_fontColor");
#endif
            base.GatherProperties(director, driver);
        }
    }
}
