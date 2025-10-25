using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class GradientNode : Node
    {
        [InspectorField("Color A", Order = 0, Section = "Gradient Colors")]
        public Color colorA = Color.black;

        [InspectorField("Color B", Order = 1, Section = "Gradient Colors")]
        public Color colorB = Color.white;

        [InspectorField("Gradient Type", Order = 2, Section = "Settings")]
        public GradientType gradientType = GradientType.Linear;

        [InspectorField("Angle (°)", Order = 3, Section = "Settings")]
        [Range(0f, 360f)]
        public float angle = 0f;
        
        private RenderTexture outputTexture;
        private Material gradientMaterial;
        
        private static Shader cachedShader;
        
        protected override void InitializeSlots()
        {
            nodeName = "Gradient";
            AddInputSlot("angle_in", "Angle", DataType.Float);
            AddOutputSlot("texture_out", "Texture", DataType.Texture);
        }
        
        public override void Execute()
        {
            if (isExecuted) return;

            // Get angle from input or use field value
            float angleValue = HasInputConnection("angle_in")
                ? GetInputValue<float>("angle_in")
                : angle;

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
                    Debug.LogError("Gradient shader not found! Creating fallback texture.");
                    CreateFallbackTexture();
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            gradientMaterial.SetColor("_ColorA", colorA);
            gradientMaterial.SetColor("_ColorB", colorB);
            gradientMaterial.SetFloat("_Angle", angleValue * Mathf.Deg2Rad);
            gradientMaterial.SetInt("_GradientType", (int)gradientType);

            TextureRenderer.DrawQuad(outputTexture, gradientMaterial);

            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }

        private bool HasInputConnection(string slotId)
        {
            var slot = inputSlots.Find(s => s.id == slotId);
            return slot != null && slot.connectedSlot != null;
        }

        private void CreateFallbackTexture()
        {
            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Fill with colorB as fallback
            RenderTexture.active = outputTexture;
            GL.Clear(true, true, colorB);
            RenderTexture.active = null;

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