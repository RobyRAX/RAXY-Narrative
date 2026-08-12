using UnityEngine;
using UnityEngine.Playables;

namespace RAXY.Narrative
{
    public class ColorOverlayBehaviour : PlayableBehaviour
    {
        public Color color = Color.black;
        public float fadeIn = 0.5f;
        public float fadeOut = 0.5f;

        public static float EvaluateHoldAlpha(float time, float duration, float fadeIn, float fadeOut)
        {
            if (duration <= 0f)
                return 0f;

            var fi = Mathf.Max(0f, fadeIn);
            var fo = Mathf.Max(0f, fadeOut);

            if (fi + fo > duration)
            {
                var scale = duration / (fi + fo);
                fi *= scale;
                fo *= scale;
            }

            if (fi > 0f && time < fi)
                return Mathf.Clamp01(time / fi);

            if (fo > 0f && time > duration - fo)
                return Mathf.Clamp01((duration - time) / fo);

            return 1f;
        }
    }
}
