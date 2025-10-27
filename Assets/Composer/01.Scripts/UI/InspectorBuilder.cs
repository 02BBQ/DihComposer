using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using VFXComposer.Core;
using VFXComposer.Core.Animation;

namespace VFXComposer.UI
{
    /// <summary>
    /// Reflection을 사용하여 Node의 Attribute를 읽고 자동으로 Inspector UI를 생성하는 빌더
    /// </summary>
    public class InspectorBuilder
    {
        private VisualElement container;
        private Node targetNode;
        private Action onValueChanged;
        private TimelineController timelineController;

        // 성능 최적화를 위한 Type별 캐싱
        private static Dictionary<Type, List<InspectorFieldInfo>> fieldCache = new Dictionary<Type, List<InspectorFieldInfo>>();

        public InspectorBuilder(VisualElement container, Node node, Action onValueChanged, TimelineController timeline = null)
        {
            this.container = container;
            this.targetNode = node;
            this.onValueChanged = onValueChanged;
            this.timelineController = timeline;
        }

        /// <summary>
        /// Node의 Attribute를 읽어 자동으로 Inspector 생성
        /// </summary>
        public void Build()
        {
            var fields = GetInspectorFields(targetNode.GetType());

            // Section별로 그룹화
            var sections = fields.GroupBy(f => f.Section ?? "Properties")
                                .OrderBy(g => fields.First(f => (f.Section ?? "Properties") == g.Key).Order);

            foreach (var section in sections)
            {
                // Section 헤더 추가
                AddSection(section.Key);

                // 필드들을 Order 순서대로 추가
                foreach (var fieldInfo in section.OrderBy(f => f.Order))
                {
                    CreateFieldUI(fieldInfo);
                }
            }
        }

        /// <summary>
        /// Type에서 [InspectorField] Attribute가 붙은 필드/프로퍼티 추출 (캐싱됨)
        /// </summary>
        private List<InspectorFieldInfo> GetInspectorFields(Type nodeType)
        {
            // 캐시 확인
            if (fieldCache.TryGetValue(nodeType, out var cachedFields))
            {
                return cachedFields;
            }

            var fields = new List<InspectorFieldInfo>();

            // Public Fields
            var publicFields = nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in publicFields)
            {
                var attr = field.GetCustomAttribute<InspectorFieldAttribute>();
                if (attr == null || field.GetCustomAttribute<HideInInspectorAttribute>() != null)
                    continue;

                fields.Add(new InspectorFieldInfo
                {
                    MemberInfo = field,
                    Attribute = attr,
                    Label = attr.Label ?? field.Name,
                    Order = attr.Order,
                    Section = attr.Section,
                    Tooltip = attr.Tooltip,
                    ValueType = field.FieldType,
                    RangeAttr = field.GetCustomAttribute<VFXComposer.Core.RangeAttribute>(),
                    InfoAttr = field.GetCustomAttribute<InspectorInfoAttribute>()
                });
            }

            // Public Properties
            var publicProps = nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in publicProps)
            {
                var attr = prop.GetCustomAttribute<InspectorFieldAttribute>();
                if (attr == null || prop.GetCustomAttribute<HideInInspectorAttribute>() != null)
                    continue;

                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                fields.Add(new InspectorFieldInfo
                {
                    MemberInfo = prop,
                    Attribute = attr,
                    Label = attr.Label ?? prop.Name,
                    Order = attr.Order,
                    Section = attr.Section,
                    Tooltip = attr.Tooltip,
                    ValueType = prop.PropertyType,
                    RangeAttr = prop.GetCustomAttribute<VFXComposer.Core.RangeAttribute>(),
                    InfoAttr = prop.GetCustomAttribute<InspectorInfoAttribute>()
                });
            }

            // 캐시에 저장
            fieldCache[nodeType] = fields;

