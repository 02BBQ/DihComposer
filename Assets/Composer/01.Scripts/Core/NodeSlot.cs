using UnityEngine;
using System;

namespace VFXComposer.Core
{
    /// <summary>
    /// 노드 입출력 슬롯 클래스
    /// </summary>
    [System.Serializable]
    public class NodeSlot
    {
        public string id;
        public string displayName;
        public SlotType slotType;
        public DataType dataType;
        
        // 소유자 노드
        public Node owner;
        
        public NodeSlot connectedSlot; // Input
        
        public NodeSlot(string id, string displayName, SlotType slotType, DataType dataType)
        {
            this.id = id;
            this.displayName = displayName;
            this.slotType = slotType;
            this.dataType = dataType;
        }
        
        /// <summary>
        /// 다른 슬롯과 연결 가능한지 체크
        /// </summary>
        public bool CanConnectTo(NodeSlot other)
        {
            if (other == null) return false;
            if (other.owner == this.owner) return false;
            if (other.slotType == this.slotType) return false;

            // 타입 호환성 체크 (타입 변환 지원)
            if (!IsTypeCompatible(this.dataType, other.dataType)) return false;

            return true;
        }

        /// <summary>
        /// 두 데이터 타입이 호환 가능한지 체크
        /// </summary>
        public static bool IsTypeCompatible(DataType from, DataType to)
        {
            // 같은 타입은 항상 호환
            if (from == to) return true;

            // Texture ↔ Color 양방향 변환 허용
            if ((from == DataType.Texture && to == DataType.Color) ||
                (from == DataType.Color && to == DataType.Texture))
            {
                return true;
            }

            // 추후 추가 가능한 변환:
            // Float ↔ Vector2/Vector3 (확장)
            // Vector2 ↔ Vector3 (확장/축소)

            return false;
        }
    }
    
    /// <summary>
    /// 슬롯 타입 (입력/출력)
    /// </summary>
    public enum SlotType
    {
        Input,
        Output
    }
    
    /// <summary>
    /// 데이터 타입
    /// </summary>
    public enum DataType
    {
        Texture,    
        Float,      
        Vector2,    
        Vector3,    
        Color       
    }
}