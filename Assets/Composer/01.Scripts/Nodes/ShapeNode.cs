using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class ShapeNode : Node
    {
        [InspectorField("Shape Type", Order = 0, Section = "Shape Properties")]
        public ShapeType shapeType = ShapeType.Circle;

        [InspectorField("Size", Order = 1, Section = "Shape Properties")]
        [VFXComposer.Core.Range(0.1f, 1f)]
        public float size = 0.5f;

        [InspectorField("Smoothness", Order = 2, Section = "Shape Properties")]
        [VFXComposer.Core.Range(0f, 0.1f)]
        [InspectorInfo("Edge softness")]
        public float smoothness = 0.01f;

        [InspectorField("Fill Color", Order = 3, Section = "Colors")]
        public Color fillColor = Color.white;

        [InspectorField("Background Color", Order = 4, Section = "Colors")]
        public Color backgroundColor = Color.black;
        
        private RenderTexture outputTexture;
        private Material shapeMaterial;
        
        private static Shader cachedShader;
        
        protected override void InitializeSlots()
        {
            nodeName = "Shape";
            AddInputSlot("size_in", "Size", DataType.Float);
            AddInputSlot("smoothness_in", "Smoothness", DataType.Float);
            AddOutputSlot("texture_out", "Texture", DataType.Texture);
        }
        
        public override void Execute()
        {
            if (isExecuted) return;

            // Get values from inputs or use field values
            float sizeValue = HasInputConnection("size_in")
                ? GetInputValue<float>("size_in")
                : size;

            float smoothnessValue = HasInputConnection("smoothness_in")
                ? GetInputValue<float>("smoothness_in")
                : smoothness;

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
            shapeMaterial.SetFloat("_Size", sizeValue);
            shapeMaterial.SetFloat("_Smoothness", smoothnessValue);
            shapeMaterial.SetColor("_FillColor", fillColor);
            shapeMaterial.SetColor("_BackgroundColor", backgroundColor);

            TextureRenderer.DrawQuad(outputTexture, shapeMaterial);

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