using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;
using VFXComposer.UI;

public class ComposerWindow : MonoBehaviour
{
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private StyleSheet inspectorStyleSheet;
    [SerializeField] private PanelSettings panelSettings;
    
    private UIDocument uiDocument;
    private NodeGraphView graphView;
    private NodeGraph graph;

    // Toolbar buttons
    private Button undoButton;
    private Button redoButton;
    
    void Start()
    {
        SetupUI();
        CreateTestGraph();
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

        // Toolbar at the top
        var toolbar = CreateToolbar();
        rootVisualElement.Add(toolbar);

        var horizontalContainer = new VisualElement();
        horizontalContainer.style.flexDirection = FlexDirection.Row;
        horizontalContainer.style.flexGrow = 1;
        rootVisualElement.Add(horizontalContainer);

        graphView = new NodeGraphView();
        graphView.style.flexGrow = 1;
        horizontalContainer.Add(graphView);

        var inspector = new NodeInspector(graphView);
        horizontalContainer.Add(inspector);

        graphView.SetInspector(inspector);

        // Subscribe to command history changes to update button states
        graphView.OnCommandHistoryChanged += UpdateToolbarButtons;
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
        toolbar.AddToClassList("toolbar");
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        toolbar.style.paddingLeft = 8;
        toolbar.style.paddingRight = 8;
        toolbar.style.paddingTop = 4;
        toolbar.style.paddingBottom = 4;
        toolbar.style.borderBottomWidth = 2;
        toolbar.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);

        // Undo button
        undoButton = new Button(() => graphView?.Undo());
        undoButton.text = "↶ Undo";
        undoButton.AddToClassList("toolbar-button");
        undoButton.style.marginRight = 4;
        undoButton.style.paddingLeft = 12;
        undoButton.style.paddingRight = 12;
        undoButton.style.paddingTop = 6;
        undoButton.style.paddingBottom = 6;
        toolbar.Add(undoButton);

        // Redo button
        redoButton = new Button(() => graphView?.Redo());
        redoButton.text = "↷ Redo";
        redoButton.AddToClassList("toolbar-button");
        redoButton.style.marginRight = 16;
        redoButton.style.paddingLeft = 12;
        redoButton.style.paddingRight = 12;
        redoButton.style.paddingTop = 6;
        redoButton.style.paddingBottom = 6;
        toolbar.Add(redoButton);

        // Info label
        var infoLabel = new Label("VFX Composer - Mobile Edition");
        infoLabel.style.flexGrow = 1;
        infoLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        infoLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
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
}