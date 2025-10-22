using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class BlendNode : Node
    {
        [InspectorField("Blend Mode", Order = 0, Section = "🎨 Blend Settings")]
        public BlendMode blendMode = BlendMode.Normal;

        [InspectorField("Opacity", Order = 1, Section = "🎨 Blend Settings")]
        [Range(0f, 1f)]
        [InspectorInfo("Connect two textures:\n• Base: Bottom layer\n• Blend: Top layer")]
        public float opacity = 1f;

        private RenderTexture outputTexture;
        private Material blendMaterial;

        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Blend";
            AddInputSlot("texture_a", "Base", DataType.Texture);
            AddInputSlot("texture_b", "Blend", DataType.Texture);
            AddOutputSlot("texture_out", "Result", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            // Get input textures
            RenderTexture textureA = GetInputValue<RenderTexture>("texture_a");
            RenderTexture textureB = GetInputValue<RenderTexture>("texture_b");

            if (textureA == null || textureB == null)
            {
                Debug.LogWarning("BlendNode: Missing input textures");
                isExecuted = true;
                return;
            }

            if (blendMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Blend");
                }

                if (cachedShader != null)
                {
                    blendMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Blend shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Set material properties
            blendMaterial.SetTexture("_BaseTexture", textureA);
            blendMaterial.SetTexture("_BlendTexture", textureB);
            blendMaterial.SetInt("_BlendMode", (int)blendMode);
            blendMaterial.SetFloat("_Opacity", opacity);

            TextureRenderer.DrawQuad(outputTexture, blendMaterial);

            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }
    }

    public enum BlendMode
    {
        Normal = 0,      // 일반
        Multiply = 1,    // 곱하기
        Screen = 2,      // 스크린
        Overlay = 3,     // 오버레이
        Add = 4,         // 더하기
        Subtract = 5     // 빼기
    }
}
