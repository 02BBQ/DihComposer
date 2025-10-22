using UnityEngine;

namespace VFXComposer.Core
{
    public class ConstantColorNode : Node
    {
        [InspectorField("Color", Order = 0, Section = "🎨 Color")]
        public Color color = Color.white;

        protected override void InitializeSlots()
        {
            nodeName = "Constant Color";
            AddOutputSlot("color_out", "Color", DataType.Color);
        }

        public override void Execute()
        {
            if (isExecuted) return;

            SetOutputValue("color_out", color);
            isExecuted = true;
        }
    }
}
