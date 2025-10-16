using UnityEngine;
using System.Collections.Generic;

namespace VFXComposer.Core
{
    /// <summary>
    /// 모든 노드의 베이스 클래스
    /// </summary>
    public abstract class Node
    {
        public string id;
        
        public string nodeName;
        
        public Vector2 position;
        
        public List<NodeSlot> inputSlots = new List<NodeSlot>();
        
        public List<NodeSlot> outputSlots = new List<NodeSlot>();
        
        public bool isExecuted { get; protected set; } = false;
        
        protected Dictionary<string, object> cachedOutputs = new Dictionary<string, object>();
        
        public Node()
        {
            id = System.Guid.NewGuid().ToString();
            nodeName = GetType().Name;

            InitializeSlots();
        }
        
        /// <summary>
        /// 슬롯 초기화 (각 노드가 오버라이드)
        /// </summary>
        protected abstract void InitializeSlots();
        
        /// <summary>
        /// 노드 실행 (각 노드가 구체적으로 구현)
        /// </summary>
        public abstract void Execute();
        
        /// <summary>
        /// 입력 슬롯 추가
        /// </summary>
        protected NodeSlot AddInputSlot(string id, string displayName, DataType dataType)
        {
            var slot = new NodeSlot(id, displayName, SlotType.Input, dataType);
            slot.owner = this;
            inputSlots.Add(slot);
            return slot;
        }
        
        /// <summary>
        /// 출력 슬롯 추가
        /// </summary>
        protected NodeSlot AddOutputSlot(string id, string displayName, DataType dataType)
        {
            var slot = new NodeSlot(id, displayName, SlotType.Output, dataType);
            slot.owner = this;
            outputSlots.Add(slot);
            return slot;
        }
        
        /// <summary>
        /// 입력 슬롯에서 값 가져오기
        /// </summary>
        protected T GetInputValue<T>(string slotId)
        {
            var slot = inputSlots.Find(s => s.id == slotId);
            if (slot == null || slot.connectedSlot == null)
            {
                return default(T);
            }
            
            // 연결된 노드를 먼저 실행
            slot.connectedSlot.owner.Execute();
            
            // 연결된 노드의 출력값 가져오기
            var outputNode = slot.connectedSlot.owner;
            if (outputNode.cachedOutputs.ContainsKey(slot.connectedSlot.id))
            {
                return (T)outputNode.cachedOutputs[slot.connectedSlot.id];
            }
            
            return default(T);
        }
        
        /// <summary>
        /// 출력 슬롯에 값 설정
        /// </summary>
        protected void SetOutputValue(string slotId, object value)
        {
            cachedOutputs[slotId] = value;
        }
        
        /// <summary>
        /// 실행 상태 초기화 (그래프 재실행 전에 호출)
        /// </summary>
        public void ResetExecution()
        {
            isExecuted = false;
            cachedOutputs.Clear();
        }
    }
}