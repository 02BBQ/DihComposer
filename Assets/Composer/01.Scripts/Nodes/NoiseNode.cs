using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    public class NoiseNode : Node
    {
        public NoiseType noiseType = NoiseType.Perlin;
        public float scale = 5f;
        public int octaves = 4;
        public float persistence = 0.5f;
        public Vector2 offset = Vector2.zero;
        
        private RenderTexture outputTexture;
        private Material noiseMaterial;
        
        private static Shader cachedShader;
        
        protected override void InitializeSlots()
        {
            nodeName = "Noise";
            AddOutputSlot("texture_out", "Texture", DataType.Texture);
        }
        
        public override void Execute()
        {
            if (isExecuted) return;
            
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
                    Debug.LogError("Noise shader not found!");
                    return;
                }
            }
            
            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }
            
            noiseMaterial.SetInt("_NoiseType", (int)noiseType);
            noiseMaterial.SetFloat("_Scale", scale);
            noiseMaterial.SetInt("_Octaves", octaves);
            noiseMaterial.SetFloat("_Persistence", persistence);
            noiseMaterial.SetVector("_Offset", offset);
            
            TextureRenderer.DrawQuad(outputTexture, noiseMaterial);
            
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