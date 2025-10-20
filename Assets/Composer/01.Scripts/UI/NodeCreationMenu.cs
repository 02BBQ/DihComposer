using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using VFXComposer.Core;

namespace VFXComposer.UI
{
    public class NodeCreationMenu : VisualElement
    {
        private NodeGraphView graphView;
        private Vector2 spawnPosition;
        
        private static List<NodeMenuItem> nodeTypes = new List<NodeMenuItem>
        {
            new NodeMenuItem("Generator/Constant Color", () => new ConstantColorNode()),
            new NodeMenuItem("Generator/Gradient", () => new GradientNode()),
            new NodeMenuItem("Generator/Shape", () => new ShapeNode()),
            new NodeMenuItem("Generator/Noise", () => new NoiseNode()),
            new NodeMenuItem("Color/Blend", () => new BlendNode()),
            new NodeMenuItem("Math/Add", () => new AddNode()),
            new NodeMenuItem("Math/Subtract", () => new SubtractNode()),
            new NodeMenuItem("Math/Multiply", () => new MultiplyNode()),
            new NodeMenuItem("Math/Power", () => new PowerNode()),
            // OutputNode는 자동으로 생성되므로 메뉴에서 제외
        };
        
        public NodeCreationMenu(NodeGraphView view)
        {
            graphView = view;
            
            style.position = Position.Absolute;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            style.borderTopWidth = 2;
            style.borderBottomWidth = 2;
            style.borderLeftWidth = 2;
            style.borderRightWidth = 2;
            style.borderTopColor = new Color(0.5f, 0.5f, 0.5f);
            style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f);
            style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
            style.borderRightColor = new Color(0.5f, 0.5f, 0.5f);
            style.borderTopLeftRadius = 4;
            style.borderTopRightRadius = 4;
            style.borderBottomLeftRadius = 4;
            style.borderBottomRightRadius = 4;
            style.minWidth = 200;
            style.maxHeight = 400;
            style.paddingTop = 4;
            style.paddingBottom = 4;
            
            var scrollView = new ScrollView();
            scrollView.style.maxHeight = 400;
            Add(scrollView);
            
            foreach (var item in nodeTypes)
            {
                var button = new Button(() => CreateNode(item.factory));
                button.text = item.name;
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.paddingLeft = 8;
                button.style.paddingRight = 8;
                button.style.paddingTop = 6;
                button.style.paddingBottom = 6;
                button.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                button.style.borderTopWidth = 0;
                button.style.borderBottomWidth = 1;
                button.style.borderLeftWidth = 0;
                button.style.borderRightWidth = 0;
                button.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
                button.style.color = Color.white;
                
                scrollView.Add(button);
            }
        }
        
        public void Show(Vector2 position)
        {
            spawnPosition = position;
            style.left = position.x;
            style.top = position.y;
            style.display = DisplayStyle.Flex;
        }
        
        public void Hide()
        {
            style.display = DisplayStyle.None;
        }
        
        private void CreateNode(Func<Node> factory)
        {
            var node = factory();
            // Convert screen coordinates to world coordinates considering pan and zoom
            Vector2 worldPosition = (spawnPosition - graphView.PanOffset) / graphView.ZoomScale;
            node.position = worldPosition;

            graphView.AddNode(node);

            Hide();
        }
        
        private struct NodeMenuItem
        {
            public string name;
            public Func<Node> factory;
            
            public NodeMenuItem(string name, Func<Node> factory)
            {
                this.name = name;
                this.factory = factory;
            }
        }
    }
}