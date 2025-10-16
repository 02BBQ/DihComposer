using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;

namespace VFXComposer.UI
{
    public class NodeView : VisualElement
    {
        public Node node;
        
        private Label headerLabel;
        private VisualElement slotsContainer;
        
        private Vector2 dragStartMousePos;
        private Vector2 dragStartNodePos;
        private bool isDragging = false;
        
        public NodeView(Node node)
        {
            this.node = node;
            
            AddToClassList("node");
            
            headerLabel = new Label(node.nodeName);
            headerLabel.AddToClassList("node__header");
            Add(headerLabel);
            
            slotsContainer = new VisualElement();
            slotsContainer.AddToClassList("node__slots");
            Add(slotsContainer);
            
            BuildSlots();
            
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
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
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                isDragging = true;
                dragStartMousePos = evt.mousePosition;
                dragStartNodePos = node.position;
                
                this.CaptureMouse();
                
                BringToFront();
                AddToClassList("node--selected");
                
                evt.StopPropagation();
            }
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isDragging)
            {
                Vector2 delta = evt.mousePosition - dragStartMousePos;
                
                node.position = dragStartNodePos + delta;
                style.left = node.position.x;
                style.top = node.position.y;
                
                evt.StopPropagation();
            }
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 0 && isDragging)
            {
                isDragging = false;
                this.ReleaseMouse();
                RemoveFromClassList("node--selected");
                
                evt.StopPropagation();
            }
        }
        
        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (isDragging)
            {
                isDragging = false;
                this.ReleaseMouse();
                RemoveFromClassList("node--selected");
            }
        }
    }
}