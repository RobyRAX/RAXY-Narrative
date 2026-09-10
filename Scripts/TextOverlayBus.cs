using Sirenix.OdinInspector;
using UnityEngine;

namespace RAXY.Narrative
{
    public class TextOverlayBus : MonoBehaviour
    {
        [SerializeField]
        string text = "";

        [SerializeField]
        Color color = Color.white;

        [SerializeField]
        float fadeDuration = 0.33f;

        [SerializeField]
        bool force;

        [Button]
        public void Execute()
        {
            TextOverlayManager.Instance?.SetTextOverlay(text, color, fadeDuration, force);
        }
    }
}
