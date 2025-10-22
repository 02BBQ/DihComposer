using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class PowerNode : Node
    {
        [InspectorField("Exponent", Order = 0, Section = "⚡ Power")]
        [VFXComposer.Core.Range(0.1f, 10f)]
        [InspectorInfo("Power operation:\n• A ^ B (both textures)\n• A ^ exp (texture only)\n• exp (no inputs)")]
        public float exponent = 2.0f;

        private RenderTexture outputTexture;
        private Material powerMaterial;
        private Material powerTexMaterial;

        private static Shader cachedShader;
        private static Shader cachedTexShader;

        protected override void InitializeSlots()
        {
            nodeName = "Power";
            AddInputSlot("input_a", "Base", DataType.Texture);
            AddInputSlot("input_b", "Exponent", DataType.Texture);
            AddOutputSlot("output", "Output", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            var slotA = inputSlots.Find(s => s.id == "input_a");
            var slotB = inputSlots.Find(s => s.id == "input_b");

            bool hasA = slotA != null && slotA.connectedSlot != null;
            bool hasB = slotB != null && slotB.connectedSlot != null;

            // Case 1: Texture ^ Texture
            if (hasA && hasB)
            {
                RenderTexture texA = GetInputValue<RenderTexture>("input_a");
                RenderTexture texB = GetInputValue<RenderTexture>("input_b");

                if (texA != null && texB != null)
                {
                    PowerTextures(texA, texB);
                }
            }
            // Case 2: Texture ^ Float
            else if (hasA && !hasB)
            {
                RenderTexture texA = GetInputValue<RenderTexture>("input_a");
                if (texA != null)
                {
                    PowerByValue(texA, exponent);
                }
            }
            // Case 3: Float ^ Texture (less common but supported)
            else if (!hasA && hasB)
            {
                RenderTexture texB = GetInputValue<RenderTexture>("input_b");
                if (texB != null)
                {
                    PowerByValue(texB, exponent);
                }
            }
            // Case 4: No inputs
            else
            {
                CreateSolidTexture(exponent);
            }

            isExecuted = true;
        }

        private void PowerTextures(RenderTexture texA, RenderTexture texB)
        {
            if (powerTexMaterial == null)
            {
                if (cachedTexShader == null)
                {
                    cachedTexShader = Shader.Find("VFXComposer/PowerTexture");
                }

                if (cachedTexShader != null)
                {
                    powerTexMaterial = new Material(cachedTexShader);
                }
                else
                {
                    Debug.LogError("PowerTexture shader not found!");
                    CreatePassthrough(texA);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            powerTexMaterial.SetTexture("_MainTex", texA);
            powerTexMaterial.SetTexture("_SecondTex", texB);

            Graphics.Blit(texA, outputTexture, powerTexMaterial);
            SetOutputValue("output", outputTexture);
        }

        private void PowerByValue(RenderTexture tex, float exp)
        {
            if (powerMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Power");
                }

                if (cachedShader != null)
                {
                    powerMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Power shader not found!");
                    CreatePassthrough(tex);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            powerMaterial.SetTexture("_MainTex", tex);
            powerMaterial.SetFloat("_Exponent", exp);

            Graphics.Blit(tex, outputTexture, powerMaterial);
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