            return fields;
        }

        /// <summary>
        /// 필드 정보에 맞는 UI Element 생성
        /// </summary>
        private void CreateFieldUI(InspectorFieldInfo fieldInfo)
        {
            Type fieldType = fieldInfo.ValueType;
            object currentValue = GetValue(fieldInfo.MemberInfo);

            // Enum 타입
            if (fieldType.IsEnum)
            {
                AddEnumField(fieldInfo, currentValue);
            }
            // Float 타입
            else if (fieldType == typeof(float))
            {
                AddFloatField(fieldInfo, (float)currentValue);
            }
            // Int 타입
            else if (fieldType == typeof(int))
            {
                AddIntField(fieldInfo, (int)currentValue);
            }
            // Bool 타입
            else if (fieldType == typeof(bool))
            {
                AddBoolField(fieldInfo, (bool)currentValue);
            }
            // Color 타입
            else if (fieldType == typeof(Color))
            {
                AddColorField(fieldInfo, (Color)currentValue);
            }
            // Vector2 타입
            else if (fieldType == typeof(Vector2))
            {
                AddVector2Field(fieldInfo, (Vector2)currentValue);
            }
            // Vector3 타입
            else if (fieldType == typeof(Vector3))
            {
                AddVector3Field(fieldInfo, (Vector3)currentValue);
            }
            // String 타입
            else if (fieldType == typeof(string))
            {
                AddStringField(fieldInfo, (string)currentValue);
            }
            else
            {
                Debug.LogWarning($"Unsupported inspector field type: {fieldType.Name}");
            }

            // Info Attribute가 있으면 설명 추가
            if (fieldInfo.InfoAttr != null)
            {
                AddInfo(fieldInfo.InfoAttr.Text);
            }
        }

        // --- UI Element 생성 메서드들 ---

        private void AddSection(string title)
        {
            var sectionContainer = new VisualElement();
            sectionContainer.AddToClassList("inspector__section-container");
            container.Add(sectionContainer);

            var sectionLabel = new Label(title);
            sectionLabel.AddToClassList("inspector__section");
            sectionContainer.Add(sectionLabel);
        }

        private void AddEnumField(InspectorFieldInfo fieldInfo, object currentValue)
        {
            var enumField = new EnumField(fieldInfo.Label, (Enum)currentValue);
            enumField.RegisterValueChangedCallback(evt =>
            {
                SetValue(fieldInfo.MemberInfo, evt.newValue);
                onValueChanged?.Invoke();
            });
            enumField.AddToClassList("inspector__field");
            container.Add(enumField);
        }

        private void AddFloatField(InspectorFieldInfo fieldInfo, float currentValue)
        {
            // 필드와 키프레임 버튼을 담을 컨테이너
            var fieldContainer = new VisualElement();
            fieldContainer.style.flexDirection = FlexDirection.Row;
            fieldContainer.AddToClassList("inspector__field-container");

            var floatField = new FloatField(fieldInfo.Label);
            floatField.value = currentValue;
            floatField.formatString = "F3";
            floatField.style.flexGrow = 1;
            floatField.RegisterValueChangedCallback(evt =>
            {
                // Range 속성이 있으면 Clamp 적용
                float value = evt.newValue;
                if (fieldInfo.RangeAttr != null)
                {
                    value = Mathf.Clamp(value, fieldInfo.RangeAttr.Min, fieldInfo.RangeAttr.Max);
                    if (value != evt.newValue)
                    {
                        floatField.SetValueWithoutNotify(value);
                    }
                }

                SetValue(fieldInfo.MemberInfo, value);
                onValueChanged?.Invoke();
            });
            floatField.AddToClassList("inspector__field");
            fieldContainer.Add(floatField);

            // 키프레임 버튼 추가 (타임라인이 있을 때만)
            if (timelineController != null)
            {
                var keyframeButton = CreateKeyframeButton(fieldInfo);
                fieldContainer.Add(keyframeButton);
            }

            container.Add(fieldContainer);
        }

        private void AddIntField(InspectorFieldInfo fieldInfo, int currentValue)
        {
            var fieldContainer = new VisualElement();
            fieldContainer.style.flexDirection = FlexDirection.Row;
            fieldContainer.AddToClassList("inspector__field-container");

            var intField = new IntegerField(fieldInfo.Label);
            intField.value = currentValue;
            intField.style.flexGrow = 1;
            intField.RegisterValueChangedCallback(evt =>
            {
                // Range 속성이 있으면 Clamp 적용
                int value = evt.newValue;
                if (fieldInfo.RangeAttr != null)
                {
                    value = Mathf.Clamp(value, (int)fieldInfo.RangeAttr.Min, (int)fieldInfo.RangeAttr.Max);
                    if (value != evt.newValue)
                    {
                        intField.SetValueWithoutNotify(value);
                    }
                }

                SetValue(fieldInfo.MemberInfo, value);
                onValueChanged?.Invoke();
            });
            intField.AddToClassList("inspector__field");
            fieldContainer.Add(intField);

            if (timelineController != null)
            {
                var keyframeButton = CreateKeyframeButton(fieldInfo);
                fieldContainer.Add(keyframeButton);
            }

            container.Add(fieldContainer);
        }

        private void AddBoolField(InspectorFieldInfo fieldInfo, bool currentValue)
        {
            var toggle = new Toggle(fieldInfo.Label);
            toggle.value = currentValue;
            toggle.RegisterValueChangedCallback(evt =>
            {
                SetValue(fieldInfo.MemberInfo, evt.newValue);
                onValueChanged?.Invoke();
            });
            toggle.AddToClassList("inspector__field");
            container.Add(toggle);
        }

        private void AddColorField(InspectorFieldInfo fieldInfo, Color currentValue)
        {
            var mainContainer = new VisualElement();
            mainContainer.AddToClassList("inspector__field-container");

            // 라벨과 키프레임 버튼을 위한 헤더 행
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.marginBottom = 4;
            mainContainer.Add(headerRow);

            var label = new Label(fieldInfo.Label);
            label.style.flexGrow = 1;
            headerRow.Add(label);

            if (timelineController != null)
            {
                var keyframeButton = CreateKeyframeButton(fieldInfo);
                headerRow.Add(keyframeButton);
            }

            // 색상 프리뷰 박스
            var colorPreview = new VisualElement();
            colorPreview.style.height = 24;
            colorPreview.style.marginBottom = 4;
            colorPreview.style.backgroundColor = currentValue;
            colorPreview.style.borderTopWidth = 1;
            colorPreview.style.borderBottomWidth = 1;
            colorPreview.style.borderLeftWidth = 1;
            colorPreview.style.borderRightWidth = 1;
            colorPreview.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            colorPreview.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            colorPreview.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            colorPreview.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            mainContainer.Add(colorPreview);

            // RGB 슬라이더
            AddColorSlider("R", currentValue.r, (value) =>
            {
                Color newColor = (Color)GetValue(fieldInfo.MemberInfo);
                newColor.r = value;
                SetValue(fieldInfo.MemberInfo, newColor);
                colorPreview.style.backgroundColor = newColor;
                onValueChanged?.Invoke();
            }, mainContainer);

            AddColorSlider("G", currentValue.g, (value) =>
            {
                Color newColor = (Color)GetValue(fieldInfo.MemberInfo);
                newColor.g = value;
                SetValue(fieldInfo.MemberInfo, newColor);
                colorPreview.style.backgroundColor = newColor;
                onValueChanged?.Invoke();
            }, mainContainer);

            AddColorSlider("B", currentValue.b, (value) =>
            {
                Color newColor = (Color)GetValue(fieldInfo.MemberInfo);
                newColor.b = value;
                SetValue(fieldInfo.MemberInfo, newColor);
                colorPreview.style.backgroundColor = newColor;
                onValueChanged?.Invoke();
            }, mainContainer);

            AddColorSlider("A", currentValue.a, (value) =>
            {
                Color newColor = (Color)GetValue(fieldInfo.MemberInfo);
                newColor.a = value;
                SetValue(fieldInfo.MemberInfo, newColor);
                colorPreview.style.backgroundColor = newColor;
                onValueChanged?.Invoke();
            }, mainContainer);

            container.Add(mainContainer);
        }

        private void AddColorSlider(string label, float initialValue, Action<float> onChanged, VisualElement parent)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;

            var labelElement = new Label(label);
            labelElement.style.width = 16;
            labelElement.style.marginRight = 4;
            row.Add(labelElement);

            var slider = new Slider(0f, 1f);
            slider.value = initialValue;
            slider.style.flexGrow = 1;
            slider.style.marginRight = 4;
            slider.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
            row.Add(slider);

            var field = new FloatField();
            field.value = initialValue;
            field.style.width = 50;
            field.formatString = "F2";
            field.RegisterValueChangedCallback(evt =>
            {
                float value = Mathf.Clamp01(evt.newValue);
                if (value != evt.newValue)
                {
                    field.SetValueWithoutNotify(value);
                }
                slider.SetValueWithoutNotify(value);
                onChanged(value);
            });
            row.Add(field);

            // 슬라이더와 필드 동기화
            slider.RegisterValueChangedCallback(evt => field.SetValueWithoutNotify(evt.newValue));

            parent.Add(row);
        }

        private void AddVector2Field(InspectorFieldInfo fieldInfo, Vector2 currentValue)
        {
            var fieldContainer = new VisualElement();
            fieldContainer.style.flexDirection = FlexDirection.Row;
            fieldContainer.AddToClassList("inspector__field-container");

            var vec2Field = new Vector2Field(fieldInfo.Label);
            vec2Field.value = currentValue;
            vec2Field.style.flexGrow = 1;
            vec2Field.RegisterValueChangedCallback(evt =>
            {
                SetValue(fieldInfo.MemberInfo, evt.newValue);
                onValueChanged?.Invoke();
            });
            vec2Field.AddToClassList("inspector__field");
            fieldContainer.Add(vec2Field);

            if (timelineController != null)
            {
                var keyframeButton = CreateKeyframeButton(fieldInfo);
                fieldContainer.Add(keyframeButton);
            }

            container.Add(fieldContainer);
        }

        private void AddVector3Field(InspectorFieldInfo fieldInfo, Vector3 currentValue)
        {
            var fieldContainer = new VisualElement();
            fieldContainer.style.flexDirection = FlexDirection.Row;
            fieldContainer.AddToClassList("inspector__field-container");

            var vec3Field = new Vector3Field(fieldInfo.Label);
            vec3Field.value = currentValue;
            vec3Field.style.flexGrow = 1;
            vec3Field.RegisterValueChangedCallback(evt =>
            {
                SetValue(fieldInfo.MemberInfo, evt.newValue);
                onValueChanged?.Invoke();
            });
            vec3Field.AddToClassList("inspector__field");
            fieldContainer.Add(vec3Field);

            if (timelineController != null)
            {
                var keyframeButton = CreateKeyframeButton(fieldInfo);
                fieldContainer.Add(keyframeButton);
            }

            container.Add(fieldContainer);
        }

        private void AddStringField(InspectorFieldInfo fieldInfo, string currentValue)
        {
            var textField = new TextField(fieldInfo.Label);
            textField.value = currentValue ?? "";
            textField.RegisterValueChangedCallback(evt =>
            {
                SetValue(fieldInfo.MemberInfo, evt.newValue);
                onValueChanged?.Invoke();
            });
            textField.AddToClassList("inspector__field");
            container.Add(textField);
        }

        private void AddInfo(string text)
        {
            var infoLabel = new Label(text);
            infoLabel.AddToClassList("inspector__info");
            container.Add(infoLabel);
        }

        /// <summary>
        /// 키프레임 버튼 생성
        /// </summary>
        private Button CreateKeyframeButton(InspectorFieldInfo fieldInfo)
        {
            var button = new Button();
            button.text = "◆";
            button.style.width = 32;
            button.style.marginLeft = 4;

            // 초기 상태 설정
            UpdateKeyframeButtonState(button, fieldInfo);

            // 클릭 이벤트
            button.clicked += () =>
            {
                string propertyName = fieldInfo.MemberInfo.Name;
                object currentValue = GetValue(fieldInfo.MemberInfo);

                // 현재 시간에 키프레임이 있는지 확인
                if (timelineController.HasKeyframeAtCurrentTime(targetNode, propertyName))
                {
                    // 있으면 제거
                    timelineController.RemoveKeyframe(targetNode, propertyName, timelineController.currentTime);
                }
                else
                {
                    // 없으면 추가
                    timelineController.AddKeyframe(targetNode, propertyName, currentValue);
                }

                // 버튼 상태 업데이트
                UpdateKeyframeButtonState(button, fieldInfo);
            };

            // 타임라인 변경 시 버튼 상태 업데이트
            timelineController.OnTimeChanged += (time) => UpdateKeyframeButtonState(button, fieldInfo);
            timelineController.OnKeyframeAdded += () => UpdateKeyframeButtonState(button, fieldInfo);
            timelineController.OnKeyframeRemoved += () => UpdateKeyframeButtonState(button, fieldInfo);

            return button;
        }

        /// <summary>
        /// 키프레임 버튼 상태 업데이트
        /// </summary>
        private void UpdateKeyframeButtonState(Button button, InspectorFieldInfo fieldInfo)
        {
            string propertyName = fieldInfo.MemberInfo.Name;

            // 애니메이션되어 있는지 확인
            bool isAnimated = timelineController.IsPropertyAnimated(targetNode, propertyName);

            // 현재 시간에 키프레임이 있는지 확인
            bool hasKeyframe = timelineController.HasKeyframeAtCurrentTime(targetNode, propertyName);

            // 색상 업데이트
            if (hasKeyframe)
            {
                // 현재 시간에 키프레임이 있음 - 밝은 노란색
                button.style.color = new Color(1f, 0.9f, 0.3f);
            }
            else if (isAnimated)
            {
                // 애니메이션되어 있지만 현재 시간에는 키프레임 없음 - 주황색
                button.style.color = new Color(1f, 0.6f, 0.2f);
            }
            else
            {
                // 애니메이션 안됨 - 회색
                button.style.color = new Color(0.5f, 0.5f, 0.5f);
            }
        }

        // --- Reflection 헬퍼 메서드 ---

        private object GetValue(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.GetValue(targetNode);
            }
            else if (member is PropertyInfo prop)
            {
                return prop.GetValue(targetNode);
            }
            return null;
        }

        private void SetValue(MemberInfo member, object value)
        {
            if (member is FieldInfo field)
            {
                field.SetValue(targetNode, value);
            }
            else if (member is PropertyInfo prop)
            {
                prop.SetValue(targetNode, value);
            }
        }

        /// <summary>
        /// 필드 정보를 담는 내부 클래스
        /// </summary>
        private class InspectorFieldInfo
        {
            public MemberInfo MemberInfo;
            public InspectorFieldAttribute Attribute;
            public string Label;
            public int Order;
            public string Section;
            public string Tooltip;
            public Type ValueType;
            public VFXComposer.Core.RangeAttribute RangeAttr;
            public InspectorInfoAttribute InfoAttr;
        }
    }
}
