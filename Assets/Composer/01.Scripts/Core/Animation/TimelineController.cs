using System;
using System.Collections.Generic;
using UnityEngine;

namespace VFXComposer.Core.Animation
{
    public class TimelineController
    {
        // 타임라인 상태
        public float currentTime = 0f;
        public float duration = 10f;
        public bool isPlaying = false;
        public bool loop = true;
        public float playbackSpeed = 1f;
        public int fps = 30;                // 프레임 레이트 (기본 30fps)

        // 노드별 애니메이션 프로퍼티
        private Dictionary<string, AnimatedProperty> animatedProperties = new Dictionary<string, AnimatedProperty>();

        public event Action<float> OnTimeChanged;       
        public event Action<bool> OnPlayStateChanged;   
        public event Action OnKeyframeAdded;            
        public event Action OnKeyframeRemoved;          

        private float lastUpdateTime;

        public TimelineController()
        {
            lastUpdateTime = Time.time;
        }

        public void Update()
        {
            if (!isPlaying) return;

            float deltaTime = (Time.time - lastUpdateTime) * playbackSpeed;
            lastUpdateTime = Time.time;

            currentTime += deltaTime;

            // 루프 처리
            if (currentTime > duration)
            {
                if (loop)
                {
                    currentTime = currentTime % duration;
                }
                else
                {
                    currentTime = duration;
                    Pause();
                }
            }

            OnTimeChanged?.Invoke(currentTime);
        }

        public void Play()
        {
            isPlaying = true;
            lastUpdateTime = Time.time;
            OnPlayStateChanged?.Invoke(isPlaying);
        }

        public void Pause()
        {
            isPlaying = false;
            OnPlayStateChanged?.Invoke(isPlaying);
        }

        /// <summary>
        /// 0초
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            currentTime = 0f;
            OnPlayStateChanged?.Invoke(isPlaying);
            OnTimeChanged?.Invoke(currentTime);
        }

        /// <summary>
        /// 현재 시간 설정 (Scrubbing)
        /// </summary>
        public void SetTime(float time)
        {
            currentTime = Mathf.Clamp(time, 0f, duration);
            OnTimeChanged?.Invoke(currentTime);
        }

        public void AddKeyframe(Node node, string propertyName, object value)
        {
            DataType dataType = GetDataTypeFromValue(value);
            AddKeyframe(node, propertyName, value, dataType, InterpolationType.Linear);
        }

        public void AddKeyframe(string propertyKey, float time, object value, DataType dataType, InterpolationType interpolationType)
        {
            if (!animatedProperties.TryGetValue(propertyKey, out var animProp))
            {
                Type valueType = GetTypeFromDataType(dataType);
                animProp = new AnimatedProperty(propertyKey.Split('.')[1], valueType, dataType, interpolationType);
                animatedProperties[propertyKey] = animProp;
                Debug.Log($"[Timeline] Created new AnimatedProperty: {propertyKey}");
            }

            var keyframe = new Keyframe(time, value, interpolationType);
            animProp.AddKeyframe(keyframe);

            Debug.Log($"[Timeline] Added keyframe: {propertyKey} = {value} at {time:F2}s");

            OnKeyframeAdded?.Invoke();
        }

        private void AddKeyframe(Node node, string propertyName, object value, DataType dataType, InterpolationType interpolationType)
        {
            string key = GetPropertyKey(node, propertyName);

            if (!animatedProperties.TryGetValue(key, out var animProp))
            {
                Type valueType = value.GetType();
                animProp = new AnimatedProperty(propertyName, valueType, dataType, interpolationType);
                animatedProperties[key] = animProp;
                Debug.Log($"[Timeline] Created new AnimatedProperty: {key}");
            }

            var keyframe = new Keyframe(currentTime, value, interpolationType);
            animProp.AddKeyframe(keyframe);

            Debug.Log($"[Timeline] Added keyframe: {key} = {value} at {currentTime:F2}s (frame {CurrentFrame})");

            OnKeyframeAdded?.Invoke();
        }

