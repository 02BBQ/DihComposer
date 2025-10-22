using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class SubtractNode : Node
    {
        [InspectorField("Subtract Value", Order = 0, Section = "➖ Subtract")]
        [VFXComposer.Core.Range(-2f, 2f)]
        [InspectorInfo("Subtracts inputs:\n• A - B (both textures)\n• A - value (texture only)\n• value (no inputs)")]
        public float subtractValue = 0.0f;

        private RenderTexture outputTexture;
        private Material subtractMaterial;
        private Material subtractTexMaterial;

        private static Shader cachedShader;
        private static Shader cachedTexShader;

        protected override void InitializeSlots()
        {
            nodeName = "Subtract";
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

            // Case 1: Texture - Texture
            if (hasA && hasB)
            {
                RenderTexture texA = GetInputValue<RenderTexture>("input_a");
                RenderTexture texB = GetInputValue<RenderTexture>("input_b");

                if (texA != null && texB != null)
                {
                    SubtractTextures(texA, texB);
                }
            }
            // Case 2: Texture - Float
            else if (hasA && !hasB)
            {
                RenderTexture texA = GetInputValue<RenderTexture>("input_a");
                if (texA != null)
                {
                    SubtractByValue(texA, subtractValue);
                }
            }
            // Case 3: Float - Texture
            else if (!hasA && hasB)
            {
                RenderTexture texB = GetInputValue<RenderTexture>("input_b");
                if (texB != null)
                {
                    SubtractByValue(texB, subtractValue);
                }
            }
            // Case 4: No inputs
            else
            {
                CreateSolidTexture(subtractValue);
            }

            isExecuted = true;
        }

        private void SubtractTextures(RenderTexture texA, RenderTexture texB)
        {
            if (subtractTexMaterial == null)
            {
                if (cachedTexShader == null)
                {
                    cachedTexShader = Shader.Find("VFXComposer/SubtractTexture");
                }

                if (cachedTexShader != null)
                {
                    subtractTexMaterial = new Material(cachedTexShader);
                }
                else
                {
                    Debug.LogError("SubtractTexture shader not found!");
                    CreatePassthrough(texA);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            subtractTexMaterial.SetTexture("_MainTex", texA);
            subtractTexMaterial.SetTexture("_SecondTex", texB);

            Graphics.Blit(texA, outputTexture, subtractTexMaterial);
            SetOutputValue("output", outputTexture);
        }

        private void SubtractByValue(RenderTexture tex, float value)
        {
            if (subtractMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Subtract");
                }

                if (cachedShader != null)
                {
                    subtractMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Subtract shader not found!");
                    CreatePassthrough(tex);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            subtractMaterial.SetTexture("_MainTex", tex);
            subtractMaterial.SetFloat("_SubtractValue", value);

            Graphics.Blit(tex, outputTexture, subtractMaterial);
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
