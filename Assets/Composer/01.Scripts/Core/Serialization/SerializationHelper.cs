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

            // Enum은 FullName 저장 (네임스페이스 포함) - Unity 타입과 충돌 방지
            if (type.IsEnum)
            {
                typeName = type.FullName;
                return value.ToString();
            }

            // 나머지는 Name만 저장
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
            else if (IsUnityEngineType(type))
            {
                // Unity engine types는 직렬화하지 않음 (Texture, RenderTexture, Gradient 등)
                typeName = "UnityEngineType";
                Debug.LogWarning($"[SerializationHelper] Skipping Unity engine type: {type.FullName}");
                return "";
            }
            else
            {
                // 기타 직렬화 가능한 타입
                try
                {
                    return JsonUtility.ToJson(value);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SerializationHelper] Failed to serialize type {type.Name}: {e.Message}");
                    typeName = "UnserializableType";
                    return "";
                }
            }
        }

        /// <summary>
        /// Unity Engine 타입인지 확인 (직렬화 불가능한 타입들)
        /// </summary>
        private static bool IsUnityEngineType(Type type)
        {
            // UnityEngine 네임스페이스 확인
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine"))
            {
                // 직렬화 가능한 Unity 타입은 제외 (Vector2, Vector3, Color는 이미 처리됨)
                if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Color))
                    return false;

                // Texture, RenderTexture, Gradient, AnimationCurve 등은 직렬화 불가
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    return true;

                if (type == typeof(Gradient) || type == typeof(AnimationCurve))
                    return true;
            }

            return false;
        }

        public static object DeserializeValue(string valueJson, string typeName)
        {
            if (typeName == "null" || string.IsNullOrEmpty(valueJson))
                return null;

            // Unity engine type이거나 직렬화 불가능한 타입은 건너뜀
            if (typeName == "UnityEngineType" || typeName == "UnserializableType")
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
                        // Enum 타입 처리 (Reflection 사용)
                        Type enumType = FindTypeByName(typeName);
                        if (enumType != null && enumType.IsEnum)
                        {
                            return Enum.Parse(enumType, valueJson);
                        }

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

        /// <summary>
        /// 타입 이름으로 Assembly에서 Type 찾기 (Enum용)
        /// </summary>
        private static Type FindTypeByName(string typeName)
        {
            // FullName으로 검색 (예: VFXComposer.Core.GradientType)
            if (typeName.Contains("."))
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    Type type = assembly.GetType(typeName);
                    if (type != null) return type;
                }
            }

            // Name만으로 검색
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in allAssemblies)
            {
                // VFXComposer.Core 네임스페이스 우선 검색
                Type type = assembly.GetType($"VFXComposer.Core.{typeName}");
                if (type != null) return type;

                // 전체 검색 (VFXComposer 네임스페이스 우선)
                var types = assembly.GetTypes();
                foreach (var t in types)
                {
                    if (t.Name == typeName && t.FullName.StartsWith("VFXComposer"))
                        return t;
                }

                // 나머지 타입 검색
                foreach (var t in types)
                {
                    if (t.Name == typeName)
                        return t;
                }
            }
            return null;
        }
    }
}
