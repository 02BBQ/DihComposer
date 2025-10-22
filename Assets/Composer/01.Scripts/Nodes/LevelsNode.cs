using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    /// <summary>
    /// Levels 노드 - 색상 레벨 조정 (Photoshop/AE Levels와 동일)
    /// </summary>
    public class LevelsNode : Node
    {
        [InspectorField("Input Black", Order = 0, Section = "📥 Input Levels")]
        [VFXComposer.Core.Range(0f, 1f)]
        public float inputBlack = 0f;

        [InspectorField("Input White", Order = 1, Section = "📥 Input Levels")]
        [VFXComposer.Core.Range(0f, 1f)]
        public float inputWhite = 1f;

        [InspectorField("Gamma", Order = 2, Section = "⚡ Midtones")]
        [VFXComposer.Core.Range(0.1f, 10f)]
        [InspectorInfo("Adjust midtone brightness")]
        public float gamma = 1f;

        [InspectorField("Output Black", Order = 3, Section = "📤 Output Levels")]
        [VFXComposer.Core.Range(0f, 1f)]
        public float outputBlack = 0f;

        [InspectorField("Output White", Order = 4, Section = "📤 Output Levels")]
        [VFXComposer.Core.Range(0f, 1f)]
        public float outputWhite = 1f;

        private RenderTexture outputTexture;
        private Material levelsMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Levels";

            AddInputSlot("texture_in", "Texture", DataType.Texture);
            AddOutputSlot("texture_out", "Result", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            RenderTexture inputTexture = GetInputValue<RenderTexture>("texture_in");

            if (inputTexture == null)
            {
                Debug.LogWarning("LevelsNode: Missing input texture");
                isExecuted = true;
                return;
            }

            if (levelsMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Levels");
                }

                if (cachedShader != null)
                {
                    levelsMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Levels shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Set shader properties
            levelsMaterial.SetTexture("_MainTex", inputTexture);
            levelsMaterial.SetFloat("_InputBlack", inputBlack);
            levelsMaterial.SetFloat("_InputWhite", inputWhite);
            levelsMaterial.SetFloat("_Gamma", gamma);
            levelsMaterial.SetFloat("_OutputBlack", outputBlack);
            levelsMaterial.SetFloat("_OutputWhite", outputWhite);

            TextureRenderer.DrawQuad(outputTexture, levelsMaterial);

            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }
    }
}
