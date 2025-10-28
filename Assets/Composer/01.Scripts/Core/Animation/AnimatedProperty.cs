/*
 * AnimatedProperty.cs
 *
 * 타임라인에서 애니메이션 가능한 프로퍼티를 표현하는 클래스
 *
 * 구조:
 * - propertyName: 프로퍼티 이름
 * - valueType: C# Type (직렬화 불가능)
 * - dataType: DataType enum (직렬화 가능, 저장용)
 * - interpolationType: 기본 보간 타입 (각 키프레임은 개별 설정 가능)
 * - keyframes: 시간순으로 정렬된 키프레임 리스트
 *
 * 보간 방식:
 * - Evaluate(time) 호출 시 현재 시간의 값을 계산
 * - 두 키프레임 사이에서 interpolationType에 따라 보간
 * - 지원 타입: float, int, Vector2, Vector3, Color, bool
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VFXComposer.Core.Animation
{
    [Serializable]
    public class AnimatedProperty
    {
        public string propertyName;
        public Type valueType;
        public DataType dataType;
        public InterpolationType interpolationType = InterpolationType.Linear;
        public List<Keyframe> keyframes = new List<Keyframe>();

        public AnimatedProperty(string propertyName, Type valueType, DataType dataType)
        {
            this.propertyName = propertyName;
            this.valueType = valueType;
            this.dataType = dataType;
        }

        public AnimatedProperty(string propertyName, Type valueType, DataType dataType, InterpolationType interpolationType)
        {
            this.propertyName = propertyName;
            this.valueType = valueType;
            this.dataType = dataType;
            this.interpolationType = interpolationType;
        }

        public void AddKeyframe(Keyframe keyframe)
        {
            if (keyframe.interpolation == InterpolationType.Linear)
            {
                keyframe.interpolation = interpolationType;
            }
            keyframes.Add(keyframe);
            keyframes = keyframes.OrderBy(k => k.time).ToList();
        }

        public void RemoveKeyframe(Keyframe keyframe)
        {
            keyframes.Remove(keyframe);
        }

        public Keyframe GetKeyframeAtTime(float time, float tolerance = 0.01f)
        {
            return keyframes.FirstOrDefault(k => Mathf.Abs(k.time - time) < tolerance);
        }

        public object Evaluate(float time)
        {
            if (keyframes.Count == 0)
                return GetDefaultValue();

            // 첫 번째 키프레임보다 이전
            if (time <= keyframes[0].time)
                return keyframes[0].value;

            // 마지막 키프레임 이후
            if (time >= keyframes[keyframes.Count - 1].time)
                return keyframes[keyframes.Count - 1].value;

            // 두 키프레임 사이에서 보간
            for (int i = 0; i < keyframes.Count - 1; i++)
            {
                Keyframe current = keyframes[i];
                Keyframe next = keyframes[i + 1];

                if (time >= current.time && time <= next.time)
                {
                    return Interpolate(current, next, time);
                }
            }

            return keyframes[keyframes.Count - 1].value;
        }

        private object Interpolate(Keyframe from, Keyframe to, float time)
        {
            float duration = to.time - from.time;
            float t = (time - from.time) / duration;

            // 보간 타입에 따른 t 값 조정
            t = ApplyInterpolationCurve(t, from.interpolation);

            // 타입별 보간
            if (valueType == typeof(float))
            {
                return Mathf.Lerp((float)from.value, (float)to.value, t);
            }
            else if (valueType == typeof(int))
            {
                return Mathf.RoundToInt(Mathf.Lerp((int)from.value, (int)to.value, t));
            }
            else if (valueType == typeof(Vector2))
            {
                return Vector2.Lerp((Vector2)from.value, (Vector2)to.value, t);
            }
            else if (valueType == typeof(Vector3))
            {
                return Vector3.Lerp((Vector3)from.value, (Vector3)to.value, t);
            }
            else if (valueType == typeof(Color))
            {
                return Color.Lerp((Color)from.value, (Color)to.value, t);
            }
            else if (valueType == typeof(bool))
            {
                // Bool은 Constant 보간만 지원
                return t < 0.5f ? from.value : to.value;
            }

            return from.value;
        }

        private float ApplyInterpolationCurve(float t, InterpolationType interpolation)
        {
            switch (interpolation)
            {
                case InterpolationType.Constant:
                    return 0f; // 항상 from 값 반환

                case InterpolationType.Linear:
                    return t;

                case InterpolationType.EaseIn:
                    return t * t;

                case InterpolationType.EaseOut:
                    return 1f - (1f - t) * (1f - t);

                case InterpolationType.EaseInOut:
                    return t < 0.5f
                        ? 2f * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

                case InterpolationType.Bezier:
                    return t;

                default:
                    return t;
            }
        }

        private object GetDefaultValue()
        {
            if (valueType == typeof(float)) return 0f;
            if (valueType == typeof(int)) return 0;
            if (valueType == typeof(Vector2)) return Vector2.zero;
            if (valueType == typeof(Vector3)) return Vector3.zero;
            if (valueType == typeof(Color)) return Color.white;
            if (valueType == typeof(bool)) return false;
            return null;
        }

        public bool HasKeyframes => keyframes.Count > 0;
    }

    public enum DataType
    {
        Float,
        Int,
        Vector2,
        Vector3,
        Color,
        Bool
    }
}
