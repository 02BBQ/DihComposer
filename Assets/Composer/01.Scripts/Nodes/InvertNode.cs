using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    /// <summary>
    /// Invert 노드 - 색상 반전 (네거티브 효과)
    /// </summary>
    public class InvertNode : Node
    {
        [InspectorField("Invert Amount", Order = 0, Section = "Invert")]
        [VFXComposer.Core.Range(0f, 1f)]
        [InspectorInfo("0 = original, 1 = fully inverted")]
        public float invertAmount = 1f;

        private RenderTexture outputTexture;
        private Material invertMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Invert";

            AddInputSlot("texture_in", "Texture", DataType.Texture);
            AddInputSlot("amount_in", "Amount", DataType.Float);

            AddOutputSlot("texture_out", "Result", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            RenderTexture inputTexture = GetInputValue<RenderTexture>("texture_in");

            if (inputTexture == null)
            {
                Debug.LogWarning("InvertNode: Missing input texture");
                isExecuted = true;
                return;
            }

            // Get amount from input or use field value
            float amountValue = HasInputConnection("amount_in")
                ? GetInputValue<float>("amount_in")
                : invertAmount;

            if (invertMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Invert");
                }

                if (cachedShader != null)
                {
                    invertMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Invert shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Set shader properties
            invertMaterial.SetTexture("_MainTex", inputTexture);
            invertMaterial.SetFloat("_InvertAmount", amountValue);

            TextureRenderer.DrawQuad(outputTexture, invertMaterial);

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
