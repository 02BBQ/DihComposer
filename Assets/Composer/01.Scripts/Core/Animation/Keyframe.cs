using System;
using UnityEngine;

namespace VFXComposer.Core.Animation
{
    /// <summary>
    /// 키프레임 - 특정 시간의 값과 보간 정보
    /// </summary>
    [Serializable]
    public class Keyframe
    {
        public float time;              // 시간 (초 단위)
        public object value;            // 값 (float, int, Vector2, Color 등)
        public InterpolationType interpolation = InterpolationType.Linear;

        // Bezier 보간용 (나중에 구현)
        public Vector2 inTangent;
        public Vector2 outTangent;

        public Keyframe(float time, object value)
        {
            this.time = time;
            this.value = value;
        }

        public Keyframe(float time, object value, InterpolationType interpolation)
        {
            this.time = time;
            this.value = value;
            this.interpolation = interpolation;
        }
    }

    /// <summary>
    /// 보간 타입
    /// </summary>
    public enum InterpolationType
    {
        Constant,   // 계단식 (보간 없음)
        Linear,     // 선형 보간
        EaseIn,     // Ease In (가속)
        EaseOut,    // Ease Out (감속)
        EaseInOut,  // Ease In-Out (가속 후 감속)
        Bezier      // 베지어 곡선 (커스텀 탄젠트)
    }
}
