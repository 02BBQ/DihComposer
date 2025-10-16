using UnityEngine;

namespace VFXComposer.Core
{
    /// <summary>
    /// 두 노드 슬롯 사이의 연결을 나타내는 클래스
    /// </summary>
    [System.Serializable]
    public class NodeConnection
    {
        public string id;
        
        // 출력 슬롯 (연결의 시작점)
        public NodeSlot outputSlot;
        
        // 입력 슬롯 (연결의 끝점)
        public NodeSlot inputSlot;
        
        // 연결 색상 (데이터 타입에 따라 다른 색상)
        public Color connectionColor;
        
        public NodeConnection(NodeSlot output, NodeSlot input)
        {
            id = System.Guid.NewGuid().ToString();
            outputSlot = output;
            inputSlot = input;
            
            // 데이터 타입에 따른 색상 설정
            connectionColor = GetColorForDataType(output.dataType);
        }
        
        /// <summary>
        /// 데이터 타입에 따른 연결선 색상 반환
        /// </summary>
        private Color GetColorForDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Texture:
                    return new Color(0.8f, 0.4f, 1f); // 보라색
                case DataType.Float:
                    return new Color(0.3f, 0.8f, 0.3f); // 녹색
                case DataType.Vector2:
                    return new Color(1f, 0.8f, 0.3f); // 주황색
                case DataType.Vector3:
                    return new Color(1f, 0.5f, 0.3f); // 진한 주황색
                case DataType.Color:
                    return new Color(1f, 1f, 0.3f); // 노란색
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 연결이 유효한지 확인
        /// </summary>
        public bool IsValid()
        {
            return outputSlot != null && 
                   inputSlot != null && 
                   outputSlot.owner != null && 
                   inputSlot.owner != null;
        }
    }
}