using UnityEngine;
using UnityEngine.Playables;

namespace RAXY.Narrative
{
    public class TextOverlayBehaviour : PlayableBehaviour
    {
        public string text = "";
        public Color color = Color.white;
        public float fadeIn = 0.5f;
        public float fadeOut = 0.5f;
    }
}
