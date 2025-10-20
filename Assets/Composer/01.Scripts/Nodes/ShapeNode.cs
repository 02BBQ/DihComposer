using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class ShapeNode : Node
    {
        public ShapeType shapeType = ShapeType.Circle;
        public float size = 0.5f;
        public float smoothness = 0.01f;
        public Color fillColor = Color.white;
        public Color backgroundColor = Color.black;
        
        private RenderTexture outputTexture;
        private Material shapeMaterial;
        
        private static Shader cachedShader;
        
        protected override void InitializeSlots()
        {
            nodeName = "Shape";
            AddOutputSlot("texture_out", "Texture", DataType.Texture);
        }
        
        public override void Execute()
        {
            if (isExecuted) return;

            if (shapeMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Shape");
                }

                if (cachedShader != null)
                {
                    shapeMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Shape shader not found! Creating fallback texture.");
                    CreateFallbackTexture();
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            shapeMaterial.SetInt("_ShapeType", (int)shapeType);
            shapeMaterial.SetFloat("_Size", size);
            shapeMaterial.SetFloat("_Smoothness", smoothness);
            shapeMaterial.SetColor("_FillColor", fillColor);
            shapeMaterial.SetColor("_BackgroundColor", backgroundColor);

            TextureRenderer.DrawQuad(outputTexture, shapeMaterial);

            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }

        private void CreateFallbackTexture()
        {
            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Fill with solid color as fallback
            RenderTexture.active = outputTexture;
            GL.Clear(true, true, fillColor);
            RenderTexture.active = null;

            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }
    }
    
    public enum ShapeType
    {
        Circle = 0,
        Square = 1,
        Star = 2
    }
}