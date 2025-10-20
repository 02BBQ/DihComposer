using UnityEngine;

namespace VFXComposer.Core
{
    /// <summary>
    /// 노드 추가 Command
    /// </summary>
    public class AddNodeCommand : ICommand
    {
        private NodeGraph graph;
        private Node node;
        private System.Action<Node> onAdd;
        private System.Action<Node> onRemove;

        public AddNodeCommand(NodeGraph graph, Node node, System.Action<Node> onAdd, System.Action<Node> onRemove)
        {
            this.graph = graph;
            this.node = node;
            this.onAdd = onAdd;
            this.onRemove = onRemove;
        }

        public void Execute()
        {
            graph.AddNode(node);
            onAdd?.Invoke(node);
        }

        public void Undo()
        {
            graph.RemoveNode(node);
            onRemove?.Invoke(node);
        }

        public string GetDescription()
        {
            return $"Add {node.nodeName}";
        }
    }
}
