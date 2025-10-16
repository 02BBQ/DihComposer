using UnityEngine;

namespace VFXComposer.Core
{
    /// <summary>
    /// 최종 출력을 받는 노드 (그래프의 끝점)
    /// </summary>
    public class OutputNode : Node
    {
        // 최종 출력 결과
        public Color outputColor;
        public RenderTexture outputTexture;
        
        protected override void InitializeSlots()
        {
            nodeName = "Output";
            
            // 입력 슬롯 (여러 타입 가능)
            AddInputSlot("color_in", "Color", DataType.Color);
            AddInputSlot("texture_in", "Texture", DataType.Texture);
        }
        
        public override void Execute()
        {
            if (isExecuted) return;
            
            // Color 입력이 있으면 가져오기
            if (HasInputConnection("color_in"))
            {
                outputColor = GetInputValue<Color>("color_in");
            }
            
            // Texture 입력이 있으면 가져오기
            if (HasInputConnection("texture_in"))
            {
                outputTexture = GetInputValue<RenderTexture>("texture_in");
            }
            
            isExecuted = true;
        }
        
        /// <summary>
        /// 특정 슬롯에 연결이 있는지 확인
        /// </summary>
        private bool HasInputConnection(string slotId)
        {
            var slot = inputSlots.Find(s => s.id == slotId);
            return slot != null && slot.connectedSlot != null;
        }
        
        /// <summary>
        /// 최종 결과 Color 가져오기
        /// </summary>
        public Color GetOutputColor()
        {
            return outputColor;
        }
        
        /// <summary>
        /// 최종 결과 Texture 가져오기
        /// </summary>
        public RenderTexture GetOutputTexture()
        {
            return outputTexture;
        }
    }
}