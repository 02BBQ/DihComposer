/*
 * ProjectManager.cs
 *
 * VFX Composer 프로젝트 파일(.vfxc)의 저장 및 불러오기를 담당하는 매니저 클래스
 *
 * 주요 기능:
 * 1. SaveProject(): NodeGraph + TimelineController를 JSON으로 직렬화하여 파일로 저장
 * 2. LoadProject(): .vfxc 파일을 읽어 ProjectData로 역직렬화
 * 3. DeserializeGraph(): GraphDataInfo를 NodeGraph 객체로 복원
 * 4. DeserializeTimeline(): TimelineDataInfo를 TimelineController로 복원
 *
 * 저장 위치:
 * - Application.persistentDataPath (모바일/PC 공통)
 * - Android: /Android/data/com.yourapp/files/
 * - iOS: /Documents/
 *
 * 파일 포맷: JSON (Unity JsonUtility 사용)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using VFXComposer.Core.Animation;
using VFXComposer.Core.Serialization;
using VFXComposer.Factory;

namespace VFXComposer.Core
{
    public static class ProjectManager
    {
        public static string SaveProject(string fileName, NodeGraph graph, TimelineController timeline)
        {
            try
            {
                var projectData = new ProjectData();

                projectData.metadata = new MetadataInfo
                {
                    version = "1.0",
                    createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    modifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    appVersion = Application.version
                };

                projectData.graphData = SerializeGraph(graph);
                projectData.timelineData = SerializeTimeline(timeline);

                string json = JsonUtility.ToJson(projectData, true);

                if (!fileName.EndsWith(".vfxc"))
                    fileName += ".vfxc";

                string filePath = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllText(filePath, json);

                Debug.Log($"[ProjectManager] Project saved: {filePath}");
                return filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectManager] Save failed: {e.Message}");
                throw;
            }
        }

        public static ProjectData LoadProject(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[ProjectManager] File not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                ProjectData projectData = JsonUtility.FromJson<ProjectData>(json);

                Debug.Log($"[ProjectManager] Project loaded: {filePath}");
                return projectData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectManager] Load failed: {e.Message}");
                throw;
            }
        }

        private static GraphDataInfo SerializeGraph(NodeGraph graph)
        {
            var graphData = new GraphDataInfo
            {
                graphName = graph.graphName
            };

            foreach (var node in graph.nodes)
            {
                var nodeData = new NodeDataInfo
                {
                    id = node.id,
                    nodeType = node.GetType().Name,
                    nodeName = node.nodeName,
                    position = new Vector2Data(node.position)
                };

                SerializeNodeProperties(node, nodeData);
                graphData.nodes.Add(nodeData);
            }

            foreach (var connection in graph.connections)
            {
                var connData = new ConnectionDataInfo
                {
                    outputNodeId = connection.outputSlot.owner.id,
                    outputSlotId = connection.outputSlot.id,
                    inputNodeId = connection.inputSlot.owner.id,
                    inputSlotId = connection.inputSlot.id
                };
                graphData.connections.Add(connData);
            }

            return graphData;
        }

        private static void SerializeNodeProperties(Node node, NodeDataInfo nodeData)
        {
            var fields = node.GetType().GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.Name == "id" || field.Name == "nodeName" || field.Name == "position")
                    continue;

                if (field.Name == "inputSlots" || field.Name == "outputSlots" || field.Name == "cachedOutputs")
                    continue;

                object value = field.GetValue(node);
                string valueJson = SerializationHelper.SerializeValue(value, out string typeName);

                nodeData.properties.Add(new PropertyData
                {
                    key = field.Name,
                    valueJson = valueJson,
                    valueType = typeName
                });
            }
        }

        private static TimelineDataInfo SerializeTimeline(TimelineController timeline)
        {
            var timelineData = new TimelineDataInfo
            {
                fps = timeline.fps,
                duration = timeline.duration,
                currentTime = timeline.currentTime
            };

            var animatedProps = timeline.GetAllAnimatedProperties();
            foreach (var kvp in animatedProps)
            {
                var animPropInfo = new AnimatedPropertyInfo
                {
                    propertyKey = kvp.Key,
                    dataType = kvp.Value.dataType.ToString(),
                    interpolationType = kvp.Value.interpolationType.ToString()
                };

                foreach (var keyframe in kvp.Value.keyframes)
                {
                    string valueJson = SerializationHelper.SerializeValue(keyframe.value, out _);
                    animPropInfo.keyframes.Add(new KeyframeInfo
                    {
                        time = keyframe.time,
                        valueJson = valueJson
                    });
                }

                timelineData.animatedProperties.Add(animPropInfo);
            }

            return timelineData;
        }

        public static NodeGraph DeserializeGraph(GraphDataInfo graphData)
        {
            var graph = new NodeGraph
            {
                graphName = graphData.graphName
            };

            var nodeMap = new Dictionary<string, Node>();

            foreach (var nodeData in graphData.nodes)
            {
                Node node = NodeFactory.CreateNode(nodeData.nodeType);
                if (node == null)
                {
                    Debug.LogWarning($"[ProjectManager] Unknown node type: {nodeData.nodeType}");
                    continue;
                }

                node.id = nodeData.id;
                node.nodeName = nodeData.nodeName;
                node.position = nodeData.position.ToVector2();

                DeserializeNodeProperties(node, nodeData);

                graph.AddNode(node);
                nodeMap[node.id] = node;
            }

            foreach (var connData in graphData.connections)
            {
                if (!nodeMap.TryGetValue(connData.outputNodeId, out Node outputNode))
                {
                    Debug.LogWarning($"[ProjectManager] Output node not found: {connData.outputNodeId}");
                    continue;
                }

                if (!nodeMap.TryGetValue(connData.inputNodeId, out Node inputNode))
                {
                    Debug.LogWarning($"[ProjectManager] Input node not found: {connData.inputNodeId}");
                    continue;
                }

                var outputSlot = outputNode.outputSlots.Find(s => s.id == connData.outputSlotId);
                var inputSlot = inputNode.inputSlots.Find(s => s.id == connData.inputSlotId);

                if (outputSlot != null && inputSlot != null)
                {
                    graph.ConnectSlots(outputSlot, inputSlot);
                }
            }

            return graph;
        }

        private static void DeserializeNodeProperties(Node node, NodeDataInfo nodeData)
        {
            var fields = node.GetType().GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            foreach (var propData in nodeData.properties)
            {
                var field = fields.FirstOrDefault(f => f.Name == propData.key);
                if (field != null)
                {
                    object value = SerializationHelper.DeserializeValue(propData.valueJson, propData.valueType);
                    if (value != null)
                    {
                        field.SetValue(node, value);
                    }
                }
            }
        }

        public static void RestoreTimeline(TimelineController timeline, TimelineDataInfo timelineData)
        {
            timeline.fps = timelineData.fps;
            timeline.duration = timelineData.duration;
            timeline.SetTime(timelineData.currentTime);

            foreach (var animPropInfo in timelineData.animatedProperties)
            {
                if (!Enum.TryParse(animPropInfo.dataType, out Animation.DataType dataType))
                {
                    Debug.LogWarning($"[ProjectManager] Unknown DataType: {animPropInfo.dataType}");
                    continue;
                }

                if (!Enum.TryParse(animPropInfo.interpolationType, out Animation.InterpolationType interpType))
                {
                    interpType = Animation.InterpolationType.Linear;
                }

                foreach (var kfInfo in animPropInfo.keyframes)
                {
                    object value = SerializationHelper.DeserializeValue(kfInfo.valueJson, animPropInfo.dataType);
                    if (value != null)
                    {
                        timeline.AddKeyframe(animPropInfo.propertyKey, kfInfo.time, value, dataType, interpType);
                    }
                }
            }
        }
    }
}
