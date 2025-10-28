/*
 * SerializationHelper.cs
 *
 * 복잡한 데이터 타입의 직렬화/역직렬화를 돕는 헬퍼 클래스
 *
 * 주요 기능:
 * - SerializeValue(): object를 JSON string으로 변환
 * - DeserializeValue(): JSON string을 특정 타입의 object로 복원
 * - Unity 타입들(Vector2, Vector3, Color)을 커스텀 데이터 클래스로 변환
 *
 * Unity JsonUtility는 제네릭과 Dictionary를 지원하지 않으므로
 * 간단한 타입들은 string 변환하여 저장
 */

using System;
using UnityEngine;

namespace VFXComposer.Core.Serialization
{
    public static class SerializationHelper
    {
        public static string SerializeValue(object value, out string typeName)
        {
            if (value == null)
            {
                typeName = "null";
                return "";
            }

            Type type = value.GetType();
            typeName = type.Name;

            if (value is float f)
                return f.ToString();
            else if (value is int i)
                return i.ToString();
            else if (value is bool b)
                return b.ToString();
            else if (value is string s)
                return s;
            else if (value is Vector2 v2)
                return JsonUtility.ToJson(new Vector2Data(v2));
            else if (value is Vector3 v3)
                return JsonUtility.ToJson(new Vector3Data(v3));
            else if (value is Color c)
                return JsonUtility.ToJson(new ColorData(c));
            else
                return JsonUtility.ToJson(value);
        }

        public static object DeserializeValue(string valueJson, string typeName)
        {
            if (typeName == "null" || string.IsNullOrEmpty(valueJson))
                return null;

            try
            {
                switch (typeName)
                {
                    case "Single":
                    case "Float":
                        return float.Parse(valueJson);

                    case "Int32":
                    case "Int":
                        return int.Parse(valueJson);

                    case "Boolean":
                    case "Bool":
                        return bool.Parse(valueJson);

                    case "String":
                        return valueJson;

                    case "Vector2":
                        return JsonUtility.FromJson<Vector2Data>(valueJson).ToVector2();

                    case "Vector3":
                        return JsonUtility.FromJson<Vector3Data>(valueJson).ToVector3();

                    case "Color":
                        return JsonUtility.FromJson<ColorData>(valueJson).ToColor();

                    default:
                        Debug.LogWarning($"Unknown type for deserialization: {typeName}");
                        return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize value: {valueJson} as {typeName}. Error: {e.Message}");
                return null;
            }
        }
    }
}
