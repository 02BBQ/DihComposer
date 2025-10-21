using UnityEngine;
using System.Collections.Generic;

namespace VFXComposer.Core
{
    /// <summary>
    /// DataType별 색상을 중앙 집중식으로 관리하는 테이블
    /// 노드 슬롯, 연결선 등 모든 UI 요소에서 일관된 색상을 사용하도록 보장
    /// </summary>
    public static class DataTypeColorTable
    {
        private static readonly Dictionary<DataType, Color> colorTable = new Dictionary<DataType, Color>()
        {
            { DataType.Texture, Color.white },                          // 하얀색
            { DataType.Float, new Color(0.3f, 0.8f, 0.3f) },           // 녹색
            { DataType.Vector2, new Color(1f, 0.8f, 0.3f) },           // 주황색
            { DataType.Vector3, new Color(1f, 0.5f, 0.3f) },           // 진한 주황색
            { DataType.Color, new Color(1f, 1f, 0.3f) }                // 노란색
        };

        /// <summary>
        /// DataType에 해당하는 색상을 반환
        /// </summary>
        /// <param name="dataType">데이터 타입</param>
        /// <returns>해당 타입의 색상</returns>
        public static Color GetColor(DataType dataType)
        {
            if (colorTable.TryGetValue(dataType, out Color color))
            {
                return color;
            }

            // 등록되지 않은 타입의 경우 기본 색상 반환
            Debug.LogWarning($"Color not found for DataType: {dataType}. Returning default white.");
            return Color.white;
        }

        /// <summary>
        /// DataType에 해당하는 색상을 설정 (런타임에서 커스터마이징 가능)
        /// </summary>
        /// <param name="dataType">데이터 타입</param>
        /// <param name="color">설정할 색상</param>
        public static void SetColor(DataType dataType, Color color)
        {
            colorTable[dataType] = color;
        }

        /// <summary>
        /// 모든 색상을 기본값으로 리셋
        /// </summary>
        public static void ResetToDefaults()
        {
            colorTable[DataType.Texture] = Color.white;
            colorTable[DataType.Float] = new Color(0.3f, 0.8f, 0.3f);
            colorTable[DataType.Vector2] = new Color(1f, 0.8f, 0.3f);
            colorTable[DataType.Vector3] = new Color(1f, 0.5f, 0.3f);
            colorTable[DataType.Color] = new Color(1f, 1f, 0.3f);
        }

        /// <summary>
        /// 모든 DataType과 색상 매핑 정보를 반환 (디버깅용)
        /// </summary>
        public static IReadOnlyDictionary<DataType, Color> GetAllColors()
        {
            return colorTable;
        }
    }
}
