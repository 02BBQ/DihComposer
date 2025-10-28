/*
 * NodeFactory.cs
 *
 * 노드 타입 문자열을 받아 해당하는 Node 인스턴스를 생성하는 팩토리 클래스
 *
 * 사용 패턴:
 * - 파일에서 로드한 nodeType 문자열(예: "OutputNode", "NoiseNode")을 받아
 * - Reflection을 통해 자동으로 해당 타입을 찾아 인스턴스 생성
 *
 * 새로운 노드 타입 추가 시:
 * 1. Node를 상속받는 새 클래스 생성 (예: MyCustomNode)
 * 2. 끝! (자동으로 인식됨)
 *
 * 성능 최적화:
 * - 타입 캐싱으로 Reflection 오버헤드 최소화
 * - 첫 호출 시에만 Assembly 스캔, 이후 Dictionary에서 조회
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VFXComposer.Core;

namespace VFXComposer.Factory
{
    public static class NodeFactory
    {
        // 타입 캐시 (성능 최적화)
        private static Dictionary<string, Type> nodeTypeCache;

        /// <summary>
        /// 노드 타입 문자열로 Node 인스턴스 생성
        /// </summary>
        public static Node CreateNode(string nodeType)
        {
            // 캐시 초기화 (첫 호출 시에만)
            if (nodeTypeCache == null)
            {
                InitializeTypeCache();
            }

            // 캐시에서 타입 조회
            if (nodeTypeCache.TryGetValue(nodeType, out Type type))
            {
                try
                {
                    // 인스턴스 생성
                    Node node = Activator.CreateInstance(type) as Node;
                    return node;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NodeFactory] Failed to create node '{nodeType}': {e.Message}");
                    return null;
                }
            }

            Debug.LogWarning($"[NodeFactory] Unknown node type: {nodeType}");
            return null;
        }

        /// <summary>
        /// Assembly에서 Node를 상속받은 모든 타입을 찾아 캐시에 저장
        /// </summary>
        private static void InitializeTypeCache()
        {
            nodeTypeCache = new Dictionary<string, Type>();

            // 현재 Assembly에서 Node를 상속받은 모든 타입 찾기
            var nodeBaseType = typeof(Node);
            var assembly = Assembly.GetExecutingAssembly();

            var nodeTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && nodeBaseType.IsAssignableFrom(t));

            foreach (var type in nodeTypes)
            {
                nodeTypeCache[type.Name] = type;
            }

            Debug.Log($"[NodeFactory] Initialized with {nodeTypeCache.Count} node types: {string.Join(", ", nodeTypeCache.Keys)}");
        }

        /// <summary>
        /// 등록된 모든 노드 타입 이름 반환 (디버깅/UI용)
        /// </summary>
        public static string[] GetAllNodeTypes()
        {
            if (nodeTypeCache == null)
            {
                InitializeTypeCache();
            }

            return nodeTypeCache.Keys.ToArray();
        }
    }
}
