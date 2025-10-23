namespace VFXComposer.Core
{
    /// <summary>
    /// 슬롯 연결 해제 Command
    /// </summary>
    public class DisconnectSlotsCommand : ICommand
    {
        private NodeGraph graph;
        private NodeSlot outputSlot;
        private NodeSlot inputSlot;
        private NodeConnection connection;

        public DisconnectSlotsCommand(NodeGraph graph, NodeSlot outputSlot, NodeSlot inputSlot)
        {
            this.graph = graph;
            this.outputSlot = outputSlot;
            this.inputSlot = inputSlot;

            // 연결을 찾아서 저장 (Undo를 위해)
            foreach (var conn in graph.connections)
            {
                if (conn.outputSlot == outputSlot && conn.inputSlot == inputSlot)
                {
                    connection = conn;
                    break;
                }
            }
        }

        public void Execute()
        {
            if (connection != null)
            {
                graph.DisconnectConnection(connection);
            }
        }

        public void Undo()
        {
            if (connection != null)
            {
                graph.ConnectSlots(outputSlot, inputSlot);
            }
        }

        public string GetDescription()
        {
            return $"Disconnect {outputSlot.owner.nodeName}.{outputSlot.displayName} → {inputSlot.owner.nodeName}.{inputSlot.displayName}";
        }
    }
}
