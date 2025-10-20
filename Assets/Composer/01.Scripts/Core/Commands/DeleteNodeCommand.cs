using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VFXComposer.Core
{
    /// <summary>
    /// 노드 삭제 Command
    /// </summary>
    public class DeleteNodeCommand : ICommand
    {
        private NodeGraph graph;
        private Node node;
        private List<NodeConnection> savedConnections;
        private System.Action<Node> onRemove;
        private System.Action<Node> onAdd;

        public DeleteNodeCommand(NodeGraph graph, Node node, System.Action<Node> onRemove, System.Action<Node> onAdd)
        {
            this.graph = graph;
            this.node = node;
            this.onRemove = onRemove;
            this.onAdd = onAdd;

            // 삭제 전에 연결 정보 저장
            savedConnections = graph.connections
                .Where(c => (c.outputSlot != null && c.outputSlot.owner == node) ||
                           (c.inputSlot != null && c.inputSlot.owner == node))
                .ToList();
        }

        public void Execute()
        {
            graph.RemoveNode(node);
            onRemove?.Invoke(node);
        }

        public void Undo()
        {
            graph.AddNode(node);
            onAdd?.Invoke(node);

            // 연결 복원
            foreach (var connection in savedConnections)
            {
                if (connection.outputSlot != null && connection.inputSlot != null)
                {
                    graph.ConnectSlots(connection.outputSlot, connection.inputSlot);
                }
            }
        }

        public string GetDescription()
        {
            return $"Delete {node.nodeName}";
        }
    }
}
