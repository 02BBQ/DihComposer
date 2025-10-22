using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using VFXComposer.Core;
using UnityEditor.UIElements;

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

        // 성능 최적화를 위한 Type별 캐싱
        private static Dictionary<Type, List<InspectorFieldInfo>> fieldCache = new Dictionary<Type, List<InspectorFieldInfo>>();

        public InspectorBuilder(VisualElement container, Node node, Action onValueChanged)
        {
            this.container = container;
            this.targetNode = node;
            this.onValueChanged = onValueChanged;
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
            if (fieldInfo.RangeAttr != null)
            {
                // Slider 사용
                var slider = new Slider(fieldInfo.Label, fieldInfo.RangeAttr.Min, fieldInfo.RangeAttr.Max);
                slider.value = currentValue;
                slider.RegisterValueChangedCallback(evt =>
                {
                    SetValue(fieldInfo.MemberInfo, evt.newValue);
                    onValueChanged?.Invoke();
                });
                slider.AddToClassList("inspector__field");
                container.Add(slider);
            }
            else
            {
                // 일반 FloatField
                var floatField = new FloatField(fieldInfo.Label);
                floatField.value = currentValue;
                floatField.formatString = "F3";
                floatField.RegisterValueChangedCallback(evt =>
                {
                    SetValue(fieldInfo.MemberInfo, evt.newValue);
                    onValueChanged?.Invoke();
                });
                floatField.AddToClassList("inspector__field");
                container.Add(floatField);
            }
        }

        private void AddIntField(InspectorFieldInfo fieldInfo, int currentValue)
        {
            if (fieldInfo.RangeAttr != null)
            {
                // SliderInt 사용
                var slider = new SliderInt(fieldInfo.Label, (int)fieldInfo.RangeAttr.Min, (int)fieldInfo.RangeAttr.Max);
                slider.value = currentValue;
                slider.RegisterValueChangedCallback(evt =>
                {
                    SetValue(fieldInfo.MemberInfo, evt.newValue);
                    onValueChanged?.Invoke();
                });
                slider.AddToClassList("inspector__field");
                container.Add(slider);
            }
            else
            {
                // 일반 IntegerField
                var intField = new IntegerField(fieldInfo.Label);
                intField.value = currentValue;
                intField.RegisterValueChangedCallback(evt =>
                {
                    SetValue(fieldInfo.MemberInfo, evt.newValue);
                    onValueChanged?.Invoke();
                });
                intField.AddToClassList("inspector__field");
                container.Add(intField);
            }
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
            // 기존 AddColorField 로직 재사용 가능하도록 간단한 버전
            var colorField = new ColorField(fieldInfo.Label);
            colorField.value = currentValue;
            colorField.RegisterValueChangedCallback(evt =>
            {
                SetValue(fieldInfo.MemberInfo, evt.newValue);
                onValueChanged?.Invoke();
            });
            colorField.AddToClassList("inspector__field");
            container.Add(colorField);
        }

        private void AddVector2Field(InspectorFieldInfo fieldInfo, Vector2 currentValue)
        {
            var vec2Field = new Vector2Field(fieldInfo.Label);
            vec2Field.value = currentValue;
            vec2Field.RegisterValueChangedCallback(evt =>
            {
                SetValue(fieldInfo.MemberInfo, evt.newValue);
                onValueChanged?.Invoke();
            });
            vec2Field.AddToClassList("inspector__field");
            container.Add(vec2Field);
        }

        private void AddVector3Field(InspectorFieldInfo fieldInfo, Vector3 currentValue)
        {
            var vec3Field = new Vector3Field(fieldInfo.Label);
            vec3Field.value = currentValue;
            vec3Field.RegisterValueChangedCallback(evt =>
            {
                SetValue(fieldInfo.MemberInfo, evt.newValue);
                onValueChanged?.Invoke();
            });
            vec3Field.AddToClassList("inspector__field");
            container.Add(vec3Field);
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
