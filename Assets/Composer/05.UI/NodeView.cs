using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;
using System.Collections.Generic;

namespace VFXComposer.UI
{
    public class NodeView : VisualElement, ISelectable, IDeletable
    {
        public Node node;

        private Label headerLabel;
        private VisualElement slotsContainer;

        private Vector2 dragStartMousePos;
        private Vector2 dragStartNodePos;
        private bool isDragging = false;
        private bool isSlotDragging = false;
        private bool isSelected = false;

        // 슬롯 element 캐싱 (빠른 조회용)
        private Dictionary<NodeSlot, VisualElement> slotElements = new Dictionary<NodeSlot, VisualElement>();

        public bool IsSelected => isSelected;
        
        public NodeView(Node node)
        {
            this.node = node;

            AddToClassList("node");

            headerLabel = new Label(node.nodeName);
            headerLabel.AddToClassList("node__header");
            headerLabel.pickingMode = PickingMode.Ignore; // 드래그가 제대로 작동하도록
            Add(headerLabel);

            var previewImage = new Image();
            previewImage.AddToClassList("node__preview");
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            previewImage.pickingMode = PickingMode.Ignore; // 드래그가 제대로 작동하도록
            Add(previewImage);

            slotsContainer = new VisualElement();
            slotsContainer.AddToClassList("node__slots");
            slotsContainer.pickingMode = PickingMode.Ignore; // 드래그가 제대로 작동하도록
            Add(slotsContainer);

            BuildSlots();

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            schedule.Execute(() => UpdatePreview(previewImage)).Every(100);
        }
        
        private void UpdatePreview(Image previewImage)
        {
            if (node.cachedOutputs.Count > 0)
            {
                // 우선순위: Texture > Color
                // 먼저 Texture를 찾음
                foreach (var output in node.cachedOutputs.Values)
                {
                    if (output is RenderTexture rt)
                    {
                        previewImage.image = rt;
                        return;
                    }
                }

                // Texture가 없으면 Color 찾음
                foreach (var output in node.cachedOutputs.Values)
                {
                    if (output is Color col)
                    {
                        var tex = new Texture2D(1, 1);
                        tex.SetPixel(0, 0, col);
                        tex.Apply();
                        previewImage.image = tex;
                        return;
                    }
                }
            }
        }
        
        private void BuildSlots()
        {
            var inputContainer = new VisualElement();
            var outputContainer = new VisualElement();
            
            foreach (var slot in node.inputSlots)
            {
                var slotElement = CreateSlotElement(slot, false);
                inputContainer.Add(slotElement);
            }
            
            foreach (var slot in node.outputSlots)
            {
                var slotElement = CreateSlotElement(slot, true);
                outputContainer.Add(slotElement);
            }
            
            slotsContainer.Add(inputContainer);
            slotsContainer.Add(outputContainer);
        }
        
        private VisualElement CreateSlotElement(NodeSlot slot, bool isOutput)
        {
            var slotContainer = new VisualElement();
            slotContainer.AddToClassList(isOutput ? "slot-output" : "slot-input");

            var port = new VisualElement();
            port.AddToClassList("slot__port");
            port.userData = slot;

            // Set port color based on data type
            Color portColor = NodeConnection.GetColorForDataType(slot.dataType);
            port.style.backgroundColor = portColor;

            port.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, slot));
            port.RegisterCallback<PointerUpEvent>(evt => OnSlotPointerUp(evt, slot));

            // 슬롯 element 캐싱
            slotElements[slot] = port;

            var label = new Label(slot.displayName);
            label.AddToClassList("slot__label");

            if (isOutput)
            {
                slotContainer.Add(label);
                slotContainer.Add(port);
            }
            else
            {
                slotContainer.Add(port);
                slotContainer.Add(label);
            }

