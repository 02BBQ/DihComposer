namespace VFXComposer.Core
{
    /// <summary>
    /// 슬롯 연결 Command
    /// </summary>
    public class ConnectSlotsCommand : ICommand
    {
        private NodeGraph graph;
        private NodeSlot outputSlot;
        private NodeSlot inputSlot;
        private NodeConnection connection;
        private NodeSlot previousConnection; // 이전에 연결되어 있던 슬롯 (덮어쓰기 시)

        public ConnectSlotsCommand(NodeGraph graph, NodeSlot outputSlot, NodeSlot inputSlot)
        {
            this.graph = graph;
            this.outputSlot = outputSlot;
            this.inputSlot = inputSlot;

            // 입력 슬롯에 이미 연결이 있었다면 저장
            if (inputSlot.connectedSlot != null)
            {
                previousConnection = inputSlot.connectedSlot;
            }
        }

        public void Execute()
        {
            connection = graph.ConnectSlots(outputSlot, inputSlot);
        }

        public void Undo()
        {
            if (connection != null)
            {
                graph.DisconnectConnection(connection);
            }

            // 이전 연결 복원
            if (previousConnection != null)
            {
                graph.ConnectSlots(previousConnection, inputSlot);
            }
        }

        public string GetDescription()
        {
            return $"Connect {outputSlot.owner.nodeName}.{outputSlot.displayName} → {inputSlot.owner.nodeName}.{inputSlot.displayName}";
        }
    }
}
