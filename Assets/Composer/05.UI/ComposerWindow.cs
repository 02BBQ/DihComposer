using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;
using VFXComposer.Core.Animation;
using VFXComposer.UI;
using System.IO;

public class ComposerWindow : MonoBehaviour
{
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private StyleSheet inspectorStyleSheet;
    [SerializeField] private PanelSettings panelSettings;

    private UIDocument uiDocument;
    private NodeGraphView graphView;
    private NodeGraph graph;
    private TimelineController timelineController;
    private TimelineView timelineView;

    private Button undoButton;
    private Button redoButton;
    private string currentProjectPath = "";
    
    void Start()
    {
        SetupUI();
        CreateTestGraph();
    }

    void Update()
    {
        // 타임라인 업데이트 (재생 중일 때)
        if (timelineController != null)
        {
            timelineController.Update();
        }
    }

    void SetupUI()
    {
        uiDocument = gameObject.AddComponent<UIDocument>();

        if (panelSettings != null)
        {
            uiDocument.panelSettings = panelSettings;
        }

        var rootVisualElement = uiDocument.rootVisualElement;
        rootVisualElement.style.width = Length.Percent(100);
        rootVisualElement.style.height = Length.Percent(100);

        if (styleSheet != null)
        {
            rootVisualElement.styleSheets.Add(styleSheet);
        }

        if (inspectorStyleSheet != null)
        {
            rootVisualElement.styleSheets.Add(inspectorStyleSheet);
        }

        // Main container (vertical layout)
        var mainContainer = new VisualElement();
        mainContainer.style.flexDirection = FlexDirection.Column;
        mainContainer.style.width = Length.Percent(100);
        mainContainer.style.height = Length.Percent(100);
        rootVisualElement.Add(mainContainer);

        // Middle section: Graph + Inspector
        var horizontalContainer = new VisualElement();
        horizontalContainer.style.flexDirection = FlexDirection.Row;
        horizontalContainer.style.flexGrow = 1;
        mainContainer.Add(horizontalContainer);

        // Timeline controller (먼저 생성)
        timelineController = new TimelineController();

        graphView = new NodeGraphView();
        graphView.style.flexGrow = 1;
        horizontalContainer.Add(graphView);

        var inspector = new NodeInspector(graphView, timelineController);
        horizontalContainer.Add(inspector);

        graphView.SetInspector(inspector);

        // Toolbar at the top
        var toolbar = CreateToolbar();
        mainContainer.Add(toolbar);
        
        // Timeline view at the bottom
        timelineView = new TimelineView(timelineController);
        mainContainer.Add(timelineView);

        // NodeCreationMenu를 최상단 레이어에 추가 (타임라인 위로 오도록)
        mainContainer.Add(graphView.CreationMenu);

        // Subscribe to command history changes to update button states
        graphView.OnCommandHistoryChanged += UpdateToolbarButtons;

        // Timeline 시간 변경 시 애니메이션 값 적용
        timelineController.OnTimeChanged += (time) =>
        {
            ApplyAnimationToNodes();
        };
    }
    
    void CreateTestGraph()
    {
        graph = new NodeGraph();
        graph.graphName = "New VFX Graph";

        var outputNode = new OutputNode();
        outputNode.position = new Vector2(700, 200);
        
        graph.AddNode(outputNode);
        graphView.SetGraph(graph);
    }

    VisualElement CreateToolbar()
    {
        var toolbar = new VisualElement();
        toolbar.AddToClassList("main-toolbar");

        var saveButton = new Button(OnSaveProject);
        saveButton.text = "💾 Save";
        saveButton.AddToClassList("toolbar-button");
        saveButton.AddToClassList("toolbar-button--save");
        toolbar.Add(saveButton);

        var loadButton = new Button(OnLoadProject);
        loadButton.text = "📂 Load";
        loadButton.AddToClassList("toolbar-button");
        loadButton.AddToClassList("toolbar-button--load");
        toolbar.Add(loadButton);

        var addNodeButton = new Button(() =>
        {
            if (graphView != null)
            {
                graphView.ShowNodeCreationMenu();
            }
        });
        addNodeButton.text = "+ Node";
        addNodeButton.AddToClassList("toolbar-button");
        addNodeButton.AddToClassList("toolbar-button--add-node");
        toolbar.Add(addNodeButton);

        undoButton = new Button(() => graphView?.Undo());
        undoButton.text = "↶ Undo";
        undoButton.AddToClassList("toolbar-button");
        undoButton.AddToClassList("toolbar-button--undo");
        toolbar.Add(undoButton);

        redoButton = new Button(() => graphView?.Redo());
        redoButton.text = "↷ Redo";
        redoButton.AddToClassList("toolbar-button");
        redoButton.AddToClassList("toolbar-button--redo");
        toolbar.Add(redoButton);

        var infoLabel = new Label("VFX Composer - Mobile Edition");
        infoLabel.AddToClassList("toolbar-info");
        toolbar.Add(infoLabel);

        UpdateToolbarButtons();

        return toolbar;
    }

