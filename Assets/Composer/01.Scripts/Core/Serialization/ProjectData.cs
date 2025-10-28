/*
 * ProjectData.cs
 *
 * VFX Composer 프로젝트 저장/불러오기를 위한 직렬화 데이터 클래스들
 *
 * 구조:
 * - ProjectData: 최상위 컨테이너 (.vfxc 파일의 전체 내용)
 *   - MetadataInfo: 파일 메타데이터 (버전, 날짜 등)
 *   - GraphDataInfo: 노드 그래프 데이터
 *     - NodeDataInfo[]: 각 노드의 정보 (ID, 타입, 위치, 프로퍼티)
 *     - ConnectionDataInfo[]: 노드 간 연결 정보
 *   - TimelineDataInfo: 타임라인 & 애니메이션 데이터
 *     - AnimatedPropertyInfo[]: 애니메이션된 프로퍼티들
 *       - KeyframeInfo[]: 각 키프레임 정보
 *
 * JSON 직렬화를 위해 모든 클래스는 [System.Serializable] 속성 사용
 * Unity의 JsonUtility는 Dictionary를 지원하지 않으므로 커스텀 직렬화 필요
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VFXComposer.Core.Serialization
{
    [Serializable]
    public class ProjectData
    {
        public MetadataInfo metadata;
        public GraphDataInfo graphData;
        public TimelineDataInfo timelineData;
    }

    [Serializable]
    public class MetadataInfo
    {
        public string version = "1.0";
        public string createdDate;
        public string modifiedDate;
        public string appVersion;
    }

    [Serializable]
    public class GraphDataInfo
    {
        public string graphName;
        public List<NodeDataInfo> nodes = new List<NodeDataInfo>();
        public List<ConnectionDataInfo> connections = new List<ConnectionDataInfo>();
    }

    [Serializable]
    public class NodeDataInfo
    {
        public string id;
        public string nodeType;
        public string nodeName;
        public Vector2Data position;
        public List<PropertyData> properties = new List<PropertyData>();
    }

    [Serializable]
    public class ConnectionDataInfo
    {
        public string outputNodeId;
        public string outputSlotId;
        public string inputNodeId;
        public string inputSlotId;
    }

    [Serializable]
    public class TimelineDataInfo
    {
        public int fps;
        public float duration;
        public float currentTime;
        public List<AnimatedPropertyInfo> animatedProperties = new List<AnimatedPropertyInfo>();
    }

    [Serializable]
    public class AnimatedPropertyInfo
    {
        public string propertyKey;
        public string dataType;
        public string interpolationType;
        public List<KeyframeInfo> keyframes = new List<KeyframeInfo>();
    }

    [Serializable]
    public class KeyframeInfo
    {
        public float time;
        public string valueJson;
    }

    [Serializable]
    public class PropertyData
    {
        public string key;
        public string valueJson;
        public string valueType;
    }

    [Serializable]
    public class Vector2Data
    {
        public float x;
        public float y;

        public Vector2Data() { }

        public Vector2Data(Vector2 v)
        {
            x = v.x;
            y = v.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }

    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data() { }

        public Vector3Data(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class ColorData
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public ColorData() { }

        public ColorData(Color c)
        {
            r = c.r;
            g = c.g;
            b = c.b;
            a = c.a;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }
}
