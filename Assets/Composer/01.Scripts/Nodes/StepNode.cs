/*
 * StepNode.cs
 *
 * Step function node - converts input to 0 or 1 based on threshold
 * If input >= threshold, output 1, else output 0
 * Useful for creating masks and hard edges
 */

using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class StepNode : Node
    {
        [InspectorField("Threshold", Order = 0, Section = "⚡ Step")]
        [VFXComposer.Core.Range(0f, 1f)]
        [InspectorInfo("Values below threshold become 0, above become 1")]
        public float threshold = 0.5f;

        private RenderTexture outputTexture;
        private Material stepMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Step";
            AddInputSlot("input", "Input", DataType.Texture);
            AddOutputSlot("output", "Output", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            RenderTexture inputTexture = GetInputValue<RenderTexture>("input");

            if (inputTexture == null)
            {
                Debug.LogWarning("[StepNode] Missing input texture");
                isExecuted = true;
                return;
            }

            if (stepMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Step");
                }

                if (cachedShader != null)
                {
                    stepMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("[StepNode] Step shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            stepMaterial.SetTexture("_MainTex", inputTexture);
            stepMaterial.SetFloat("_Threshold", threshold);

            Graphics.Blit(inputTexture, outputTexture, stepMaterial);

            SetOutputValue("output", outputTexture);
            isExecuted = true;
        }
    }
}
