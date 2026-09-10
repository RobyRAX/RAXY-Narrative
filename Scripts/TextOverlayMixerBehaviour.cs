using UnityEngine;
using UnityEngine.Playables;

namespace RAXY.Narrative
{
    public class TextOverlayMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var manager = playerData as TextOverlayManager;
            if (manager == null)
                return;

            var inputCount = playable.GetInputCount();
            var blendedAlpha = 0f;
            var blendedRgb = Color.clear;
            var totalWeight = 0f;
            var bestTextWeight = 0f;
            var selectedText = string.Empty;

            for (var i = 0; i < inputCount; i++)
            {
                var weight = playable.GetInputWeight(i);
                if (weight <= 0f)
                    continue;

                var inputPlayable = (ScriptPlayable<TextOverlayBehaviour>)playable.GetInput(i);
                var behaviour = inputPlayable.GetBehaviour();
                var time = (float)inputPlayable.GetTime();
                var duration = (float)inputPlayable.GetDuration();

                var alpha = ColorOverlayBehaviour.EvaluateHoldAlpha(
                    time,
                    duration,
                    behaviour.fadeIn,
                    behaviour.fadeOut);

                blendedAlpha += alpha * weight;
                blendedRgb += behaviour.color * weight;
                totalWeight += weight;

                if (weight > bestTextWeight)
                {
                    bestTextWeight = weight;
                    selectedText = behaviour.text ?? string.Empty;
                }
            }

            if (totalWeight <= 0f)
            {
                var clearColor = manager.CurrentColor;
                clearColor.a = 0f;
                manager.SetTextOverlay(clearColor, 0f);
                return;
            }

            blendedRgb /= totalWeight;
            blendedRgb.a = blendedAlpha;
            manager.SetText(selectedText);
            manager.SetTextOverlay(blendedRgb, 0f);
        }
    }
}
