using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class MultiplyNode : Node
    {
        [InspectorField("Multiplier", Order = 0, Section = "✖️ Multiply")]
        [VFXComposer.Core.Range(0f, 5f)]
        [InspectorInfo("Multiplies inputs:\n• A * B (both textures)\n• A * value (texture only)\n• value (no inputs)")]
        public float multiplier = 1.0f;

        private RenderTexture outputTexture;
        private Material multiplyMaterial;
        private Material multiplyTexMaterial;

        private static Shader cachedShader;
        private static Shader cachedTexShader;

        protected override void InitializeSlots()
        {
            nodeName = "Multiply";
            AddInputSlot("input_a", "A", DataType.Texture);
            AddInputSlot("input_b", "B", DataType.Texture);
            AddOutputSlot("output", "Output", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            var slotA = inputSlots.Find(s => s.id == "input_a");
            var slotB = inputSlots.Find(s => s.id == "input_b");

            bool hasA = slotA != null && slotA.connectedSlot != null;
            bool hasB = slotB != null && slotB.connectedSlot != null;

            // Case 1: Texture * Texture
            if (hasA && hasB)
            {
                RenderTexture texA = GetInputValue<RenderTexture>("input_a");
                RenderTexture texB = GetInputValue<RenderTexture>("input_b");

                if (texA != null && texB != null)
                {
                    MultiplyTextures(texA, texB);
                }
            }
            // Case 2: Texture * Float (using multiplier field)
            else if (hasA && !hasB)
            {
                RenderTexture texA = GetInputValue<RenderTexture>("input_a");
                if (texA != null)
                {
                    MultiplyByValue(texA, multiplier);
                }
            }
            // Case 3: Float * Texture
            else if (!hasA && hasB)
            {
                RenderTexture texB = GetInputValue<RenderTexture>("input_b");
                if (texB != null)
                {
                    MultiplyByValue(texB, multiplier);
                }
            }
            // Case 4: No inputs - create solid color texture with multiplier value
            else
            {
                CreateSolidTexture(multiplier);
            }

            isExecuted = true;
        }

        private void MultiplyTextures(RenderTexture texA, RenderTexture texB)
        {
            if (multiplyTexMaterial == null)
            {
                if (cachedTexShader == null)
                {
                    cachedTexShader = Shader.Find("VFXComposer/MultiplyTexture");
                }

                if (cachedTexShader != null)
                {
                    multiplyTexMaterial = new Material(cachedTexShader);
                }
                else
                {
                    Debug.LogError("MultiplyTexture shader not found!");
                    CreatePassthrough(texA);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            multiplyTexMaterial.SetTexture("_MainTex", texA);
            multiplyTexMaterial.SetTexture("_SecondTex", texB);

            Graphics.Blit(texA, outputTexture, multiplyTexMaterial);
            SetOutputValue("output", outputTexture);
        }

        private void MultiplyByValue(RenderTexture tex, float value)
        {
            if (multiplyMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Multiply");
                }

                if (cachedShader != null)
                {
                    multiplyMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Multiply shader not found!");
                    CreatePassthrough(tex);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            multiplyMaterial.SetTexture("_MainTex", tex);
            multiplyMaterial.SetFloat("_Multiplier", value);

            Graphics.Blit(tex, outputTexture, multiplyMaterial);
            SetOutputValue("output", outputTexture);
        }

        private void CreateSolidTexture(float value)
        {
            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            Color col = new Color(value, value, value, 1);
            RenderTexture.active = outputTexture;
            GL.Clear(true, true, col);
            RenderTexture.active = null;

            SetOutputValue("output", outputTexture);
        }

        private void CreatePassthrough(RenderTexture input)
        {
            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            Graphics.Blit(input, outputTexture);
            SetOutputValue("output", outputTexture);
        }
    }
}
