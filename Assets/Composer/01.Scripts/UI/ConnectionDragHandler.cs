using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;

namespace VFXComposer.UI
{
    public class ConnectionDragHandler
    {
        private NodeGraphView graphView;
        private VisualElement dragLineLayer;
        
        private bool isDragging = false;
        private NodeSlot startSlot;
        private NodeView startNodeView;
        private Vector2 startPosition;
        private Vector2 currentPosition;
        
        public ConnectionDragHandler(NodeGraphView view)
        {
            graphView = view;
            
            dragLineLayer = new VisualElement();
            dragLineLayer.style.position = Position.Absolute;
            dragLineLayer.style.left = 0;
            dragLineLayer.style.top = 0;
            dragLineLayer.style.right = 0;
            dragLineLayer.style.bottom = 0;
            dragLineLayer.pickingMode = PickingMode.Ignore;
            dragLineLayer.generateVisualContent += DrawDragLine;
            
            graphView.Add(dragLineLayer);
        }
        
        public void StartDrag(NodeSlot slot, NodeView nodeView, Vector2 localPosition)
        {
            isDragging = true;
            startSlot = slot;
            startNodeView = nodeView;

            // Use NodeGraphView's method to get proper screen coordinates with zoom/pan
            bool isOutput = slot.slotType == SlotType.Output;
            startPosition = graphView.GetSlotWorldPosition(nodeView.node, slot, isOutput);
            currentPosition = localPosition;

            dragLineLayer.MarkDirtyRepaint();
        }

        public void UpdateDrag(Vector2 mousePosition)
        {
            if (!isDragging) return;

            currentPosition = mousePosition;
            dragLineLayer.MarkDirtyRepaint();
        }
        
        public void EndDrag(NodeSlot endSlot, NodeView endNodeView)
        {
            if (!isDragging) return;
            
            if (endSlot != null && startSlot != null)
            {
                NodeSlot outputSlot = startSlot.slotType == SlotType.Output ? startSlot : endSlot;
                NodeSlot inputSlot = startSlot.slotType == SlotType.Input ? startSlot : endSlot;
                
                if (outputSlot.slotType == SlotType.Output && inputSlot.slotType == SlotType.Input)
                {
                    graphView.ConnectSlots(outputSlot, inputSlot);
                }
            }
            
            CancelDrag();
        }
        
        public void CancelDrag()
        {
            isDragging = false;
            startSlot = null;
            startNodeView = null;
            dragLineLayer.MarkDirtyRepaint();
        }
        
        private void DrawDragLine(MeshGenerationContext ctx)
        {
            if (!isDragging) return;

            var painter = ctx.painter2D;
            painter.lineWidth = 3f;

            // Use color based on slot data type
            Color lineColor = NodeConnection.GetColorForDataType(startSlot.dataType);
            painter.strokeColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.8f);

            painter.BeginPath();
            painter.MoveTo(startPosition);

            float distance = Mathf.Abs(currentPosition.x - startPosition.x);
            float handleOffset = Mathf.Min(distance * 0.5f, 100f);

            // Tangent direction depends on slot type
            bool isOutputSlot = startSlot.slotType == SlotType.Output;
            Vector2 startTangent = startPosition + new Vector2(isOutputSlot ? handleOffset : -handleOffset, 0);
            Vector2 endTangent = currentPosition + new Vector2(isOutputSlot ? -handleOffset : handleOffset, 0);

            painter.BezierCurveTo(startTangent, endTangent, currentPosition);
            painter.Stroke();
        }
        
        public bool IsDragging => isDragging;
    }
}