/*
 * BevelNode.cs
 *
 * Bevel effect - creates beveled edges on alpha masks
 * Applies distance field-based edge smoothing
 * Useful for creating 3D-looking buttons and UI elements
 */

using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class BevelNode : Node
    {
        [InspectorField("Size", Order = 0, Section = "🔷 Bevel")]
        [VFXComposer.Core.Range(0f, 0.5f)]
        [InspectorInfo("Bevel edge width")]
        public float size = 0.1f;

        [InspectorField("Smoothness", Order = 1, Section = "🔷 Bevel")]
        [VFXComposer.Core.Range(0f, 1f)]
        [InspectorInfo("Edge smoothness amount")]
        public float smoothness = 0.5f;

        [InspectorField("Inner", Order = 2, Section = "🔷 Bevel")]
        [InspectorInfo("Bevel inward instead of outward")]
        public bool inner = false;

        private RenderTexture outputTexture;
        private Material bevelMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Bevel";
            AddInputSlot("input", "Input", DataType.Texture);
            AddOutputSlot("output", "Output", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            RenderTexture inputTexture = GetInputValue<RenderTexture>("input");

            if (inputTexture == null)
            {
                Debug.LogWarning("[BevelNode] Missing input texture");
                isExecuted = true;
                return;
            }

            if (bevelMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Bevel");
                }

                if (cachedShader != null)
                {
                    bevelMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("[BevelNode] Bevel shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            bevelMaterial.SetTexture("_MainTex", inputTexture);
            bevelMaterial.SetFloat("_Size", size);
            bevelMaterial.SetFloat("_Smoothness", smoothness);
            bevelMaterial.SetFloat("_Inner", inner ? 1f : 0f);

            Graphics.Blit(inputTexture, outputTexture, bevelMaterial);

            SetOutputValue("output", outputTexture);
            isExecuted = true;
        }
    }
}
