using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using VFXComposer.Core;
using System;
using UnityEngine.InputSystem.Interactions;

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
        public NodeCreationMenu CreationMenu => creationMenu;

        private NodeView selectedNodeView;
        private NodeConnection selectedConnection;

        private Vector2 panOffset = Vector2.zero;
        private float zoomScale = 1f;

        public Vector2 PanOffset => panOffset;
        public float ZoomScale => zoomScale;

        private NodeInspector inspector;
        private CommandHistory commandHistory = new CommandHistory();

        // Undo/Redo state change event
        public event Action OnCommandHistoryChanged;

        private Vector2 lastMousePosition;
        private bool isPanning = false;
        private bool needsConnectionRedraw = false;

        private bool isLongPressing = false;
        private Vector2 longPressStartPos;
        private float longPressTimer = 0f;
        private const float longPressThreshold = 0.3f;

        // Background drag variables
        private bool isBackgroundDragging = false;
        private Vector2 dragStartPosition;
        private const float dragThreshold = 5f; // 픽셀 단위 임계값

        // Touch gesture variables
        private Dictionary<int, Vector2> activeTouches = new Dictionary<int, Vector2>();
        private float lastPinchDistance = 0f;
        private bool isPinching = false;
        
        public NodeGraphView()
        {
            AddToClassList("node-graph-view");

            gridBackground = new GridBackground();
            Add(gridBackground);

            nodeContainer = new VisualElement();
            nodeContainer.style.position = Position.Absolute;
            nodeContainer.pickingMode = PickingMode.Ignore;
            Add(nodeContainer);

            connectionLayer = new VisualElement();
            connectionLayer.AddToClassList("connection-layer");
            connectionLayer.generateVisualContent += DrawConnections;
            connectionLayer.pickingMode = PickingMode.Ignore;
            Add(connectionLayer);

            connectionDragHandler = new ConnectionDragHandler(this);

            creationMenu = new NodeCreationMenu(this);
            creationMenu.Hide();
            
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(OnPointerCancel);

            focusable = true;

            schedule.Execute(UpdateConnections).Every(16);
            schedule.Execute(CheckLongPress).Every(50);
            schedule.Execute(UpdateGraph).Every(16); 
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
            
            nodeView.RegisterCallback<PointerMoveEvent>(evt => {
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
            OnCommandHistoryChanged?.Invoke();
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
            OnCommandHistoryChanged?.Invoke();
        }
        
        public void StartSlotDrag(NodeSlot slot, NodeView nodeView, Vector2 position)
        {
            connectionDragHandler.StartDrag(slot, nodeView, position);
        }
        
        public void EndSlotDrag(NodeSlot slot, NodeView nodeView)
        {
            connectionDragHandler.EndDrag(slot);
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

            if (selectedConnection != null)
            {
                selectedConnection = null;
                needsConnectionRedraw = true;
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
            OnCommandHistoryChanged?.Invoke();
        }

        public void DeleteConnection(NodeConnection connection)
        {
            if (connection == null || graph == null) return;

            // Use command for undo/redo
            var command = new DisconnectSlotsCommand(graph, connection.outputSlot, connection.inputSlot);
            commandHistory.ExecuteCommand(command);

            var executor = new NodeExecutor(graph);
            executor.Execute();

            needsConnectionRedraw = true;
            OnCommandHistoryChanged?.Invoke();

            if (selectedConnection == connection)
            {
                selectedConnection = null;
            }
        }

        private void DeleteSelected()
        {
            // 노드가 선택되어 있으면 노드 삭제
            if (selectedNodeView != null)
            {
                DeleteNode(selectedNodeView.node);
                return;
            }

            // 간선이 선택되어 있으면 간선 삭제
            if (selectedConnection != null)
            {
                DeleteConnection(selectedConnection);
                return;
            }
        }
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            // Delete key
            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelected();
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
            OnCommandHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Redo 실행
        /// </summary>
        public void Redo()
        {
            if (!commandHistory.CanRedo) return;

            commandHistory.Redo();
            needsConnectionRedraw = true;
            OnCommandHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Undo 가능 여부
        /// </summary>
        public bool CanUndo => commandHistory.CanUndo;

        /// <summary>
        /// Redo 가능 여부
        /// </summary>
        public bool CanRedo => commandHistory.CanRedo;

        /// <summary>
        /// 노드 생성 메뉴 열기 (공개 API)
        /// </summary>
        public void ShowNodeCreationMenu(Vector2? position = null)
        {
            Vector2 menuPos = position ?? new Vector2(
                contentRect.width / 2,
                contentRect.height / 2
            );
            creationMenu.Show(menuPos);
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
        
        

        // === Touch Gesture Handlers ===

        private void OnPointerDown(PointerDownEvent evt)
        {
            Focus();

            // Track touches for mobile gestures
            if (evt.pointerType != "mouse")
            {
                activeTouches[evt.pointerId] = evt.position;

                // Check for pinch gesture (2 fingers)
                if (activeTouches.Count == 2)
                {
                    isPinching = true;
                    lastPinchDistance = GetPinchDistance();
                    return;
                }
            }

            // 우클릭 (마우스만): 노드 생성 메뉴
            if (evt.button == 1 && evt.pointerType == "mouse")
            {
                creationMenu.Show(evt.position);
                evt.StopPropagation();
                return;
            }

            // 마우스 휠 드래그 또는 Alt+좌클릭: 팬 모드
            if (evt.button == 2 || (evt.button == 0 && evt.altKey))
            {
                isPanning = true;
                lastMousePosition = evt.position;
                evt.StopPropagation();
            }
            // 일반 좌클릭 또는 싱글 터치: 간선 선택 시도 또는 배경 드래그 준비
            else if (evt.button == 0 || evt.isPrimary)
            {
                creationMenu.Hide();

                // 먼저 간선 클릭 여부 확인
                Vector2 localMousePos = this.WorldToLocal(evt.position);
                NodeConnection clickedConnection = FindConnectionAtPoint(localMousePos);
                if (clickedConnection != null)
                {
                    // 간선 선택
                    DeselectAll();
                    selectedConnection = clickedConnection;
                    needsConnectionRedraw = true;
                    evt.StopPropagation();
                    return;
                }

                // 간선이 없으면 배경 클릭 처리
                DeselectAll();

                // 배경 드래그 준비
                dragStartPosition = evt.position;
                lastMousePosition = evt.position;

                // 롱프레스 시작 (터치만)
                if (evt.pointerType != "mouse")
                {
                    isLongPressing = true;
                    longPressStartPos = evt.position;
                    longPressTimer = 0f;
                }
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            // Update touch tracking
            if (evt.pointerType != "mouse" && activeTouches.ContainsKey(evt.pointerId))
            {
                activeTouches[evt.pointerId] = evt.position;

                if (isPinching && activeTouches.Count == 2)
                {
                    // Pinch to zoom
                    float currentDistance = GetPinchDistance();
                    float deltaDistance = currentDistance - lastPinchDistance;

                    if (Mathf.Abs(deltaDistance) > 1f)
                    {
                        float zoomDelta = 1f + (deltaDistance * 0.005f);
                        ApplyZoom(zoomDelta, GetPinchCenter());
                        lastPinchDistance = currentDistance;
                    }
                    evt.StopPropagation();
                    return;
                }
            }

            // Connection drag handling
            if (connectionDragHandler.IsDragging)
            {
                connectionDragHandler.UpdateDrag(evt.position);
                evt.StopPropagation();
                return;
            }

            // 배경 드래그 threshold 체크 (좌클릭/터치 후 약간 움직이면 팬 시작)
            if (!isPanning && !isBackgroundDragging && isLongPressing)
            {
                float dist = Vector2.Distance(evt.position, dragStartPosition);
                if (dist > dragThreshold)
                {
                    // Threshold 넘으면 배경 드래그 시작
                    isBackgroundDragging = true;
                    isPanning = true;
                    isLongPressing = false; // 롱프레스 취소
                }
            }

            // Panning
            if (isPanning)
            {
                Vector2 delta = (Vector2)evt.position - lastMousePosition;
                panOffset += delta;

                nodeContainer.transform.position = new Vector3(panOffset.x, panOffset.y, 0);
                gridBackground.SetOffset(panOffset);

                lastMousePosition = evt.position;
                needsConnectionRedraw = true;

                evt.StopPropagation();
            }

            // 롱프레스 중 움직임 체크 (threshold 안넘었을 때)
            if (isLongPressing && !isBackgroundDragging)
            {
                float dist = Vector2.Distance(evt.position, longPressStartPos);
                if (dist > 10f)
                {
                    isLongPressing = false;
                }
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            // Update touch tracking
            if (evt.pointerType != "mouse")
            {
                activeTouches.Remove(evt.pointerId);

                if (activeTouches.Count < 2)
                {
                    isPinching = false;
                }
            }

            // Handle pan/drag release
            if (evt.button == 2 || evt.button == 0)
            {
                isPanning = false;
                isBackgroundDragging = false;
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

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerType != "mouse")
            {
                activeTouches.Remove(evt.pointerId);
                if (activeTouches.Count < 2)
                {
                    isPinching = false;
                }
            }
        }

        private float GetPinchDistance()
        {
            if (activeTouches.Count < 2) return 0f;

            var touchList = new List<Vector2>(activeTouches.Values);
            return Vector2.Distance(touchList[0], touchList[1]);
        }

        private Vector2 GetPinchCenter()
        {
            if (activeTouches.Count < 2) return Vector2.zero;

            var touchList = new List<Vector2>(activeTouches.Values);
            return (touchList[0] + touchList[1]) / 2f;
        }

        private void ApplyZoom(float zoomDelta, Vector2 zoomCenter)
        {
            float oldZoom = zoomScale;
            zoomScale = Mathf.Clamp(zoomScale * zoomDelta, 0.1f, 3f);

            // Adjust pan to zoom towards center point
            Vector2 worldPoint = (zoomCenter - panOffset) / oldZoom;
            panOffset = zoomCenter - worldPoint * zoomScale;

            nodeContainer.transform.scale = new Vector3(zoomScale, zoomScale, 1);
            nodeContainer.transform.position = new Vector3(panOffset.x, panOffset.y, 0);
            gridBackground.SetZoom(zoomScale);
            gridBackground.SetOffset(panOffset);

            needsConnectionRedraw = true;
        }

        /// <summary>
        /// 주어진 점에서 가장 가까운 간선을 찾습니다 (클릭 감지용)
        /// </summary>
        private NodeConnection FindConnectionAtPoint(Vector2 point)
        {
            if (graph == null) return null;

            const float clickThreshold = 10f; // 클릭 허용 거리 (픽셀)
            NodeConnection closestConnection = null;
            float closestDistance = float.MaxValue;

            foreach (var connection in graph.connections)
            {
                if (!connection.IsValid()) continue;

                var outputNode = connection.outputSlot.owner;
                var inputNode = connection.inputSlot.owner;

                if (!nodeViews.ContainsKey(outputNode) || !nodeViews.ContainsKey(inputNode))
                    continue;

                var outputPos = GetSlotWorldPosition(outputNode, connection.outputSlot, true);
                var inputPos = GetSlotWorldPosition(inputNode, connection.inputSlot, false);

                float distance = Mathf.Abs(inputPos.x - outputPos.x);
                
                float handleOffset = Mathf.Min(distance * 0.5f, 100f);
                Vector2 outputTangent = outputPos + new Vector2(handleOffset, 0);
                Vector2 inputTangent = inputPos - new Vector2(handleOffset, 0);

                float dist = GetDistanceToBezier(point, outputPos, outputTangent, inputTangent, inputPos);

                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestConnection = connection;
                }
            }

            // Threshold 이내의 가장 가까운 connection 반환
            if (closestDistance <= clickThreshold)
                return closestConnection;

            return null;
        }

        /// <summary>
        /// 점과 베지어 곡선 사이의 최소 거리를 계산합니다
        /// </summary>
        private float GetDistanceToBezier(Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float minDistance = float.MaxValue;

            Debug.Log(point);
            Debug.Log(p0);

            const int samples = 50;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 curvePoint = CubicBezier(p0, p1, p2, p3, t);
                float dist = Vector2.Distance(point, curvePoint);

                if (dist < minDistance)
                    minDistance = dist;
            }

            return minDistance;
        }

        private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0;
            p += 3f * uu * t * p1;
            p += 3f * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        private void DrawConnections(MeshGenerationContext ctx)
        {
            if (graph == null) return;

            var painter = ctx.painter2D;

            foreach (var connection in graph.connections)
            {
                if (!connection.IsValid()) continue;

                var outputNode = connection.outputSlot.owner;
                var inputNode = connection.inputSlot.owner;

                if (!nodeViews.ContainsKey(outputNode) || !nodeViews.ContainsKey(inputNode))
                    continue;

                var outputPos = GetSlotWorldPosition(outputNode, connection.outputSlot, true);
                var inputPos = GetSlotWorldPosition(inputNode, connection.inputSlot, false);

                bool isSelected = connection == selectedConnection;
                painter.lineWidth = isSelected ? 5f : 3f;
                painter.strokeColor = isSelected ? Color.yellow : connection.connectionColor;

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

            // 실제 렌더링된 슬롯 element의 위치 사용
            var portElement = nodeView.GetSlotElement(slot);
            if (portElement != null)
            {
                // worldBound는 panel 기준 절대 좌표
                // connectionLayer 좌표계로 변환 (NodeGraphView 로컬 좌표 = connectionLayer 로컬 좌표)
                Vector2 worldPos = portElement.worldBound.center;
                Vector2 localPos = this.WorldToLocal(worldPos);
                return localPos;
            }

            // Fallback: 수동 계산 (element가 아직 렌더링 안된 경우)
            Vector2 slotPos = nodeView.GetSlotPosition(slot);
            return slotPos * zoomScale + panOffset;
        }
    }
}