    void UpdateToolbarButtons()
    {
        if (graphView == null) return;

        // Update button enabled state
        if (undoButton != null)
        {
            undoButton.SetEnabled(graphView.CanUndo);
        }

        if (redoButton != null)
        {
            redoButton.SetEnabled(graphView.CanRedo);
        }
    }

    /// <summary>
    /// 애니메이션 값을 모든 노드에 적용
    /// </summary>
    void ApplyAnimationToNodes()
    {
        if (graph == null || timelineController == null) return;

        // 모든 애니메이션된 프로퍼티 가져오기
        var animatedProps = timelineController.GetAllAnimatedProperties();

        bool hasAnimations = animatedProps.Count > 0;

        foreach (var kvp in animatedProps)
        {
            string propertyKey = kvp.Key;
            AnimatedProperty animProp = kvp.Value;

            // propertyKey 형식: "nodeId.propertyName"
            string[] parts = propertyKey.Split('.');
            if (parts.Length != 2) continue;

            string nodeId = parts[0];
            string propertyName = parts[1];

            // 노드 찾기
            Node targetNode = graph.nodes.Find(n => n.id == nodeId);
            if (targetNode == null)
            {
                Debug.LogWarning($"Node not found: {nodeId}");
                continue;
            }

            // 현재 시간의 애니메이션 값 계산
            object animatedValue = animProp.Evaluate(timelineController.currentTime);
            if (animatedValue == null) continue;

            // Reflection으로 노드 필드에 값 적용
            var field = targetNode.GetType().GetField(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                object oldValue = field.GetValue(targetNode);
                field.SetValue(targetNode, animatedValue);
                Debug.Log($"[Animation] {targetNode.nodeName}.{propertyName}: {oldValue} -> {animatedValue} (time: {timelineController.currentTime:F2}s)");
            }
            else
            {
                Debug.LogWarning($"Field not found: {targetNode.nodeName}.{propertyName}");
            }
        }

        // 애니메이션이 있을 때만 그래프 재실행
        if (hasAnimations)
        {
            var executor = new NodeExecutor(graph);
            executor.Execute();
        }
    }

    void OnSaveProject()
    {
        if (graph == null || timelineController == null)
        {
            Debug.LogWarning("[ComposerWindow] Cannot save: graph or timeline is null");
            return;
        }

        try
        {
            string fileName = string.IsNullOrEmpty(currentProjectPath)
                ? $"VFXProject_{System.DateTime.Now:yyyyMMdd_HHmmss}.vfxc"
                : Path.GetFileName(currentProjectPath);

            string savedPath = ProjectManager.SaveProject(fileName, graph, timelineController);
            currentProjectPath = savedPath;

            Debug.Log($"[ComposerWindow] Project saved successfully: {savedPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ComposerWindow] Save failed: {e.Message}");
        }
    }

    void OnLoadProject()
    {
        try
        {
            string persistentPath = Application.persistentDataPath;
            string[] files = Directory.GetFiles(persistentPath, "*.vfxc");

            if (files.Length == 0)
            {
                Debug.LogWarning("[ComposerWindow] No .vfxc files found");
                return;
            }

            string filePath = files[files.Length - 1];

            var projectData = ProjectManager.LoadProject(filePath);
            if (projectData == null)
            {
                Debug.LogError("[ComposerWindow] Failed to load project");
                return;
            }

            graph = ProjectManager.DeserializeGraph(projectData.graphData);
            graphView.SetGraph(graph);

            ProjectManager.RestoreTimeline(timelineController, projectData.timelineData);

            currentProjectPath = filePath;

            Debug.Log($"[ComposerWindow] Project loaded successfully: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ComposerWindow] Load failed: {e.Message}");
        }
    }
}