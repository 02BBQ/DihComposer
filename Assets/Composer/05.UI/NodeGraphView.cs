using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using VFXComposer.Core;
using System;

namespace VFXComposer.UI
{
    public class NodeGraphView : VisualElement
    {
        private GridBackground gridBackground;
        private VisualElement nodeContainer;
        private VisualElement connectionLayer;

        private NodeGraph graph;
        private Dictionary<Node, NodeView> nodeViews = new Dictionary<Node, NodeView>();

        private ConnectionDragHandler connectionDragHandler;
        private NodeCreationMenu creationMenu;

        private NodeView selectedNodeView;

        private Vector2 panOffset = Vector2.zero;
        private float zoomScale = 1f;

        public Vector2 PanOffset => panOffset;
        public float ZoomScale => zoomScale;

        private NodeInspector inspector;
        private CommandHistory commandHistory = new CommandHistory();

        private Vector2 lastMousePosition;
        private bool isPanning = false;
        private bool needsConnectionRedraw = false;
        
        private bool isLongPressing = false;
        private Vector2 longPressStartPos;
        private float longPressTimer = 0f;
        private const float longPressThreshold = 0.3f;
        
        public NodeGraphView()
        {
            AddToClassList("node-graph-view");
            
            gridBackground = new GridBackground();
            Add(gridBackground);
            
            connectionLayer = new VisualElement();
            connectionLayer.AddToClassList("connection-layer");
            connectionLayer.generateVisualContent += DrawConnections;
            connectionLayer.pickingMode = PickingMode.Ignore;
            Add(connectionLayer);
            
            nodeContainer = new VisualElement();
            nodeContainer.style.position = Position.Absolute;
            nodeContainer.pickingMode = PickingMode.Ignore;
            Add(nodeContainer);
            
            connectionDragHandler = new ConnectionDragHandler(this);
            
            creationMenu = new NodeCreationMenu(this);
            creationMenu.Hide();
            Add(creationMenu);
            
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            
            focusable = true;

            schedule.Execute(UpdateConnections).Every(16);
            schedule.Execute(CheckLongPress).Every(50);
            schedule.Execute(UpdateGraph).Every(16); // Update graph every frame for Time node
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

            // Execute all nodes to generate previews
            var executor = new NodeExecutor(graph);
            executor.Execute();

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
        
        public void AddNode(Node node)
        {
            if (graph == null) return;

            // Use command for undo/redo with UI callbacks
            var command = new AddNodeCommand(
                graph,
                node,
                onAdd: (n) => {
                    AddNodeView(n);
                    var executor = new NodeExecutor(graph);
                    executor.ExecuteNode(n);
                    needsConnectionRedraw = true;
                },
                onRemove: (n) => {
                    if (nodeViews.ContainsKey(n))
                    {
                        var nodeView = nodeViews[n];
                        nodeViews.Remove(n);
                        nodeContainer.Remove(nodeView);
                    }
                    needsConnectionRedraw = true;
                }
            );
            commandHistory.ExecuteCommand(command);
        }

        public void ConnectSlots(NodeSlot output, NodeSlot input)
        {
            if (graph == null) return;

            // Use command for undo/redo
            var command = new ConnectSlotsCommand(graph, output, input);
            commandHistory.ExecuteCommand(command);

            var executor = new NodeExecutor(graph);
            executor.Execute();

            needsConnectionRedraw = true;
        }
        
        public void StartSlotDrag(NodeSlot slot, NodeView nodeView, Vector2 position)
        {
            connectionDragHandler.StartDrag(slot, nodeView, position);
        }
        
        public void EndSlotDrag(NodeSlot slot, NodeView nodeView)
        {
            connectionDragHandler.EndDrag(slot, nodeView);
            needsConnectionRedraw = true;
        }
        
        public void SelectNode(NodeView nodeView)
        {
            if (selectedNodeView != null && selectedNodeView != nodeView)
            {
                selectedNodeView.Deselect();
            }
            selectedNodeView = nodeView;
            Focus();

            // Update inspector
            if (inspector != null)
            {
                inspector.ShowNodeProperties(nodeView.node);
            }
        }

        public void SetInspector(NodeInspector nodeInspector)
        {
            inspector = nodeInspector;
        }

        public NodeGraph GetGraph()
        {
            return graph;
        }

        public void DeselectAll()
        {
            if (selectedNodeView != null)
            {
                selectedNodeView.Deselect();
                selectedNodeView = null;
            }

            // Clear inspector
            if (inspector != null)
            {
                inspector.ShowNodeProperties(null);
            }
        }
        
        public void DeleteNode(Node node)
        {
            if (node == null || graph == null) return;
            if (!nodeViews.ContainsKey(node)) return;

            // OutputNode는 삭제 불가능
            if (node is OutputNode)
            {
                Debug.LogWarning("Cannot delete OutputNode!");
                return;
            }

            // Use command for undo/redo with UI callbacks
            var command = new DeleteNodeCommand(
                graph,
                node,
                onRemove: (n) => {
                    if (nodeViews.ContainsKey(n))
                    {
                        var nodeView = nodeViews[n];
                        nodeViews.Remove(n);
                        nodeContainer.Remove(nodeView);
                    }

                    if (selectedNodeView != null && selectedNodeView.node == n)
                    {
                        selectedNodeView = null;
                    }

                    if (inspector != null)
                    {
                        inspector.ShowNodeProperties(null);
                    }

                    needsConnectionRedraw = true;
                },
                onAdd: (n) => {
                    AddNodeView(n);
                    needsConnectionRedraw = true;
                }
            );
            commandHistory.ExecuteCommand(command);
        }

        private void DeleteSelectedNode()
        {
            if (selectedNodeView == null) return;

            DeleteNode(selectedNodeView.node);
        }
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            // Delete key
            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelectedNode();
                evt.StopPropagation();
            }
            // Undo: Ctrl+Z (or Cmd+Z on Mac)
            else if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.Z && !evt.shiftKey)
            {
                Undo();
                evt.StopPropagation();
            }
            // Redo: Ctrl+Y or Ctrl+Shift+Z (or Cmd+Shift+Z on Mac)
            else if ((evt.ctrlKey || evt.commandKey) && (evt.keyCode == KeyCode.Y || (evt.shiftKey && evt.keyCode == KeyCode.Z)))
            {
                Redo();
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Undo 실행
        /// </summary>
        public void Undo()
        {
            if (!commandHistory.CanUndo) return;

            commandHistory.Undo();
            needsConnectionRedraw = true;
        }

        /// <summary>
        /// Redo 실행
        /// </summary>
        public void Redo()
        {
            if (!commandHistory.CanRedo) return;

            commandHistory.Redo();
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

        private void UpdateGraph()
        {
            if (graph != null)
            {
                var executor = new NodeExecutor(graph);
                executor.Execute();
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
            Focus();
            
            if (evt.button == 1)
            {
                creationMenu.Show(evt.mousePosition);
                evt.StopPropagation();
                return;
            }
            
            if (evt.button == 2 || (evt.button == 0 && evt.altKey))
            {
                isPanning = true;
                lastMousePosition = evt.mousePosition;
                evt.StopPropagation();
            }
            else if (evt.button == 0)
            {
                creationMenu.Hide();
                DeselectAll();
                
                isLongPressing = true;
                longPressStartPos = evt.mousePosition;
                longPressTimer = 0f;
            }
        }
        
        private void CheckLongPress()
        {
            if (isLongPressing)
            {
                longPressTimer += 0.05f;
                
                if (longPressTimer >= longPressThreshold)
                {
                    creationMenu.Show(longPressStartPos);
                    isLongPressing = false;
                }
            }
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (connectionDragHandler.IsDragging)
            {
                connectionDragHandler.UpdateDrag(evt.mousePosition);
                evt.StopPropagation();
                return;
            }
            
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
            
            if (isLongPressing)
            {
                float dist = Vector2.Distance(evt.mousePosition, longPressStartPos);
                if (dist > 10f)
                {
                    isLongPressing = false;
                }
            }
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 2 || evt.button == 0)
            {
                isPanning = false;
            }
            
            if (evt.button == 0)
            {
                isLongPressing = false;
            }
            
            if (connectionDragHandler.IsDragging)
            {
                connectionDragHandler.CancelDrag();
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
        
        public Vector2 GetSlotWorldPosition(Node node, NodeSlot slot, bool isOutput)
        {
            if (!nodeViews.ContainsKey(node)) return Vector2.zero;

            var nodeView = nodeViews[node];
            Vector2 slotPos = nodeView.GetSlotPosition(slot);

            return slotPos * zoomScale + panOffset;
        }
    }
}