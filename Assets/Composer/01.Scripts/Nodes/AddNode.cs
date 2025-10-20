using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class AddNode : Node
    {
        public float addValue = 0.0f;

        private RenderTexture outputTexture;
        private Material addMaterial;
        private Material addTexMaterial;

        private static Shader cachedShader;
        private static Shader cachedTexShader;

        protected override void InitializeSlots()
        {
            nodeName = "Add";
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

            // Case 1: Texture + Texture
            if (hasA && hasB)
            {
                RenderTexture texA = GetInputValue<RenderTexture>("input_a");
                RenderTexture texB = GetInputValue<RenderTexture>("input_b");

                if (texA != null && texB != null)
                {
                    AddTextures(texA, texB);
                }
            }
            // Case 2: Texture + Float
            else if (hasA && !hasB)
            {
                RenderTexture texA = GetInputValue<RenderTexture>("input_a");
                if (texA != null)
                {
                    AddByValue(texA, addValue);
                }
            }
            // Case 3: Float + Texture
            else if (!hasA && hasB)
            {
                RenderTexture texB = GetInputValue<RenderTexture>("input_b");
                if (texB != null)
                {
                    AddByValue(texB, addValue);
                }
            }
            // Case 4: No inputs
            else
            {
                CreateSolidTexture(addValue);
            }

            isExecuted = true;
        }

        private void AddTextures(RenderTexture texA, RenderTexture texB)
        {
            if (addTexMaterial == null)
            {
                if (cachedTexShader == null)
                {
                    cachedTexShader = Shader.Find("VFXComposer/AddTexture");
                }

                if (cachedTexShader != null)
                {
                    addTexMaterial = new Material(cachedTexShader);
                }
                else
                {
                    Debug.LogError("AddTexture shader not found!");
                    CreatePassthrough(texA);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            addTexMaterial.SetTexture("_MainTex", texA);
            addTexMaterial.SetTexture("_SecondTex", texB);

            Graphics.Blit(texA, outputTexture, addTexMaterial);
            SetOutputValue("output", outputTexture);
        }

        private void AddByValue(RenderTexture tex, float value)
        {
            if (addMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Add");
                }

                if (cachedShader != null)
                {
                    addMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Add shader not found!");
                    CreatePassthrough(tex);
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            addMaterial.SetTexture("_MainTex", tex);
            addMaterial.SetFloat("_AddValue", value);

            Graphics.Blit(tex, outputTexture, addMaterial);
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
