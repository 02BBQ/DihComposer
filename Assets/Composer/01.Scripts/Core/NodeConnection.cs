using UnityEngine;

namespace VFXComposer.Core
{
    /// <summary>
    /// 두 노드 슬롯 사이의 연결을 나타내는 클래스
    /// </summary>
    [System.Serializable]
    public class NodeConnection : ISelectable, IDeletable
    {
        public string id;

        // 출력 슬롯 (연결의 시작점)
        public NodeSlot outputSlot;

        // 입력 슬롯 (연결의 끝점)
        public NodeSlot inputSlot;

        // 연결 색상 (데이터 타입에 따라 다른 색상)
        public Color connectionColor;

        // 선택 상태
        private bool isSelected = false;
        public bool IsSelected => isSelected;

        // NodeGraph 참조 (삭제를 위해 필요)
        private NodeGraph graph;

        public NodeConnection(NodeSlot output, NodeSlot input, NodeGraph graph = null)
        {
            id = System.Guid.NewGuid().ToString();
            outputSlot = output;
            inputSlot = input;
            this.graph = graph;

            // 데이터 타입에 따른 색상 설정
            connectionColor = GetColorForDataType(output.dataType);
        }

        public void Select()
        {
            isSelected = true;
            OnSelectionChanged(true);
        }

        public void Deselect()
        {
            isSelected = false;
            OnSelectionChanged(false);
        }

        public void OnSelectionChanged(bool selected)
        {
            // Selection changed - will trigger redraw in NodeGraphView
        }

        public bool CanDelete()
        {
            return true; // 모든 Connection은 삭제 가능
        }

        public void Delete()
        {
            if (graph != null)
            {
                graph.DisconnectConnection(this);
            }
        }

        public string GetDeleteDescription()
        {
            return $"Delete connection {outputSlot?.owner.nodeName ?? "?"} → {inputSlot?.owner.nodeName ?? "?"}";
        }
        
        /// <summary>
        /// 데이터 타입에 따른 연결선 색상 반환
        /// </summary>
        public static Color GetColorForDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Texture:
                    return Color.white; // 하얀색
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