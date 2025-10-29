using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VFXComposer.Core
{
    public static class NodeGraphExecutionHelper
    {
        public static void InvalidateAndExecuteDownstream(NodeGraph graph, HashSet<Node> downstreamNodes)
        {
            var nodesToExecute = new HashSet<Node>(downstreamNodes);

            foreach (var downstream in nodesToExecute)
            {
                downstream.ResetExecution();
            }

            var outputNodes = graph.GetOutputNodes();
            foreach (var outputNode in outputNodes)
            {
                outputNode.ResetExecution();
                nodesToExecute.Add(outputNode);
            }

            var executor = new NodeExecutor(graph);
            foreach (var node in nodesToExecute)
            {
                executor.ExecuteNode(node);
            }
        }
    }
}
