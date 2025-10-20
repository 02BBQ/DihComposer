namespace VFXComposer.Core
{
    /// <summary>
    /// 연결 해제 Command
    /// </summary>
    public class DisconnectCommand : ICommand
    {
        private NodeGraph graph;
        private NodeConnection connection;
        private NodeSlot outputSlot;
        private NodeSlot inputSlot;

        public DisconnectCommand(NodeGraph graph, NodeConnection connection)
        {
            this.graph = graph;
            this.connection = connection;
            this.outputSlot = connection.outputSlot;
            this.inputSlot = connection.inputSlot;
        }

        public void Execute()
        {
            graph.DisconnectConnection(connection);
        }

        public void Undo()
        {
            // 연결 복원
            if (outputSlot != null && inputSlot != null)
            {
                graph.ConnectSlots(outputSlot, inputSlot);
            }
        }

        public string GetDescription()
        {
            return $"Disconnect {outputSlot?.owner.nodeName ?? "?"}.{outputSlot?.displayName ?? "?"} from {inputSlot?.owner.nodeName ?? "?"}.{inputSlot?.displayName ?? "?"}";
        }
    }
}
