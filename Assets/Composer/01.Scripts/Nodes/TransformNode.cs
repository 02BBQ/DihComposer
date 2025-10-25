using UnityEngine;
using VFXComposer.Rendering;

namespace VFXComposer.Core
{
    /// <summary>
    /// Transform 노드 - 텍스처 이동/회전/스케일
    /// </summary>
    public class TransformNode : Node
    {
        [InspectorField("Offset X", Order = 0, Section = "Position")]
        [VFXComposer.Core.Range(-1f, 1f)]
        public float offsetX = 0f;

        [InspectorField("Offset Y", Order = 1, Section = "Position")]
        [VFXComposer.Core.Range(-1f, 1f)]
        public float offsetY = 0f;

        [InspectorField("Rotation", Order = 2, Section = "Rotation")]
        [VFXComposer.Core.Range(0f, 360f)]
        [InspectorInfo("Rotation in degrees")]
        public float rotation = 0f;

        [InspectorField("Scale X", Order = 3, Section = "Scale")]
        [VFXComposer.Core.Range(0.1f, 5f)]
        public float scaleX = 1f;

        [InspectorField("Scale Y", Order = 4, Section = "Scale")]
        [VFXComposer.Core.Range(0.1f, 5f)]
        public float scaleY = 1f;

        private RenderTexture outputTexture;
        private Material transformMaterial;
        private static Shader cachedShader;

        protected override void InitializeSlots()
        {
            nodeName = "Transform";

            AddInputSlot("texture_in", "Texture", DataType.Texture);
            AddInputSlot("offset_x_in", "Offset X", DataType.Float);
            AddInputSlot("offset_y_in", "Offset Y", DataType.Float);
            AddInputSlot("rotation_in", "Rotation", DataType.Float);

            AddOutputSlot("texture_out", "Result", DataType.Texture);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            RenderTexture inputTexture = GetInputValue<RenderTexture>("texture_in");

            if (inputTexture == null)
            {
                Debug.LogWarning("TransformNode: Missing input texture");
                isExecuted = true;
                return;
            }

            // Get values from inputs or use field values
            float offsetXValue = HasInputConnection("offset_x_in")
                ? GetInputValue<float>("offset_x_in")
                : offsetX;

            float offsetYValue = HasInputConnection("offset_y_in")
                ? GetInputValue<float>("offset_y_in")
                : offsetY;

            float rotationValue = HasInputConnection("rotation_in")
                ? GetInputValue<float>("rotation_in")
                : rotation;

            if (transformMaterial == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Shader.Find("VFXComposer/Transform");
                }

                if (cachedShader != null)
                {
                    transformMaterial = new Material(cachedShader);
                }
                else
                {
                    Debug.LogError("Transform shader not found!");
                    isExecuted = true;
                    return;
                }
            }

            if (outputTexture == null)
            {
                outputTexture = TextureRenderer.CreateRenderTexture();
            }

            // Set shader properties
            transformMaterial.SetTexture("_MainTex", inputTexture);
            transformMaterial.SetFloat("_OffsetX", offsetXValue);
            transformMaterial.SetFloat("_OffsetY", offsetYValue);
            transformMaterial.SetFloat("_Rotation", rotationValue);
            transformMaterial.SetFloat("_ScaleX", scaleX);
            transformMaterial.SetFloat("_ScaleY", scaleY);

            TextureRenderer.DrawQuad(outputTexture, transformMaterial);

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
