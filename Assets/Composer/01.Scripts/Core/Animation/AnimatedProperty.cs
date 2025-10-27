using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VFXComposer.Core.Animation
{
    /// <summary>
    /// 애니메이션 가능한 프로퍼티 - 키프레임 리스트 관리 및 보간
    /// </summary>
    [Serializable]
    public class AnimatedProperty
    {
        public string propertyName;     // 프로퍼티 이름 (예: "scale", "color.r")
        public Type valueType;          // 값 타입 (float, int, Vector2, Color 등)
        public List<Keyframe> keyframes = new List<Keyframe>();

        public AnimatedProperty(string propertyName, Type valueType)
        {
            this.propertyName = propertyName;
            this.valueType = valueType;
        }

        /// <summary>
        /// 키프레임 추가 (시간 순서대로 정렬됨)
        /// </summary>
        public void AddKeyframe(Keyframe keyframe)
        {
            keyframes.Add(keyframe);
            keyframes = keyframes.OrderBy(k => k.time).ToList();
        }

        /// <summary>
        /// 키프레임 제거
        /// </summary>
        public void RemoveKeyframe(Keyframe keyframe)
        {
            keyframes.Remove(keyframe);
        }

        /// <summary>
        /// 특정 시간의 키프레임 찾기
        /// </summary>
        public Keyframe GetKeyframeAtTime(float time, float tolerance = 0.01f)
        {
            return keyframes.FirstOrDefault(k => Mathf.Abs(k.time - time) < tolerance);
        }

        /// <summary>
        /// 주어진 시간에서의 값을 보간하여 반환
        /// </summary>
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

        /// <summary>
        /// 두 키프레임 사이의 값 보간
        /// </summary>
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

        /// <summary>
        /// 보간 타입에 따른 커브 적용
        /// </summary>
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
                    // TODO: 베지어 곡선 구현
                    return t;

                default:
                    return t;
            }
        }

        /// <summary>
        /// 기본값 반환
        /// </summary>
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

        /// <summary>
        /// 키프레임이 있는지 확인
        /// </summary>
        public bool HasKeyframes => keyframes.Count > 0;
    }
}
