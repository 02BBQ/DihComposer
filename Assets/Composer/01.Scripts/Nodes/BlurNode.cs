using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    /// <summary>
    /// Blur 노드 - 가우시안 블러 효과
    /// </summary>
    public class BlurNode : Node
    {
        [InspectorField("Blur Size", Order = 0, Section = "Blur")]
        [VFXComposer.Core.Range(0f, 10f)]
        [InspectorInfo("Size of blur effect")]
        public float blurSize = 1f;

        [InspectorField("Iterations", Order = 1, Section = "Blur")]
        [VFXComposer.Core.Range(1, 5)]
        [InspectorInfo("More iterations = smoother blur (slower)")]
        public int iterations = 2;

        private RenderTexture outputTexture;
        private RenderTexture tempTexture;
        private Material blurMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Blur";

            AddInputSlot("texture_in", "Texture", DataType.Texture);
            AddInputSlot("blur_size_in", "Blur Size", DataType.Float);

            AddOutputSlot("texture_out", "Result", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            RenderTexture inputTexture = GetInputValue<RenderTexture>("texture_in");

            if (inputTexture == null)
            {
                Debug.LogWarning("BlurNode: Missing input texture");
                isExecuted = true;
                return;
            }

            float blurSizeValue = HasInputConnection("blur_size_in")
                ? GetInputValue<float>("blur_size_in")
                : blurSize;

            if (blurMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Blur");
                }

                if (cachedShader != null)
                {
                    blurMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Blur shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            if (tempTexture == null)
            {
                tempTexture = TextureRenderer.CreateRenderTexture();
            }

            blurMaterial.SetFloat("_BlurSize", blurSizeValue);

            // Multi-pass blur for smoother result
            RenderTexture source = inputTexture;
            RenderTexture dest = tempTexture;

            for (int i = 0; i < iterations; i++)
            {
                blurMaterial.SetTexture("_MainTex", source);
                TextureRenderer.DrawQuad(dest, blurMaterial);

                // Swap buffers
                RenderTexture temp = source;
                source = dest;
                dest = (i == iterations - 1) ? outputTexture : temp;
            }

            // Ensure final result is in outputTexture
            if (source != outputTexture)
            {
                Graphics.Blit(source, outputTexture);
            }

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