        private DataType GetDataTypeFromValue(object value)
        {
            if (value is float) return DataType.Float;
            if (value is int) return DataType.Int;
            if (value is Vector2) return DataType.Vector2;
            if (value is Vector3) return DataType.Vector3;
            if (value is Color) return DataType.Color;
            if (value is bool) return DataType.Bool;
            return DataType.Float;
        }

        private Type GetTypeFromDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Float: return typeof(float);
                case DataType.Int: return typeof(int);
                case DataType.Vector2: return typeof(Vector2);
                case DataType.Vector3: return typeof(Vector3);
                case DataType.Color: return typeof(Color);
                case DataType.Bool: return typeof(bool);
                default: return typeof(float);
            }
        }

        public void RemoveKeyframe(Node node, string propertyName, float time)
        {
            string key = GetPropertyKey(node, propertyName);

            if (animatedProperties.TryGetValue(key, out var animProp))
            {
                var keyframe = animProp.GetKeyframeAtTime(time);
                if (keyframe != null)
                {
                    animProp.RemoveKeyframe(keyframe);
                    OnKeyframeRemoved?.Invoke();
                }
            }
        }

        /// <summary>
        /// 현재 시간에 키프레임이 있는지 확인
        /// </summary>
        public bool HasKeyframeAtCurrentTime(Node node, string propertyName)
        {
            string key = GetPropertyKey(node, propertyName);

            if (animatedProperties.TryGetValue(key, out var animProp))
            {
                return animProp.GetKeyframeAtTime(currentTime) != null;
            }

            return false;
        }

        public object GetAnimatedValue(Node node, string propertyName)
        {
            string key = GetPropertyKey(node, propertyName);

            if (animatedProperties.TryGetValue(key, out var animProp))
            {
                return animProp.Evaluate(currentTime);
            }

            return null;
        }

        public bool IsPropertyAnimated(Node node, string propertyName)
        {
            string key = GetPropertyKey(node, propertyName);
            return animatedProperties.ContainsKey(key) && animatedProperties[key].HasKeyframes;
        }

        /// <summary>
        /// 특정 프로퍼티의 모든 키프레임 가져오기
        /// </summary>
        public List<Keyframe> GetKeyframes(Node node, string propertyName)
        {
            string key = GetPropertyKey(node, propertyName);

            if (animatedProperties.TryGetValue(key, out var animProp))
            {
                return new List<Keyframe>(animProp.keyframes);
            }

            return new List<Keyframe>();
        }

        /// <summary>
        /// 모든 애니메이션 프로퍼티 가져오기
        /// </summary>
        public Dictionary<string, AnimatedProperty> GetAllAnimatedProperties()
        {
            return new Dictionary<string, AnimatedProperty>(animatedProperties);
        }

        private string GetPropertyKey(Node node, string propertyName)
        {
            return $"{node.id}.{propertyName}";
        }

        // === 프레임 단위 유틸리티 ===

        /// <summary>
        /// 시간을 프레임으로 변환
        /// </summary>
        public int TimeToFrame(float time)
        {
            return Mathf.RoundToInt(time * fps);
        }

        /// <summary>
        /// 프레임을 시간으로 변환
        /// </summary>
        public float FrameToTime(int frame)
        {
            return frame / (float)fps;
        }

        /// <summary>
        /// 현재 프레임 가져오기
        /// </summary>
        public int CurrentFrame => TimeToFrame(currentTime);

        /// <summary>
        /// 전체 프레임 수 가져오기
        /// </summary>
        public int TotalFrames => TimeToFrame(duration);

        /// <summary>
        /// 프레임 단위로 시간 설정
        /// </summary>
        public void SetFrame(int frame)
        {
            float time = FrameToTime(frame);
            SetTime(time);
        }

        /// <summary>
        /// 다음 프레임으로 이동
        /// </summary>
        public void NextFrame()
        {
            SetFrame(CurrentFrame + 1);
        }

        /// <summary>
        /// 이전 프레임으로 이동
        /// </summary>
        public void PreviousFrame()
        {
            SetFrame(CurrentFrame - 1);
        }

        /// <summary>
        /// 시간을 프레임에 스냅
        /// </summary>
        public void SnapToFrame()
        {
            currentTime = FrameToTime(CurrentFrame);
            OnTimeChanged?.Invoke(currentTime);
        }
    }
}
