using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using VFXComposer.Core;

namespace VFXComposer.UI
{
    public class NodeGraphView : VisualElement
    {
        private GridBackground gridBackground;
        private VisualElement nodeContainer;
        private VisualElement connectionLayer;
        
        private NodeGraph graph;
        private Dictionary<Node, NodeView> nodeViews = new Dictionary<Node, NodeView>();
        
        private Vector2 panOffset = Vector2.zero;
        private float zoomScale = 1f;
        
        private Vector2 lastMousePosition;
        private bool isPanning = false;
        private bool needsConnectionRedraw = false;
        
        public NodeGraphView()
        {
            AddToClassList("node-graph-view");
            
            gridBackground = new GridBackground();
            Add(gridBackground);
            
            connectionLayer = new VisualElement();
            connectionLayer.AddToClassList("connection-layer");
            connectionLayer.generateVisualContent += DrawConnections;
            Add(connectionLayer);
            
            nodeContainer = new VisualElement();
            nodeContainer.style.position = Position.Absolute;
            nodeContainer.pickingMode = PickingMode.Ignore;
            Add(nodeContainer);
            
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            
            schedule.Execute(UpdateConnections).Every(16);
        }
        
        public void SetGraph(NodeGraph newGraph)
        {
            graph = newGraph;
            RefreshView();
        }
        
        public void RefreshView()
        {
            nodeContainer.Clear();
            nodeViews.Clear();
            
            if (graph == null) return;
            
            foreach (var node in graph.nodes)
            {
                AddNodeView(node);
            }
            
            connectionLayer.MarkDirtyRepaint();
        }
        
        private void AddNodeView(Node node)
        {
            var nodeView = new NodeView(node);
            nodeViews[node] = nodeView;
            nodeContainer.Add(nodeView);
            
            nodeView.style.left = node.position.x;
            nodeView.style.top = node.position.y;
            
            nodeView.RegisterCallback<MouseMoveEvent>(evt => {
                if (evt.button == 0) needsConnectionRedraw = true;
            });
        }
        
        public void RequestConnectionRedraw()
        {
            needsConnectionRedraw = true;
        }
        
        private void UpdateConnections()
        {
            if (needsConnectionRedraw)
            {
                connectionLayer.MarkDirtyRepaint();
                needsConnectionRedraw = false;
            }
        }
        
        private void OnWheel(WheelEvent evt)
        {
            float delta = evt.delta.y;
            float zoomDelta = delta > 0 ? 0.9f : 1.1f;
            
            zoomScale = Mathf.Clamp(zoomScale * zoomDelta, 0.1f, 3f);
            
            nodeContainer.transform.scale = new Vector3(zoomScale, zoomScale, 1);
            gridBackground.SetZoom(zoomScale);
            
            needsConnectionRedraw = true;
            
            evt.StopPropagation();
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 2 || (evt.button == 0 && evt.altKey))
            {
                isPanning = true;
                lastMousePosition = evt.mousePosition;
                evt.StopPropagation();
            }
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isPanning)
            {
                Vector2 delta = evt.mousePosition - lastMousePosition;
                panOffset += delta;
                
                nodeContainer.transform.position = new Vector3(panOffset.x, panOffset.y, 0);
                gridBackground.SetOffset(panOffset);
                
                lastMousePosition = evt.mousePosition;
                needsConnectionRedraw = true;
                
                evt.StopPropagation();
            }
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 2 || evt.button == 0)
            {
                isPanning = false;
            }
        }
        
        private void DrawConnections(MeshGenerationContext ctx)
        {
            if (graph == null) return;
            
            var painter = ctx.painter2D;
            painter.lineWidth = 3f;
            
            foreach (var connection in graph.connections)
            {
                if (!connection.IsValid()) continue;
                
                var outputNode = connection.outputSlot.owner;
                var inputNode = connection.inputSlot.owner;
                
                if (!nodeViews.ContainsKey(outputNode) || !nodeViews.ContainsKey(inputNode))
                    continue;
                
                var outputPos = GetSlotWorldPosition(outputNode, connection.outputSlot, true);
                var inputPos = GetSlotWorldPosition(inputNode, connection.inputSlot, false);
                
                painter.strokeColor = connection.connectionColor;
                
                painter.BeginPath();
                painter.MoveTo(outputPos);
                
                float distance = Mathf.Abs(inputPos.x - outputPos.x);
                float handleOffset = Mathf.Min(distance * 0.5f, 100f);
                
                Vector2 outputTangent = outputPos + new Vector2(handleOffset, 0);
                Vector2 inputTangent = inputPos - new Vector2(handleOffset, 0);
                
                painter.BezierCurveTo(outputTangent, inputTangent, inputPos);
                painter.Stroke();
            }
        }
        
        private Vector2 GetSlotWorldPosition(Node node, NodeSlot slot, bool isOutput)
        {
            var nodeView = nodeViews[node];
            var nodePos = new Vector2(node.position.x, node.position.y);
            
            float slotY = 40 + (isOutput ? node.outputSlots.IndexOf(slot) : node.inputSlots.IndexOf(slot)) * 24;
            float slotX = isOutput ? 150 : 0;
            
            return (nodePos + new Vector2(slotX, slotY)) * zoomScale + panOffset;
        }
    }
}