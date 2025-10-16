using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VFXComposer.Core
{
    /// <summary>
    /// 노드 그래프의 실행 순서를 관리하는 클래스
    /// Topological Sort를 사용하여 의존성 순서대로 노드를 실행
    /// </summary>
    public class NodeExecutor
    {
        private NodeGraph graph;
        
        public NodeExecutor(NodeGraph graph)
        {
            this.graph = graph;
        }
        
        /// <summary>
        /// 그래프 실행 (최적화된 순서로)
        /// </summary>
        public void Execute()
        {
            // 순환 참조 검사
            if (graph.HasCycle())
            {
                Debug.LogError("Cannot execute graph: Cycle detected!");
                return;
            }
            
            // 실행 순서 계산
            var executionOrder = GetExecutionOrder();
            
            if (executionOrder.Count == 0)
            {
                Debug.LogWarning("No nodes to execute");
                return;
            }
            
            // 모든 노드 초기화
            foreach (var node in graph.nodes)
            {
                node.ResetExecution();
            }
            
            // 순서대로 실행
            foreach (var node in executionOrder)
            {
                if (!node.isExecuted)
                {
                    node.Execute();
                }
            }
        }
        
        /// <summary>
        /// Topological Sort를 사용한 실행 순서 계산
        /// </summary>
        private List<Node> GetExecutionOrder()
        {
            var executionOrder = new List<Node>();
            var visited = new HashSet<Node>();
            var tempMarked = new HashSet<Node>();
            
            // 모든 노드에 대해 DFS 수행
            foreach (var node in graph.nodes)
            {
                if (!visited.Contains(node))
                {
                    if (!TopologicalSortVisit(node, visited, tempMarked, executionOrder))
                    {
                        return new List<Node>(); // 순환 참조 발견
                    }
                }
            }
            
            // 역순으로 반환 (의존성이 먼저 실행되도록)
            executionOrder.Reverse();
            return executionOrder;
        }
        
        /// <summary>
        /// DFS를 사용한 Topological Sort 방문
        /// </summary>
        private bool TopologicalSortVisit(Node node, HashSet<Node> visited, HashSet<Node> tempMarked, List<Node> executionOrder)
        {
            if (tempMarked.Contains(node))
            {
                return false; // 순환 참조 발견
            }
            
            if (visited.Contains(node))
            {
                return true;
            }
            
            tempMarked.Add(node);
            
            // 이 노드의 입력에 연결된 노드들을 먼저 방문
            foreach (var inputSlot in node.inputSlots)
            {
                if (inputSlot.connectedSlot != null)
                {
                    var sourceNode = inputSlot.connectedSlot.owner;
                    if (!TopologicalSortVisit(sourceNode, visited, tempMarked, executionOrder))
                    {
                        return false;
                    }
                }
            }
            
            tempMarked.Remove(node);
            visited.Add(node);
            executionOrder.Add(node);
            
            return true;
        }
        
        /// <summary>
        /// 특정 노드만 실행 (해당 노드의 의존성 포함)
        /// </summary>
        public void ExecuteNode(Node targetNode)
        {
            if (!graph.nodes.Contains(targetNode))
            {
                Debug.LogError("Node not in graph!");
                return;
            }
            
            var nodesToExecute = GetDependencies(targetNode);
            nodesToExecute.Add(targetNode);
            
            // 초기화
            foreach (var node in nodesToExecute)
            {
                node.ResetExecution();
            }
            
            // 의존성 순서대로 실행
            var executionOrder = TopologicalSortSubset(nodesToExecute);
            foreach (var node in executionOrder)
            {
                node.Execute();
            }
        }
        
        /// <summary>
        /// 특정 노드의 모든 의존성 찾기
        /// </summary>
        private List<Node> GetDependencies(Node node)
        {
            var dependencies = new List<Node>();
            var visited = new HashSet<Node>();
            
            GetDependenciesRecursive(node, dependencies, visited);
            
            return dependencies;
        }
        
        private void GetDependenciesRecursive(Node node, List<Node> dependencies, HashSet<Node> visited)
        {
            if (visited.Contains(node))
                return;
            
            visited.Add(node);
            
            foreach (var inputSlot in node.inputSlots)
            {
                if (inputSlot.connectedSlot != null)
                {
                    var sourceNode = inputSlot.connectedSlot.owner;
                    dependencies.Add(sourceNode);
                    GetDependenciesRecursive(sourceNode, dependencies, visited);
                }
            }
        }
        
        /// <summary>
        /// 부분 노드 집합에 대한 Topological Sort
        /// </summary>
        private List<Node> TopologicalSortSubset(List<Node> nodes)
        {
            var executionOrder = new List<Node>();
            var visited = new HashSet<Node>();
            var tempMarked = new HashSet<Node>();
            
            foreach (var node in nodes)
            {
                if (!visited.Contains(node))
                {
                    TopologicalSortVisit(node, visited, tempMarked, executionOrder);
                }
            }
            
            executionOrder.Reverse();
            return executionOrder;
        }
    }
}