            return slotContainer;
        }
        
        private void OnSlotPointerDown(PointerDownEvent evt, NodeSlot slot)
        {
            // 터치는 button이 -1일 수 있으므로 체크
            if (evt.button != 0 && evt.button != -1) return;
            if (!evt.isPrimary) return;

            isSlotDragging = true;

            var graphView = GetFirstAncestorOfType<NodeGraphView>();
            if (graphView != null)
            {
                // Use position (works for both mouse and touch)
                graphView.StartSlotDrag(slot, this, evt.position);
            }

            evt.StopPropagation();
        }

        private void OnSlotPointerUp(PointerUpEvent evt, NodeSlot slot)
        {
            if (!evt.isPrimary) return;

            isSlotDragging = false;

            var graphView = GetFirstAncestorOfType<NodeGraphView>();
            if (graphView != null)
            {
                graphView.EndSlotDrag(slot, this);
            }

            evt.StopPropagation();
        }
        
        private void OnPointerDown(PointerDownEvent evt)
        {
            Debug.Log($"[NodeView {node.nodeName}] OnPointerDown - button: {evt.button}, isPrimary: {evt.isPrimary}, pointerType: {evt.pointerType}, target: {evt.target?.GetType().Name}, isSlotDragging: {isSlotDragging}");

            // 터치는 button이 -1일 수 있으므로 체크
            if ((evt.button == 0 || evt.button == -1) && evt.isPrimary && !isSlotDragging)
            {
                Debug.Log($"[NodeView {node.nodeName}] ✓ Starting drag");
                isDragging = true;

                BringToFront();
                AddToClassList("node--selected");

                var graphView = GetFirstAncestorOfType<NodeGraphView>();
                if (graphView != null)
                {
                    graphView.SelectNode(this);
                    // Delegate drag handling to NodeGraphView for mobile compatibility
                    graphView.StartNodeDrag(this, evt.position);
                }

                evt.StopPropagation();
            }
            else
            {
                Debug.Log($"[NodeView {node.nodeName}] ✗ Drag rejected");
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            // Node dragging is now handled at NodeGraphView level for mobile compatibility
            // This allows proper touch event handling without CapturePointer issues
            Debug.Log($"[NodeView {node.nodeName}] OnPointerMove - isDragging: {isDragging}");
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            Debug.Log($"[NodeView {node.nodeName}] OnPointerUp - button: {evt.button}, isDragging: {isDragging}");

            // 터치는 button이 -1일 수 있으므로 체크
            if ((evt.button == 0 || evt.button == -1) && evt.isPrimary)
            {
                if (isDragging)
                {
                    isDragging = false;
                    Debug.Log($"[NodeView {node.nodeName}] Drag ended");
                }

                isSlotDragging = false;

                // StopPropagation 제거! NodeGraphView가 draggingNodeView를 null로 만들어야 함
                // evt.StopPropagation();
            }
        }
        
        public void Select()
        {
            isSelected = true;
            AddToClassList("node--selected");
            OnSelectionChanged(true);
        }

        public void Deselect()
        {
            isSelected = false;
            RemoveFromClassList("node--selected");
            OnSelectionChanged(false);
        }

        public void OnSelectionChanged(bool selected)
        {
            // Hook for future use
        }

        public bool CanDelete()
        {
            return !(node is OutputNode);
        }

        public void Delete()
        {
            var graphView = GetFirstAncestorOfType<NodeGraphView>();
            graphView?.DeleteNode(node);
        }

        public string GetDeleteDescription()
        {
            return $"Delete {node.nodeName}";
        }
        
        /// <summary>
        /// 슬롯의 port element를 반환합니다
        /// </summary>
        public VisualElement GetSlotElement(NodeSlot slot)
        {
            if (slotElements.TryGetValue(slot, out var element))
            {
                return element;
            }
            return null;
        }

        /// <summary>
        /// 슬롯의 중심 위치를 반환합니다 (NodeGraphView 기준 좌표)
        /// </summary>
        public Vector2 GetSlotPosition(NodeSlot slot)
        {
            var portElement = GetSlotElement(slot);
            if (portElement == null)
            {
                // Fallback: 수동 계산
                bool isOutput = slot.slotType == SlotType.Output;
                int slotIndex = isOutput ?
                    node.outputSlots.IndexOf(slot) :
                    node.inputSlots.IndexOf(slot);

                float nodePadding = 8;
                float headerHeight = 36;
                float previewHeight = 128;
                float slotMargin = 4;
                float portSize = 12;

                float slotHeight = portSize + (slotMargin * 2);
                float yPos = nodePadding + headerHeight + previewHeight + (slotIndex * slotHeight) + portSize / 2;

                float nodeWidth = 150;
                float xPos = isOutput ? nodeWidth : 0;

                return new Vector2(node.position.x + xPos, node.position.y + yPos);
            }

            // 실제 렌더링된 위치 사용 (더 정확함)
            // worldBound는 NodeGraphView 기준의 절대 좌표
            Rect portBound = portElement.worldBound;
            Vector2 portCenter = portBound.center;

            return portCenter;
        }
        
        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            // Dragging continues even when pointer leaves the node
            // NodeGraphView handles the drag tracking now
            isSlotDragging = false;
        }
    }
}