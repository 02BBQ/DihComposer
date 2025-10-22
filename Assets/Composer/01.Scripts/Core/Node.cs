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
        
        public Dictionary<string, object> cachedOutputs { get; protected set; } = new();
        
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
        /// 입력 슬롯에서 값 가져오기 (타입 변환 지원)
        /// </summary>
        protected T GetInputValue<T>(string slotId)
        {
            var slot = inputSlots.Find(s => s.id == slotId);
            if (slot == null || slot.connectedSlot == null)
            {
                return default;
            }

            slot.connectedSlot.owner.Execute();

            var outputNode = slot.connectedSlot.owner;
            if (!outputNode.cachedOutputs.ContainsKey(slot.connectedSlot.id))
            {
                return default;
            }

            object value = outputNode.cachedOutputs[slot.connectedSlot.id];

            // 타입이 일치하면 그대로 반환
            if (value is T directValue)
            {
                return directValue;
            }

            // 타입 변환 시도
            return ConvertValue<T>(value, slot.dataType, slot.connectedSlot.dataType);
        }

        /// <summary>
        /// 타입 간 자동 변환 처리
        /// </summary>
        private T ConvertValue<T>(object value, DataType targetType, DataType sourceType)
        {
            // Texture → Color 변환
            if (typeof(T) == typeof(Color) && value is RenderTexture rt)
            {
                return (T)(object)SampleTextureCenter(rt);
            }

            // Color → Texture 변환
            if (typeof(T) == typeof(RenderTexture) && value is Color color)
            {
                return (T)(object)CreateSolidColorTexture(color);
            }

            // 변환 실패 시 기본값 반환
            Debug.LogWarning($"Type conversion failed: {sourceType} → {targetType}");
            return default;
        }

        /// <summary>
        /// RenderTexture 중앙 픽셀 샘플링 (Texture → Color 변환)
        /// </summary>
        private Color SampleTextureCenter(RenderTexture rt)
        {
            if (rt == null) return Color.black;

            // GPU → CPU 읽기 (성능 주의)
            RenderTexture.active = rt;
            Texture2D temp = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            temp.ReadPixels(new Rect(rt.width / 2, rt.height / 2, 1, 1), 0, 0);
            temp.Apply();
            RenderTexture.active = null;

            Color result = temp.GetPixel(0, 0);
            Object.Destroy(temp);

            return result;
        }

        /// <summary>
        /// 단색 RenderTexture 생성 (Color → Texture 변환)
        /// </summary>
        private RenderTexture CreateSolidColorTexture(Color color, int size = 256)
        {
            RenderTexture rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            rt.Create();

            // Material로 단색 채우기
            Material fillMat = new Material(Shader.Find("Hidden/Internal-Colored"));
            RenderTexture previousRT = RenderTexture.active;
            RenderTexture.active = rt;

            GL.Clear(true, true, color);

            RenderTexture.active = previousRT;

            return rt;
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