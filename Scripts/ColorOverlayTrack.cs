using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace RAXY.Narrative
{
    [TrackColor(0.15f, 0.15f, 0.15f)]
    [TrackClipType(typeof(ColorOverlayClip))]
    [TrackBindingType(typeof(ColorOverlayManager))]
    public class ColorOverlayTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<ColorOverlayMixerBehaviour>.Create(graph, inputCount);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
#if UNITY_EDITOR
            var binding = director.GetGenericBinding(this) as ColorOverlayManager;
            if (binding != null && binding.OverlayImage != null)
                driver.AddFromName<Image>(binding.OverlayImage.gameObject, "m_Color");
#endif
            base.GatherProperties(director, driver);
        }
    }
}
