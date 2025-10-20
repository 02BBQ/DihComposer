using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;

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

        public bool IsSelected => isSelected;
        
        public NodeView(Node node)
        {
            this.node = node;
            
            AddToClassList("node");
            
            headerLabel = new Label(node.nodeName);
            headerLabel.AddToClassList("node__header");
            Add(headerLabel);
            
            var previewImage = new Image();
            previewImage.AddToClassList("node__preview");
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            Add(previewImage);
            
            slotsContainer = new VisualElement();
            slotsContainer.AddToClassList("node__slots");
            Add(slotsContainer);
            
            BuildSlots();
            
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            
            schedule.Execute(() => UpdatePreview(previewImage)).Every(100);
        }
        
        private void UpdatePreview(Image previewImage)
        {
            if (node.cachedOutputs.Count > 0)
            {
                foreach (var output in node.cachedOutputs.Values)
                {
                    if (output is RenderTexture rt)
                    {
                        previewImage.image = rt;
                        return;
                    }
                    else if (output is Color col)
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

            port.RegisterCallback<MouseDownEvent>(evt => OnSlotMouseDown(evt, slot));
            port.RegisterCallback<MouseUpEvent>(evt => OnSlotMouseUp(evt, slot));

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
        
        private void OnSlotMouseDown(MouseDownEvent evt, NodeSlot slot)
        {
            if (evt.button != 0) return;

            isSlotDragging = true;

            var graphView = GetFirstAncestorOfType<NodeGraphView>();
            if (graphView != null)
            {
                // Use mousePosition (screen coordinates) instead of localMousePosition
                graphView.StartSlotDrag(slot, this, evt.mousePosition);
            }

            evt.StopPropagation();
        }
        
        private void OnSlotMouseUp(MouseUpEvent evt, NodeSlot slot)
        {
            if (evt.button != 0) return;
            
            isSlotDragging = false;
            
            var graphView = GetFirstAncestorOfType<NodeGraphView>();
            if (graphView != null)
            {
                graphView.EndSlotDrag(slot, this);
            }
            
            evt.StopPropagation();
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0 && !isSlotDragging)
            {
                isDragging = true;
                dragStartMousePos = evt.mousePosition;
                dragStartNodePos = node.position;
                
                this.CaptureMouse();
                
                BringToFront();
                AddToClassList("node--selected");
                
                var graphView = GetFirstAncestorOfType<NodeGraphView>();
                if (graphView != null)
                {
                    graphView.SelectNode(this);
                }
                
                evt.StopPropagation();
            }
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isDragging && !isSlotDragging)
            {
                Vector2 delta = evt.mousePosition - dragStartMousePos;

                // Get zoom scale from graph view
                var graphView = GetFirstAncestorOfType<NodeGraphView>();
                if (graphView != null)
                {
                    delta /= graphView.ZoomScale;
                }

                node.position = dragStartNodePos + delta;
                style.left = node.position.x;
                style.top = node.position.y;

                evt.StopPropagation();
            }
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 0)
            {
                if (isDragging)
                {
                    isDragging = false;
                    this.ReleaseMouse();
                }
                
                isSlotDragging = false;
                
                evt.StopPropagation();
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
        
        public Vector2 GetSlotPosition(NodeSlot slot)
        {
            bool isOutput = slot.slotType == SlotType.Output;
            int slotIndex = isOutput ?
                node.outputSlots.IndexOf(slot) :
                node.inputSlots.IndexOf(slot);

            // Match CSS values from GridBackground.uss
            float nodePadding = 8;        // .node padding
            float headerHeight = 36;      // header padding + font + margin-bottom (약간 여유)
            float previewHeight = 128;    // preview margin-top + height + margin-bottom
            float slotMargin = 4;         // .slot-input/.slot-output margin
            float portSize = 12;          // .slot__port height

            // Calculate slot center position
            float slotHeight = portSize + (slotMargin * 2);
            float yPos = nodePadding + headerHeight + previewHeight + (slotIndex * slotHeight) + portSize / 2;

            // X position - port는 노드 경계에 위치
            float nodeWidth = 150;
            float xPos = isOutput ? nodeWidth : 0;

            return new Vector2(node.position.x + xPos, node.position.y + yPos);
        }
        
        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (isDragging)
            {
                isDragging = false;
                this.ReleaseMouse();
            }
            
            isSlotDragging = false;
        }
    }
}