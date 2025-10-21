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
}