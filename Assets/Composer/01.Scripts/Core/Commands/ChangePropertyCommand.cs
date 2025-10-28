/*
 * ChangePropertyCommand.cs
 *
 * 노드 프로퍼티 값 변경을 위한 Command 패턴 구현
 *
 * 구조:
 * - node: 대상 노드
 * - memberInfo: 변경할 필드/프로퍼티 정보 (Reflection)
 * - oldValue: 이전 값 (Undo용)
 * - newValue: 새 값 (Execute/Redo용)
 * - onValueChanged: 값 변경 시 호출할 콜백 (UI 업데이트용)
 *
 * 사용 예:
 * var command = new ChangePropertyCommand(node, fieldInfo, oldValue, newValue, () => UpdateUI());
 * commandHistory.ExecuteCommand(command);
 */

using System;
using System.Reflection;
using UnityEngine;

namespace VFXComposer.Core
{
    public class ChangePropertyCommand : ICommand
    {
        private Node node;
        private MemberInfo memberInfo;
        private object oldValue;
        private object newValue;
        private Action onValueChanged;

        public ChangePropertyCommand(
            Node node,
            MemberInfo memberInfo,
            object oldValue,
            object newValue,
            Action onValueChanged = null)
        {
            this.node = node;
            this.memberInfo = memberInfo;
            this.oldValue = CloneValue(oldValue);
            this.newValue = CloneValue(newValue);
            this.onValueChanged = onValueChanged;
        }

        public void Execute()
        {
            SetValue(newValue);
            onValueChanged?.Invoke();
        }

        public void Undo()
        {
            SetValue(oldValue);
            onValueChanged?.Invoke();
        }

        public string GetDescription()
        {
            string propertyName = memberInfo.Name;
            return $"Change {node.nodeName}.{propertyName}";
        }

        private void SetValue(object value)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                fieldInfo.SetValue(node, value);
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                propertyInfo.SetValue(node, value);
            }
        }

        private object CloneValue(object value)
        {
            if (value == null) return null;

            Type type = value.GetType();

            if (type.IsValueType || type == typeof(string))
            {
                return value;
            }

            if (value is Vector2 v2)
                return new Vector2(v2.x, v2.y);
            else if (value is Vector3 v3)
                return new Vector3(v3.x, v3.y, v3.z);
            else if (value is Color c)
                return new Color(c.r, c.g, c.b, c.a);
            else
            {
                Debug.LogWarning($"[ChangePropertyCommand] Unsupported type for cloning: {type.Name}");
                return value;
            }
        }
    }
}
