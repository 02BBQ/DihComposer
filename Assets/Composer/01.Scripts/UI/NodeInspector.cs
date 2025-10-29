using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;
using VFXComposer.Core.Animation;
using System; // System.Action을 위해 추가

namespace VFXComposer.UI
{
    public class NodeInspector : VisualElement
    {
        private Node currentNode;
        private VisualElement propertiesContainer;
        private Label titleLabel;
        private NodeGraphView graphView;
        private TimelineController timelineController;
        private Image previewImage;

        public NodeInspector(NodeGraphView view, TimelineController timeline)
        {
            graphView = view;
            timelineController = timeline;

            // 전체 인스펙터 스타일 클래스
            AddToClassList("node-inspector");

            // Title Container: 모바일에서 상단 헤더처럼 보이도록 컨테이너 추가
            var titleContainer = new VisualElement();
            titleContainer.AddToClassList("inspector__header");
            Add(titleContainer);

            // Title
            titleLabel = new Label("Inspector");
            titleLabel.AddToClassList("inspector__title");
            titleContainer.Add(titleLabel);

            // ScrollView to handle overflow
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.AddToClassList("inspector__scroll-view");
            scrollView.style.flexGrow = 1;
            Add(scrollView);

            // Properties container inside ScrollView
            propertiesContainer = new VisualElement();
            propertiesContainer.AddToClassList("inspector__properties-container");
            scrollView.Add(propertiesContainer);

            // Schedule preview updates
            schedule.Execute(UpdatePreview).Every(100);

            // 타임라인 시간 변경 시 Inspector UI 업데이트
            if (timelineController != null)
            {
                timelineController.OnTimeChanged += OnTimelineChanged;
            }
        }

        private void OnTimelineChanged(float time)
        {
            // 현재 선택된 노드가 있으면 UI 업데이트
            if (currentNode != null)
            {
                // Inspector 값 갱신 (애니메이션 값 반영)
                ShowNodeProperties(currentNode);
            }
        }

        public void ShowNodeProperties(Node node)
        {
            currentNode = node;
            propertiesContainer.Clear();
            previewImage = null;

            if (node == null)
            {
                titleLabel.text = "Inspector";
                var emptyLabel = new Label("No node selected");
                emptyLabel.AddToClassList("inspector__empty");
                propertiesContainer.Add(emptyLabel);
                return;
            }

            titleLabel.text = node.nodeName;

            // Add preview image
            previewImage = new Image();
            previewImage.AddToClassList("inspector__preview");
            previewImage.scaleMode = ScaleMode.ScaleToFit;
            propertiesContainer.Add(previewImage);

            var builder = new InspectorBuilder(propertiesContainer, node, () => ExecuteNode(node), timelineController);
            builder.Build();

            // OutputNode의 경우 추가 정보 표시
            if (node is OutputNode)
            {
                var infoLabel = new Label("This is the final output node.\nConnect texture or color inputs.");
                infoLabel.AddToClassList("inspector__info");
                propertiesContainer.Add(infoLabel);
            }

            // 공통 버튼 (예: 노드 삭제) 추가 - OutputNode는 제외
            if (!(node is OutputNode))
            {
                AddDeleteButton(node);
            }
        }
        
        private void UpdatePreview()
        {
            if (currentNode == null || previewImage == null) return;

            if (currentNode.cachedOutputs.Count > 0)
            {
                foreach (var output in currentNode.cachedOutputs.Values)
                {
                    if (output is RenderTexture rt)
                    {
                        previewImage.image = rt;
                        return;
                    }
                    else if (output is Color col)
                    {
                        var tex = new Texture2D(1, 1);
                        tex.SetPixel(0, 0, col);
                        tex.Apply();
                        previewImage.image = tex;
                        return;
                    }
                }
            }
        }

        private void AddDeleteButton(Node node)
        {
            var deleteButton = new Button(() =>
            {
                if (graphView != null && node != null)
                {
                    graphView.DeleteNode(node);
                }
            });
            deleteButton.text = "🗑️ Delete Node";
            deleteButton.AddToClassList("inspector__delete-button");
            propertiesContainer.Add(deleteButton);
        }


        // 기존 ExecuteNode 메서드는 그대로 유지
        private void ExecuteNode(Node node)
        {
            if (graphView == null || node == null) return;

            var graph = graphView.GetGraph();
            if (graph == null) return;

            node.ResetExecution();
            var executor = new NodeExecutor(graph);
            executor.Execute();
        }
    }
}