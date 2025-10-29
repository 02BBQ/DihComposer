using System.Collections.Generic;

namespace VFXComposer.Core
{
    public class ConnectSlotsCommand : ICommand
    {
        private NodeGraph graph;
        private NodeSlot outputSlot;
        private NodeSlot inputSlot;
        private NodeConnection connection;
        private NodeSlot previousConnection;
        private readonly HashSet<Node> downstreamNodes;

        public ConnectSlotsCommand(NodeGraph graph, NodeSlot outputSlot, NodeSlot inputSlot)
        {
            this.graph = graph;
            this.outputSlot = outputSlot;
            this.inputSlot = inputSlot;

            if (inputSlot.connectedSlot != null)
            {
                previousConnection = inputSlot.connectedSlot;
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
            connection = graph.ConnectSlots(outputSlot, inputSlot);
            NodeGraphExecutionHelper.InvalidateAndExecuteDownstream(graph, downstreamNodes);
        }

        public void Undo()
        {
            if (connection != null)
            {
                graph.DisconnectConnection(connection);
            }

            if (previousConnection != null)
            {
                graph.ConnectSlots(previousConnection, inputSlot);
            }

            NodeGraphExecutionHelper.InvalidateAndExecuteDownstream(graph, downstreamNodes);
        }

        public string GetDescription()
        {
            return $"Connect {outputSlot.owner.nodeName}.{outputSlot.displayName} → {inputSlot.owner.nodeName}.{inputSlot.displayName}";
        }
    }
}
