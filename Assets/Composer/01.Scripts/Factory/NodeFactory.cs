/*
 * NodeFactory.cs
 *
 * 노드 타입 문자열을 받아 해당하는 Node 인스턴스를 생성하는 팩토리 클래스
 *
 * 사용 패턴:
 * - 파일에서 로드한 nodeType 문자열(예: "OutputNode", "NoiseNode")을 받아
 * - switch 문으로 분기하여 적절한 Node 서브클래스의 인스턴스를 생성
 *
 * 새로운 노드 타입 추가 시:
 * 1. Node를 상속받는 새 클래스 생성 (예: MyCustomNode)
 * 2. 이 파일의 switch문에 case "MyCustomNode": return new MyCustomNode(); 추가
 *
 * Reflection을 사용할 수도 있지만 모바일 성능을 위해 명시적 switch 사용
 */

using UnityEngine;
using VFXComposer.Core;

namespace VFXComposer.Factory
{
    public static class NodeFactory
    {
        public static Node CreateNode(string nodeType)
        {
            switch (nodeType)
            {
                case "OutputNode":
                    return new OutputNode();

                default:
                    Debug.LogWarning($"[NodeFactory] Unknown node type: {nodeType}");
                    return null;
            }
        }
    }
}
