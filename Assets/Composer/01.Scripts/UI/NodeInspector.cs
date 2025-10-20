using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;
using System; // System.Action을 위해 추가

namespace VFXComposer.UI
{
    public class NodeInspector : VisualElement
    {
        private Node currentNode;
        private VisualElement propertiesContainer;
        private Label titleLabel;
        private NodeGraphView graphView;
        private Image previewImage;

        public NodeInspector(NodeGraphView view)
        {
            graphView = view;

            // 전체 인스펙터 스타일 클래스
            AddToClassList("node-inspector");

            // Title Container: 모바일에서 상단 헤더처럼 보이도록 컨테이너 추가
            var titleContainer = new VisualElement();
            titleContainer.AddToClassList("inspector__header");
            Add(titleContainer);

            // Title
            titleLabel = new Label("Inspector");
            titleLabel.AddToClassList("inspector__title");
            titleContainer.Add(titleLabel);

            // Properties container: 스크롤 가능하도록 설정
            propertiesContainer = new VisualElement();
            propertiesContainer.AddToClassList("inspector__properties-container");
            propertiesContainer.style.flexGrow = 1;
            Add(propertiesContainer);

            // Schedule preview updates
            schedule.Execute(UpdatePreview).Every(100);
        }

        public void ShowNodeProperties(Node node)
        {
            currentNode = node;
            propertiesContainer.Clear();
            previewImage = null;

            if (node == null)
            {
                titleLabel.text = "Inspector";
                var emptyLabel = new Label("No node selected");
                emptyLabel.AddToClassList("inspector__empty");
                propertiesContainer.Add(emptyLabel);
                return;
            }

            titleLabel.text = node.nodeName;

            // Add preview image
            previewImage = new Image();
            previewImage.AddToClassList("inspector__preview");
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            propertiesContainer.Add(previewImage);

            // Add properties based on node type
            if (node is GradientNode gradientNode)
            {
                BuildGradientNodeInspector(gradientNode);
            }
            else if (node is NoiseNode noiseNode)
            {
                BuildNoiseNodeInspector(noiseNode);
            }
            else if (node is ShapeNode shapeNode)
            {
                BuildShapeNodeInspector(shapeNode);
            }
            else if (node is OutputNode outputNode)
            {
                BuildOutputNodeInspector(outputNode);
            }
            
            // 공통 버튼 (예: 노드 삭제) 추가
            AddDeleteButton(node);
        }

        // --- 반복되는 필드 추가 로직을 간소화하는 제네릭 메서드 ---
        private void AddField<TField, TValue>(string label, TValue initialValue, Action<TValue> onValueChanged)
            where TField : BaseField<TValue>, new()
        {
            var field = new TField { label = label, value = initialValue };

            // FloatField의 경우 모바일에서 너무 작은 입력값을 막기 위해 정밀도 설정
            if (field is FloatField floatField)
            {
                floatField.formatString = "F3";
            }

            field.RegisterValueChangedCallback(evt =>
            {
                onValueChanged?.Invoke(evt.newValue);
                ExecuteNode(currentNode); // currentNode를 사용하도록 변경
            });

            field.AddToClassList("inspector__field");
            propertiesContainer.Add(field);
        }

