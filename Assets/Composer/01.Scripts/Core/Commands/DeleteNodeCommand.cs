using System.Collections.Generic;
using System.Linq;

namespace VFXComposer.Core
{
    public class DeleteNodeCommand : ICommand
    {
        private NodeGraph graph;
        private Node node;
        private List<NodeConnection> savedConnections;
        private readonly HashSet<Node> downstreamNodes;
        private System.Action<Node> onRemove;
        private System.Action<Node> onAdd;

        public DeleteNodeCommand(NodeGraph graph, Node node, System.Action<Node> onRemove, System.Action<Node> onAdd)
        {
            this.graph = graph;
            this.node = node;
            this.onRemove = onRemove;
            this.onAdd = onAdd;

            savedConnections = graph.connections
                .Where(c => (c.outputSlot != null && c.outputSlot.owner == node) ||
                           (c.inputSlot != null && c.inputSlot.owner == node))
                .ToList();

            downstreamNodes = graph.GetDownstreamNodes(node);
        }

        public void Execute()
        {
            graph.RemoveNode(node);
            onRemove?.Invoke(node);
            NodeGraphExecutionHelper.InvalidateAndExecuteDownstream(graph, downstreamNodes);
        }

        public void Undo()
        {
            graph.AddNode(node);
            onAdd?.Invoke(node);

            foreach (var connection in savedConnections)
            {
                if (connection.outputSlot != null && connection.inputSlot != null)
                {
                    graph.ConnectSlots(connection.outputSlot, connection.inputSlot);
                }
            }

            NodeGraphExecutionHelper.InvalidateAndExecuteDownstream(graph, downstreamNodes);
        }

        public string GetDescription()
        {
            return $"Delete {node.nodeName}";
        }
    }
}
