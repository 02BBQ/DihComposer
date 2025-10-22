using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    /// <summary>
    /// Displacement 노드 - 텍스처를 다른 텍스처의 값으로 왜곡
    /// </summary>
    public class DisplaceNode : Node
    {
        [InspectorField("Intensity", Order = 0, Section = "🌊 Displacement")]
        [VFXComposer.Core.Range(0f, 1f)]
        [InspectorInfo("Strength of displacement effect")]
        public float intensity = 0.1f;

        [InspectorField("Angle Offset", Order = 1, Section = "🌊 Displacement")]
        [VFXComposer.Core.Range(0f, 360f)]
        [InspectorInfo("Rotate displacement direction")]
        public float angleOffset = 0f;

        private RenderTexture outputTexture;
        private Material displaceMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Displace";

            AddInputSlot("base_texture", "Base Texture", DataType.Texture);
            AddInputSlot("displacement_map", "Displacement Map", DataType.Texture);
            AddInputSlot("intensity_in", "Intensity", DataType.Float);

            AddOutputSlot("texture_out", "Result", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            // Get input textures
            RenderTexture baseTexture = GetInputValue<RenderTexture>("base_texture");
            RenderTexture displacementMap = GetInputValue<RenderTexture>("displacement_map");

            if (baseTexture == null || displacementMap == null)
            {
                Debug.LogWarning("DisplaceNode: Missing input textures");
                isExecuted = true;
                return;
            }

            // Get intensity from input or use field value
            float intensityValue = HasInputConnection("intensity_in")
                ? GetInputValue<float>("intensity_in")
                : intensity;

            if (displaceMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Displace");
                }

                if (cachedShader != null)
                {
                    displaceMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Displace shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Set shader properties
            displaceMaterial.SetTexture("_MainTex", baseTexture);
            displaceMaterial.SetTexture("_DispTex", displacementMap);
            displaceMaterial.SetFloat("_Intensity", intensityValue);
            displaceMaterial.SetFloat("_AngleOffset", angleOffset);

            TextureRenderer.DrawQuad(outputTexture, displaceMaterial);

            SetOutputValue("texture_out", outputTexture);
            isExecuted = true;
        }

        private bool HasInputConnection(string slotId)
        {
            var slot = inputSlots.Find(s => s.id == slotId);
            return slot != null && slot.connectedSlot != null;
        }
    }
}
