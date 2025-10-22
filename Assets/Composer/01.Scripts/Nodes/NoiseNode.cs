using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class NoiseNode : Node
    {
        [InspectorField("Noise Type", Order = 0, Section = "🌀 Noise Properties")]
        public NoiseType noiseType = NoiseType.Perlin;

        [InspectorField("Scale", Order = 1, Section = "🌀 Noise Properties")]
        [Range(0.1f, 20f)]
        public float scale = 5f;

        [InspectorField("Octaves", Order = 2, Section = "🌀 Noise Properties")]
        [Range(1, 8)]
        public int octaves = 4;

        [InspectorField("Persistence", Order = 3, Section = "🌀 Noise Properties")]
        [Range(0f, 1f)]
        public float persistence = 0.5f;

        [InspectorField("Offset", Order = 4, Section = "🌀 Noise Properties")]
        public Vector2 offset = Vector2.zero;
        
        private RenderTexture outputTexture;
        private Material noiseMaterial;
        
        private static Shader cachedShader;
        
        protected override void InitializeSlots()
        {
            nodeName = "Noise";
            AddInputSlot("scale_in", "Scale", DataType.Float);
            AddInputSlot("octaves_in", "Octaves", DataType.Float);
            AddInputSlot("persistence_in", "Persistence", DataType.Float);
            AddInputSlot("offset_x_in", "Offset X", DataType.Float);
            AddInputSlot("offset_y_in", "Offset Y", DataType.Float);
            AddOutputSlot("texture_out", "Texture", DataType.Texture);
        }
        
        public override void Execute()
        {
            if (isExecuted) return;

            // Get values from inputs or use field values
            float scaleValue = HasInputConnection("scale_in")
                ? GetInputValue<float>("scale_in")
                : scale;

            int octavesValue = HasInputConnection("octaves_in")
                ? Mathf.RoundToInt(GetInputValue<float>("octaves_in"))
                : octaves;

            float persistenceValue = HasInputConnection("persistence_in")
                ? GetInputValue<float>("persistence_in")
                : persistence;

            float offsetX = HasInputConnection("offset_x_in")
                ? GetInputValue<float>("offset_x_in")
                : offset.x;

            float offsetY = HasInputConnection("offset_y_in")
                ? GetInputValue<float>("offset_y_in")
                : offset.y;

            if (noiseMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Noise");
                }

                if (cachedShader != null)
                {
                    noiseMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Noise shader not found! Creating fallback texture.");
                    CreateFallbackTexture();
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            noiseMaterial.SetInt("_NoiseType", (int)noiseType);
            noiseMaterial.SetFloat("_Scale", scaleValue);
            noiseMaterial.SetInt("_Octaves", octavesValue);
            noiseMaterial.SetFloat("_Persistence", persistenceValue);
            noiseMaterial.SetVector("_Offset", new Vector2(offsetX, offsetY));

            TextureRenderer.DrawQuad(outputTexture, noiseMaterial);

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

            // Fill with white as fallback
            RenderTexture.active = outputTexture;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = null;

            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }
    }
    
    public enum NoiseType
    {
        Perlin = 0,
        Voronoi = 1
    }
}