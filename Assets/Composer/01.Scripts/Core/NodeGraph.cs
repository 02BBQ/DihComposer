using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VFXComposer.Core
{
    /// <summary>
    /// 노드 그래프 전체를 관리하는 클래스
    /// </summary>
    public class NodeGraph
    {
        // 그래프에 속한 모든 노드들
        public List<Node> nodes = new List<Node>();
        
        // 모든 연결들
        public List<NodeConnection> connections = new List<NodeConnection>();
        
        // 그래프 이름
        public string graphName = "New Graph";
        
        /// <summary>
        /// 노드 추가
        /// </summary>
        public void AddNode(Node node)
        {
            if (!nodes.Contains(node))
            {
                // OutputNode는 하나만 존재해야 함
                if (node is OutputNode && GetOutputNode() != null)
                {
                    Debug.LogWarning("OutputNode already exists in the graph!");
                    return;
                }

                nodes.Add(node);
            }
        }

        /// <summary>
        /// OutputNode 가져오기
        /// </summary>
        public OutputNode GetOutputNode()
        {
            return nodes.OfType<OutputNode>().FirstOrDefault();
        }
        
        /// <summary>
        /// 노드 제거
        /// </summary>
        public void RemoveNode(Node node)
        {
            // OutputNode는 삭제 불가능
            if (node is OutputNode)
            {
                Debug.LogWarning("Cannot delete OutputNode!");
                return;
            }

            // 연결된 슬롯들 먼저 해제
            DisconnectNode(node);

            nodes.Remove(node);
        }
        
        /// <summary>
        /// 두 슬롯 연결
        /// </summary>
        public NodeConnection ConnectSlots(NodeSlot output, NodeSlot input)
        {
            if (!output.CanConnectTo(input))
            {
                return null;
            }
            
            // Output이 Input보다 먼저 와야 함
            if (output.slotType != SlotType.Output || input.slotType != SlotType.Input)
            {
                return null;
            }
            
            // Input 슬롯에 이미 연결이 있으면 제거
            if (input.connectedSlot != null)
            {
                DisconnectSlot(input);
            }
            
            // 연결 생성 (graph 참조 전달)
            var connection = new NodeConnection(output, input, this);
            connections.Add(connection);

            // 슬롯에 연결 정보 저장
            input.connectedSlot = output;

            return connection;
        }
        
        /// <summary>
        /// 슬롯 연결 해제
        /// </summary>
        public void DisconnectSlot(NodeSlot slot)
        {
            if (slot.slotType == SlotType.Input && slot.connectedSlot != null)
            {
                // 연결 객체 찾아서 제거
                var connection = connections.FirstOrDefault(c => c.inputSlot == slot);
                if (connection != null)
                {
                    connections.Remove(connection);
                }
                
                slot.connectedSlot = null;
            }
        }
        
        /// <summary>
        /// 연결 객체로 연결 해제
        /// </summary>
        public void DisconnectConnection(NodeConnection connection)
        {
            if (connection != null && connections.Contains(connection))
            {
                if (connection.inputSlot != null)
                {
                    connection.inputSlot.connectedSlot = null;
                }
                connections.Remove(connection);
            }
        }
        
        /// <summary>
        /// 노드의 모든 연결 해제
        /// </summary>
        private void DisconnectNode(Node node)
        {
            // 이 노드와 관련된 모든 연결 찾기
            var nodeConnections = connections.Where(c => 
                (c.outputSlot != null && c.outputSlot.owner == node) ||
                (c.inputSlot != null && c.inputSlot.owner == node)
            ).ToList();
            
            // 연결 제거
            foreach (var connection in nodeConnections)
            {
                if (connection.inputSlot != null)
                {
                    connection.inputSlot.connectedSlot = null;
                }
                connections.Remove(connection);
            }
        }
        
        /// <summary>
        /// 그래프 실행 (모든 노드 실행)
        /// </summary>
        public void Execute()
        {
            // 실행 상태 초기화
            foreach (var node in nodes)
            {
                node.ResetExecution();
            }
            
            // 출력 노드들을 찾아서 실행 (재귀적으로 입력 노드들도 실행됨)
            var outputNodes = GetOutputNodes();
            
            foreach (var node in outputNodes)
            {
                node.Execute();
            }
        }
        
        /// <summary>
        /// 출력 노드들 찾기 (Output 슬롯이 다른 노드에 연결되지 않은 노드들)
        /// </summary>
        private List<Node> GetOutputNodes()
        {
            var outputNodes = new List<Node>();
            
            foreach (var node in nodes)
            {
                bool isOutputNode = true;
                
                // 이 노드의 출력이 다른 노드에 연결되어 있는지 확인
                foreach (var outputSlot in node.outputSlots)
                {
                    if (connections.Any(c => c.outputSlot == outputSlot))
                    {
                        isOutputNode = false;
                        break;
                    }
                }
                
                if (isOutputNode)
                {
                    outputNodes.Add(node);
                }
            }
            
            return outputNodes;
        }
        
        /// <summary>
        /// 순환 참조 검사 (Cycle Detection)
        /// </summary>
        public bool HasCycle()
        {
            var visited = new HashSet<Node>();
            var recursionStack = new HashSet<Node>();
            
            foreach (var node in nodes)
            {
                if (HasCycleUtil(node, visited, recursionStack))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private bool HasCycleUtil(Node node, HashSet<Node> visited, HashSet<Node> recursionStack)
        {
            if (recursionStack.Contains(node))
                return true;
            
            if (visited.Contains(node))
                return false;
            
            visited.Add(node);
            recursionStack.Add(node);
            
            // 이 노드의 출력에 연결된 노드들 확인
            foreach (var outputSlot in node.outputSlots)
            {
                var connectedNodes = connections
                    .Where(c => c.outputSlot == outputSlot)
                    .Select(c => c.inputSlot.owner);
                
                foreach (var connectedNode in connectedNodes)
                {
                    if (HasCycleUtil(connectedNode, visited, recursionStack))
                        return true;
                }
            }
            
            recursionStack.Remove(node);
            return false;
        }
        
        /// <summary>
        /// ID로 노드 찾기
        /// </summary>
        public Node GetNodeById(string id)
        {
            return nodes.FirstOrDefault(n => n.id == id);
        }
        
        /// <summary>
        /// 그래프 초기화
        /// </summary>
        public void Clear()
        {
            nodes.Clear();
            connections.Clear();
        }
    }
}