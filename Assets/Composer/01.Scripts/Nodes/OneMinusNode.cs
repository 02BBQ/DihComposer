using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class OneMinusNode : Node
    {
        [InspectorField("Value", Order = 0, Section = "➖ One Minus")]
        [VFXComposer.Core.Range(0f, 1f)]
        [InspectorInfo("Calculates 1 - Input:\n• With texture: inverts each pixel\n• Without input: returns 1 - value")]
        public float value = 0.0f;

        private RenderTexture outputTexture;
        private Material oneMinusMaterial;

        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "One Minus";
            AddInputSlot("input", "Input", DataType.Texture);
            AddOutputSlot("output", "Output", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            var inputSlot = inputSlots.Find(s => s.id == "input");
            bool hasInput = inputSlot != null && inputSlot.connectedSlot != null;

            if (hasInput)
            {
                RenderTexture inputTex = GetInputValue<RenderTexture>("input");
                if (inputTex != null)
                {
                    ApplyOneMinus(inputTex);
                }
            }
            else
            {
                // No input: create solid texture with 1 - value
                CreateSolidTexture(1f - value);
            }

            isExecuted = true;
        }

        private void ApplyOneMinus(RenderTexture input)
        {
            if (oneMinusMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/OneMinus");
                }

                if (cachedShader != null)
                {
                    oneMinusMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("OneMinus shader not found!");
                    CreatePassthrough(input);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            oneMinusMaterial.SetTexture("_MainTex", input);

            Graphics.Blit(input, outputTexture, oneMinusMaterial);
            SetOutputValue("output", outputTexture);
        }

        private void CreateSolidTexture(float val)
        {
            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            Color col = new Color(val, val, val, 1);
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
