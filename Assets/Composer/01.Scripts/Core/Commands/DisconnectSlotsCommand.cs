using System.Collections.Generic;

namespace VFXComposer.Core
{
    public class DisconnectSlotsCommand : ICommand
    {
        private NodeGraph graph;
        private NodeSlot outputSlot;
        private NodeSlot inputSlot;
        private NodeConnection connection;
        private readonly HashSet<Node> downstreamNodes;

        public DisconnectSlotsCommand(NodeGraph graph, NodeSlot outputSlot, NodeSlot inputSlot)
        {
            this.graph = graph;
            this.outputSlot = outputSlot;
            this.inputSlot = inputSlot;

            foreach (var conn in graph.connections)
            {
                if (conn.outputSlot == outputSlot && conn.inputSlot == inputSlot)
                {
                    connection = conn;
                    break;
                }
            }

            if (inputSlot != null && inputSlot.owner != null)
            {
                downstreamNodes = graph.GetDownstreamNodes(inputSlot.owner);
                downstreamNodes.Add(inputSlot.owner);
            }
            else
            {
                downstreamNodes = new HashSet<Node>();
            }
        }

        public void Execute()
        {
            if (connection != null)
            {
                graph.DisconnectConnection(connection);
                NodeGraphExecutionHelper.InvalidateAndExecuteDownstream(graph, downstreamNodes);
            }
        }

        public void Undo()
        {
            if (connection != null)
            {
                graph.ConnectSlots(outputSlot, inputSlot);
                NodeGraphExecutionHelper.InvalidateAndExecuteDownstream(graph, downstreamNodes);
            }
        }

        public string GetDescription()
        {
            return $"Disconnect {outputSlot.owner.nodeName}.{outputSlot.displayName} → {inputSlot.owner.nodeName}.{inputSlot.displayName}";
        }
    }
}
