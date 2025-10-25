using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    /// <summary>
    /// HSV Adjust 노드 - Hue/Saturation/Value 조정
    /// </summary>
    public class HSVAdjustNode : Node
    {
        [InspectorField("Hue Shift", Order = 0, Section = "HSV Adjust")]
        [VFXComposer.Core.Range(-180f, 180f)]
        [InspectorInfo("Rotate hue (color wheel)")]
        public float hueShift = 0f;

        [InspectorField("Saturation", Order = 1, Section = "HSV Adjust")]
        [VFXComposer.Core.Range(0f, 2f)]
        [InspectorInfo("0 = grayscale, 1 = original, 2 = doubled")]
        public float saturation = 1f;

        [InspectorField("Value (Brightness)", Order = 2, Section = "HSV Adjust")]
        [VFXComposer.Core.Range(0f, 2f)]
        [InspectorInfo("Overall brightness multiplier")]
        public float value = 1f;

        private RenderTexture outputTexture;
        private Material hsvMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "HSV Adjust";

            AddInputSlot("texture_in", "Texture", DataType.Texture);
            AddInputSlot("hue_in", "Hue Shift", DataType.Float);
            AddInputSlot("saturation_in", "Saturation", DataType.Float);
            AddInputSlot("value_in", "Value", DataType.Float);

            AddOutputSlot("texture_out", "Result", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            RenderTexture inputTexture = GetInputValue<RenderTexture>("texture_in");

            if (inputTexture == null)
            {
                Debug.LogWarning("HSVAdjustNode: Missing input texture");
                isExecuted = true;
                return;
            }

            // Get values from inputs or use field values
            float hueValue = HasInputConnection("hue_in")
                ? GetInputValue<float>("hue_in")
                : hueShift;

            float satValue = HasInputConnection("saturation_in")
                ? GetInputValue<float>("saturation_in")
                : saturation;

            float valValue = HasInputConnection("value_in")
                ? GetInputValue<float>("value_in")
                : value;

            if (hsvMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/HSVAdjust");
                }

                if (cachedShader != null)
                {
                    hsvMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("HSVAdjust shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Set shader properties
            hsvMaterial.SetTexture("_MainTex", inputTexture);
            hsvMaterial.SetFloat("_HueShift", hueValue);
            hsvMaterial.SetFloat("_Saturation", satValue);
            hsvMaterial.SetFloat("_Value", valValue);

            TextureRenderer.DrawQuad(outputTexture, hsvMaterial);

            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }

        private bool HasInputConnection(string slotId)
        {
            var slot = inputSlots.Find(s => s.id == slotId);
            return slot != null && slot.connectedSlot != null;
        }
    }
}
