using System;

namespace VFXComposer.Core
{
    /// <summary>
    /// Inspector에 자동으로 필드를 표시하기 위한 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class InspectorFieldAttribute : Attribute
    {
        /// <summary>
        /// Inspector에 표시될 레이블
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 필드 표시 순서 (낮을수록 먼저 표시)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 섹션 이름 (그룹화용)
        /// </summary>
        public string Section { get; set; }

        /// <summary>
        /// 툴팁 설명
        /// </summary>
        public string Tooltip { get; set; }

        public InspectorFieldAttribute(string label = null)
        {
            Label = label;
            Order = 0;
            Section = null;
            Tooltip = null;
        }
    }

    /// <summary>
    /// Float/Int 필드에 Range 제한을 추가하는 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class RangeAttribute : Attribute
    {
        public float Min { get; }
        public float Max { get; }

        public RangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Inspector 정보 레이블 표시용 Attribute (읽기 전용)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class InspectorInfoAttribute : Attribute
    {
        public string Text { get; }

        public InspectorInfoAttribute(string text)
        {
            Text = text;
        }
    }

    /// <summary>
    /// Inspector에서 해당 필드를 숨기는 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class HideInInspectorAttribute : Attribute
    {
    }
}
