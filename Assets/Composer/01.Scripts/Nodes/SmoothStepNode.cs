/*
 * SmoothStepNode.cs
 *
 * Smooth step function - smoothly interpolates between 0 and 1
 * Values below edge0 = 0, above edge1 = 1, between = smooth curve
 * Creates soft gradients and smooth transitions
 */

using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class SmoothStepNode : Node
    {
        [InspectorField("Edge 0 (Lower)", Order = 0, Section = "⚡ Smooth Step")]
        [VFXComposer.Core.Range(0f, 1f)]
        [InspectorInfo("Lower threshold - values below become 0")]
        public float edge0 = 0.3f;

        [InspectorField("Edge 1 (Upper)", Order = 1, Section = "⚡ Smooth Step")]
        [VFXComposer.Core.Range(0f, 1f)]
        [InspectorInfo("Upper threshold - values above become 1")]
        public float edge1 = 0.7f;

        private RenderTexture outputTexture;
        private Material smoothStepMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Smooth Step";
            AddInputSlot("input", "Input", DataType.Texture);
            AddOutputSlot("output", "Output", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            RenderTexture inputTexture = GetInputValue<RenderTexture>("input");

            if (inputTexture == null)
            {
                Debug.LogWarning("[SmoothStepNode] Missing input texture");
                isExecuted = true;
                return;
            }

            if (smoothStepMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/SmoothStep");
                }

                if (cachedShader != null)
                {
                    smoothStepMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("[SmoothStepNode] SmoothStep shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            smoothStepMaterial.SetTexture("_MainTex", inputTexture);
            smoothStepMaterial.SetFloat("_Edge0", edge0);
            smoothStepMaterial.SetFloat("_Edge1", edge1);

            Graphics.Blit(inputTexture, outputTexture, smoothStepMaterial);

            SetOutputValue("output", outputTexture);
            isExecuted = true;
        }
    }
}
