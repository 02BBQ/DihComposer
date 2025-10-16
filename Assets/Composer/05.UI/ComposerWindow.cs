using UnityEngine;
using UnityEngine.UIElements;
using VFXComposer.Core;
using VFXComposer.UI;

public class ComposerWindow : MonoBehaviour
{
    [SerializeField] private StyleSheet styleSheet;
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
        
        graphView = new NodeGraphView();
        graphView.style.flexGrow = 1;
        rootVisualElement.Add(graphView);
    }
    
    void CreateTestGraph()
    {
        graph = new NodeGraph();
        
        // var colorNode1 = new ConstantColorNode();
        // colorNode1.SetColor(Color.red);
        // colorNode1.position = new Vector2(100, 100);
        
        // var colorNode2 = new ConstantColorNode();
        // colorNode2.SetColor(Color.blue);
        // colorNode2.position = new Vector2(100, 300);
        
        // var blendNode = new BlendNode();
        // blendNode.position = new Vector2(400, 200);
        
        var outputNode = new OutputNode();
        outputNode.position = new Vector2(700, 200);
        
        // graph.AddNode(colorNode1);
        // graph.AddNode(colorNode2);
        // graph.AddNode(blendNode);
        graph.AddNode(outputNode);
        
        // graph.ConnectSlots(colorNode1.outputSlots[0], blendNode.inputSlots[0]);
        // graph.ConnectSlots(colorNode2.outputSlots[0], blendNode.inputSlots[1]);
        // graph.ConnectSlots(blendNode.outputSlots[0], outputNode.inputSlots[0]);
        
        graphView.SetGraph(graph);
    }
}