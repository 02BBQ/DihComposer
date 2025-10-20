using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class TimeNode : Node
    {
        public float speed = 1.0f;
        public float offset = 0.0f;
        public bool useLoop = false;
        public float loopDuration = 1.0f;

        private RenderTexture outputTexture;

        protected override void InitializeSlots()
        {
            nodeName = "Time";
            AddOutputSlot("value_out", "Value", DataType.Float);
            AddOutputSlot("texture_out", "Texture", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            // Calculate time value
            float timeValue = Time.time * speed + offset;

            // Apply loop if enabled
            if (useLoop && loopDuration > 0)
            {
                timeValue = timeValue % loopDuration;
            }

            // Output as float value
            SetOutputValue("value_out", timeValue);

            // Output as grayscale texture (for visual debugging)
            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Normalize time to 0-1 for texture
            float normalizedTime = useLoop ? (timeValue / loopDuration) : Mathf.Repeat(timeValue, 1.0f);
            Color col = new Color(normalizedTime, normalizedTime, normalizedTime, 1);

            RenderTexture.active = outputTexture;
            GL.Clear(true, true, col);
            RenderTexture.active = null;

            SetOutputValue("texture_out", outputTexture);

            isExecuted = true;
        }
    }
}
