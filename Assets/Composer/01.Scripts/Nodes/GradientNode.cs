using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class GradientNode : Node
    {
        public Color colorA = Color.black;
        public Color colorB = Color.white;
        public GradientType gradientType = GradientType.Linear;
        public float angle = 0f;
        
        private RenderTexture outputTexture;
        private Material gradientMaterial;
        
        private static Shader cachedShader;
        
        protected override void InitializeSlots()
        {
            nodeName = "Gradient";
            AddOutputSlot("texture_out", "Texture", DataType.Texture);
        }
        
        public override void Execute()
        {
            if (isExecuted) return;
            
            if (gradientMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Gradient");
                }
                
                if (cachedShader != null)
                {
                    gradientMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Gradient shader not found!");
                    return;
                }
            }
            
            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }
            
            gradientMaterial.SetColor("_ColorA", colorA);
            gradientMaterial.SetColor("_ColorB", colorB);
            gradientMaterial.SetFloat("_Angle", angle * Mathf.Deg2Rad);
            gradientMaterial.SetInt("_GradientType", (int)gradientType);
            
            TextureRenderer.DrawQuad(outputTexture, gradientMaterial);
            
            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }
    }
    
    public enum GradientType
    {
        Linear = 0,
        Radial = 1
    }
}