        // EnumField는 특별한 초기화가 필요하므로 별도 메서드
        private void AddEnumField<TEnum>(string label, TEnum initialValue, Action<TEnum> onValueChanged) where TEnum : System.Enum
        {
            var enumField = new EnumField(label, initialValue);

            enumField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is TEnum enumValue)
                {
                    onValueChanged?.Invoke(enumValue);
                    ExecuteNode(currentNode);
                }
            });

            enumField.AddToClassList("inspector__field");
            propertiesContainer.Add(enumField);
        }
        
        // --- 빌더 메서드 개선 (AddField 사용) ---

        private void BuildGradientNodeInspector(GradientNode node)
        {
            AddSection("🎨 Gradient Colors");

            // 기존 AddColorField를 그대로 사용 (내부 CSS로 디자인 변경)
            AddColorField("Color A", node.colorA, newColor => node.colorA = newColor);
            AddColorField("Color B", node.colorB, newColor => node.colorB = newColor);

            AddSection("⚙️ Settings");

            // EnumField (Type)
            AddEnumField("Gradient Type", node.gradientType, newValue => node.gradientType = newValue);

            // FloatField (Angle)
            AddField<FloatField, float>("Angle (°)", node.angle, newValue => node.angle = newValue);
        }

        private void BuildNoiseNodeInspector(NoiseNode node)
        {
            AddSection("🌀 Noise Properties");

            // EnumField (Type)
            AddEnumField("Noise Type", node.noiseType, newValue => node.noiseType = newValue);

            // FloatField (Scale)
            AddField<FloatField, float>("Scale", node.scale, newValue => node.scale = newValue);

            // IntegerField (Octaves)
            AddField<IntegerField, int>("Octaves", node.octaves, newValue => node.octaves = newValue);

            // FloatField (Persistence)
            AddField<FloatField, float>("Persistence", node.persistence, newValue => node.persistence = newValue);

            // Vector2Field (Offset)
            // Vector2Field는 BaseField<Vector2>를 상속하므로 AddField 사용 가능
            AddField<Vector2Field, Vector2>("Offset", node.offset, newValue => node.offset = newValue);
        }

        private void BuildShapeNodeInspector(ShapeNode node)
        {
            AddSection("📐 Shape Properties");

            // EnumField (Type)
            AddEnumField("Shape Type", node.shapeType, newValue => node.shapeType = newValue);

            // FloatField (Size)
            AddField<FloatField, float>("Size", node.size, newValue => node.size = newValue);

            // FloatField (Smoothness)
            AddField<FloatField, float>("Smoothness", node.smoothness, newValue => node.smoothness = newValue);

            AddSection("🎨 Colors");

            AddColorField("Fill Color", node.fillColor, newColor => node.fillColor = newColor);
            AddColorField("Background Color", node.backgroundColor, newColor => node.backgroundColor = newColor);
        }

        private void BuildOutputNodeInspector(OutputNode node)
        {
            AddSection("🖥️ Output Node");

            var infoLabel = new Label("This is the final output node.\nConnect texture or color inputs.");
            infoLabel.AddToClassList("inspector__info");
            propertiesContainer.Add(infoLabel);
        }
        
        // --- 유틸리티 메서드 개선 및 추가 ---

        private void AddSection(string title)
        {
            // 수직 공간 추가를 위한 컨테이너 (모바일에서 깔끔한 여백)
            var sectionContainer = new VisualElement();
            sectionContainer.AddToClassList("inspector__section-container");
            propertiesContainer.Add(sectionContainer);
            
            var sectionLabel = new Label(title);
            sectionLabel.AddToClassList("inspector__section");
            sectionContainer.Add(sectionLabel);
        }

        private void AddColorField(string label, Color initialColor, System.Action<Color> onValueChanged)
        {
            var container = new VisualElement();
            // 기존 "inspector__color-field" 클래스는 유지하되, 내부 구조 변경
            container.AddToClassList("inspector__color-field"); 
            
            // Label과 Preview를 담는 상단 컨테이너 (FlexRow로 깔끔하게)
            var headerContainer = new VisualElement();
            headerContainer.AddToClassList("inspector__color-header");
            container.Add(headerContainer);

            var labelElement = new Label(label);
            labelElement.AddToClassList("inspector__color-label");
            headerContainer.Add(labelElement);

            // Color preview box
            var colorPreview = new VisualElement();
            colorPreview.AddToClassList("inspector__color-preview");
            colorPreview.style.backgroundColor = initialColor;
            headerContainer.Add(colorPreview);

            Color currentColor = initialColor;

            // R, G, B, A 슬라이더는 그대로 유지 (모바일 터치에 적합한 크기로 CSS에서 조정)
            Action<Slider, Action<Color>> setupSlider = (slider, updateColor) =>
            {
                slider.RegisterValueChangedCallback(evt =>
                {
                    updateColor?.Invoke(currentColor);
                    colorPreview.style.backgroundColor = currentColor;
                    onValueChanged?.Invoke(currentColor);
                    ExecuteNode(currentNode);
                });
                slider.AddToClassList("inspector__color-slider");
                container.Add(slider);
            };

            // R Slider
            var rSlider = new Slider("R", 0f, 1f, SliderDirection.Horizontal);
            rSlider.value = initialColor.r;
            setupSlider(rSlider, (c) => currentColor.r = rSlider.value);
            rSlider.AddToClassList("inspector__r-slider"); // 색상별 스타일링을 위해 클래스 추가

            // G Slider
            var gSlider = new Slider("G", 0f, 1f, SliderDirection.Horizontal);
            gSlider.value = initialColor.g;
            setupSlider(gSlider, (c) => currentColor.g = gSlider.value);
            gSlider.AddToClassList("inspector__g-slider");

            // B Slider
            var bSlider = new Slider("B", 0f, 1f, SliderDirection.Horizontal);
            bSlider.value = initialColor.b;
            setupSlider(bSlider, (c) => currentColor.b = bSlider.value);
            bSlider.AddToClassList("inspector__b-slider");

            // A Slider (Alpha)
            var aSlider = new Slider("A", 0f, 1f, SliderDirection.Horizontal);
            aSlider.value = initialColor.a;
            setupSlider(aSlider, (c) => currentColor.a = aSlider.value);
            aSlider.AddToClassList("inspector__a-slider");

            propertiesContainer.Add(container);
        }
        
        private void UpdatePreview()
        {
            if (currentNode == null || previewImage == null) return;

            if (currentNode.cachedOutputs.Count > 0)
            {
                foreach (var output in currentNode.cachedOutputs.Values)
                {
                    if (output is RenderTexture rt)
                    {
                        previewImage.image = rt;
                        return;
                    }
                    else if (output is Color col)
                    {
                        var tex = new Texture2D(1, 1);
                        tex.SetPixel(0, 0, col);
                        tex.Apply();
                        previewImage.image = tex;
                        return;
                    }
                }
            }
        }

        private void AddDeleteButton(Node node)
        {
            var deleteButton = new Button(() =>
            {
                if (graphView != null && node != null)
                {
                    graphView.DeleteNode(node);
                }
            });
            deleteButton.text = "🗑️ Delete Node";
            deleteButton.AddToClassList("inspector__delete-button");
            propertiesContainer.Add(deleteButton);
        }


        // 기존 ExecuteNode 메서드는 그대로 유지
        private void ExecuteNode(Node node)
        {
            if (graphView == null || node == null) return;

            var graph = graphView.GetGraph();
            if (graph == null) return;

            node.ResetExecution();
            var executor = new NodeExecutor(graph);
            executor.Execute();
        }
    }
}