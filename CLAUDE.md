# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TetoComposerTemp is a Unity project implementing a **mobile VFX compositor** - a node-based visual effects editor for creating textures, sprite sheets, and simple effects in real-time on mobile devices. Think of it as a **mobile version of Pixel Composer**.

**Unity Version**: Based on URP Blank Template (Unity 6000+)
**Target Platform**: Mobile-first (iOS/Android) with Android primary target configured with Vulkan/OpenGL ES3

### Project Vision

Enable users to create VFX textures, sprite sheets, and effects through an intuitive node-based interface on mobile devices, with the ability to export results as:
- MP4 video files
- PNG/JPEG images
- Sprite sheets

The system uses Unity's UI Toolkit for the editor interface and provides a graph-based architecture optimized for mobile performance.

## Architecture

### Core Node System (`Assets/Composer/01.Scripts/Core/`)

The node graph system is built around several key abstractions:

1. **Node** (Base class): All nodes inherit from this abstract class
   - Contains input/output slots for connections
   - Implements lazy evaluation with caching (nodes only execute when needed)
   - Each node has a unique GUID and position for the visual graph
   - Uses the Template Method pattern: subclasses override `InitializeSlots()` and `Execute()`

2. **NodeGraph**: Container for the entire node graph
   - Manages all nodes and connections
   - Handles connection validation and cycle detection
   - Implements topological sort for dependency-ordered execution
   - Connection rules: Output → Input only, matching data types required, one input per slot

3. **NodeExecutor**: Execution engine for the graph
   - Uses topological sort (DFS-based) to determine execution order
   - Prevents infinite loops via cycle detection
   - Supports executing specific nodes with their dependencies

4. **NodeSlot**: Represents input/output connection points
   - Enforces type safety (Texture, Float, Vector2, Vector3, Color)
   - Validates connection compatibility (types must match, no self-connections, opposite slot types)

5. **NodeConnection**: Represents edges between nodes
   - Color-coded by data type (Texture=purple, Float=green, Vector2/3=orange, Color=yellow)
   - Validates both endpoints exist

6. **OutputNode**: Special terminal node that receives final results
   - Can accept both Color and Texture inputs
   - Represents the "end" of the graph execution

### UI System (`Assets/Composer/05.UI/`)

Built with Unity UI Toolkit (not IMGUI or uGUI):

- **ComposerWindow**: MonoBehaviour that initializes the UI Document
  - Sets up UIDocument with PanelSettings
  - Creates NodeGraphView and test graph
  - Runtime window (not Editor-only)

- **NodeGraphView**: Main canvas for the node editor
  - Implements pan (middle mouse/alt+left) and zoom (mouse wheel)
  - Renders connections using Painter2D with Bezier curves
  - Manages NodeView instances for each node
  - Uses VisualElement layers: GridBackground → Connections → Nodes

- **NodeView**: Visual representation of individual nodes
  - Draggable with left mouse button
  - Displays node header with name
  - Shows input slots (left) and output slots (right)
  - Updates node position in data model during drag

- **GridBackground**: Custom grid rendering for editor background
  - Supports panning offset

### Data Flow

The system uses **pull-based evaluation**:
1. OutputNodes trigger execution (leaf nodes in the graph)
2. When a node executes, it calls `GetInputValue<T>()` for each input
3. This recursively executes connected upstream nodes
4. Results are cached in `cachedOutputs` dictionary to prevent redundant computation
5. `ResetExecution()` clears cache before each graph execution

### Folder Structure

```
Assets/Composer/
├── 01.Scripts/
│   ├── Core/           # Node graph data model and execution engine
│   └── Nodes/          # Concrete node implementations (currently empty)
├── 02.Shaders/         # Shader resources
├── 03.Materials/       # Material assets
├── 04.Scenes/          # Scene files
└── 05.UI/              # UI Toolkit views and USS styles
```

## Development Workflow

### Opening the Project

Unity projects use the Unity Editor. Open this folder in Unity 2023.1+ (URP template).

### Building

Since this is a Unity project:
- Build via Unity Editor: File → Build Settings → Build
- Platform can be configured in Build Settings (currently set up for Android + Standalone)

### Testing

- **Play Mode**: Press Play button in Unity Editor to test runtime behavior
- **Scene**: Test scenes would be in `Assets/Composer/04.Scenes/`
- Unity's Test Runner (if configured) for unit/playmode tests

### Creating New Nodes

To add a new node type:

1. Create a class in `Assets/Composer/01.Scripts/Nodes/` inheriting from `Node`
2. Override `InitializeSlots()` to define inputs/outputs using `AddInputSlot()` and `AddOutputSlot()`
3. Override `Execute()` to implement node logic
4. Use `GetInputValue<T>(slotId)` to retrieve input values from connected nodes
5. Use `SetOutputValue(slotId, value)` to set output values for downstream nodes

Example pattern:
```csharp
protected override void InitializeSlots()
{
    AddInputSlot("input_a", "Input A", DataType.Float);
    AddOutputSlot("result", "Result", DataType.Float);
}

public override void Execute()
{
    if (isExecuted) return;

    float inputA = GetInputValue<float>("input_a");
    float result = /* compute result */;

    SetOutputValue("result", result);
    isExecuted = true;
}
```

### UI Styling

- USS (Unity Style Sheets) files are in `Assets/Composer/05.UI/`
- Nodes use class names like `.node`, `.node__header`, `.slot__port`, `.slot__label`
- GridBackground has custom visual content generation

## Important Design Decisions

1. **Lazy Evaluation**: Nodes only execute when their outputs are requested, not when the graph structure changes
2. **Single Input Connection**: Each input slot can only have one connection (enforced in `ConnectSlots`)
3. **Type Safety**: Connections require matching DataType enums between slots
4. **Cycle Prevention**: Graph validates for cycles before execution using DFS-based cycle detection
5. **UI Toolkit**: Uses modern Unity UI Toolkit (VisualElement) instead of IMGUI or uGUI
6. **Runtime Editor**: The composer window is a MonoBehaviour, not an Editor extension (runs in play mode)

## Development Roadmap

### Phase 1: Core System (MVP)

**Goal**: Build the fundamental node graph engine with basic functionality

- **Node Graph Engine**:
  - Node creation, connection, deletion with mobile touch optimization
  - Input/Output slot data types (primarily Texture2D, Float, Vector)
  - Touch-based interactions (drag, pinch-to-zoom, pan)

- **Basic Generator Nodes**:
  - Noise (Perlin/FBM)
  - Constant Color
  - Gradient

- **Basic Transform Nodes**:
  - Blend (Multiply, Screen, Add, etc.)
  - Mask

- **Real-time Preview**:
  - Node outputs rendered to RenderTexture in real-time
  - Results displayed on canvas with live updates

### Phase 2: File I/O & Effect Expansion

**Goal**: Add save/export functionality and After Effects-inspired nodes

- **File I/O**:
  - PNG/JPEG export from final RenderTexture
  - Project save/load (node graph serialization via JSON/ScriptableObject)

- **Animation & Video Export**:
  - Frame Capture System linked to Time node
  - MP4 encoding from captured frames (using Unity Recorder API or FFmpeg wrapper)

- **Advanced Nodes**:
  - Displacement Map
  - Blur (Gaussian, Motion)
  - Color Correction (Levels, Curves)
  - Shape generators (Circle, Rectangle, SDF-based shapes)

### Phase 3: UX Polish & Deployment

**Goal**: Optimize user experience and prepare for App Store release

- **UI/UX Improvements**:
  - Node library search/filtering
  - Parameter controls optimized for mobile (sliders, input fields)
  - Touch gesture refinements

- **Tutorial & Examples**:
  - Built-in workflow examples for common VFX textures
  - Interactive tutorial system

- **App Store Deployment**:
  - iOS App Store and Google Play Store release
  - App optimization and testing on target devices

### Current Implementation Status

**✅ Completed**:
- Core node graph architecture (Node, NodeGraph, NodeExecutor, NodeSlot, NodeConnection)
- Lazy evaluation system with caching
- Topological sort-based execution engine
- Cycle detection
- Basic UI Toolkit setup (NodeGraphView, NodeView, GridBackground)
- Pan and zoom controls (middle mouse/alt+left, mouse wheel)
- Bezier curve connection rendering
- OutputNode implementation

**🚧 In Progress** (Phase 1 - MVP):
- Concrete node implementations (Nodes folder is currently empty)
- Touch-based controls for mobile
- Real-time RenderTexture rendering pipeline

**📋 Planned**:
- All Phase 2 and Phase 3 features
- Mobile-optimized UI/UX
- File I/O and export functionality
- Advanced node types

## Target Features (After Effects Inspired)

The following table maps common After Effects features to node-based implementations:

| Category | Node Types | AE Equivalent | Unity Implementation |
|----------|-----------|---------------|---------------------|
| **Generators** | Noise (Perlin, FBM, Voronoi) | Fractal Noise, Turbulent Displace | Fragment Shader with noise algorithms |
| | Shape (Circle, Rectangle, Gradient) | Shape Layer | Math formulas or SDF (Signed Distance Field) shaders |
| **Color** | Levels / Curves | Levels, Curves | Look-up Table (LUT) or Image Effect Shader |
| | Color Correction | Hue/Saturation, Color Balance | Post-processing shader |
| **Transform** | Blur (Gaussian, Motion) | Blur filters | Post-processing shader with depth/iteration control |
| | Displace | Displacement Map | Two-texture shader with offset calculation |
| **Animation** | Time / Loop Controller | Expression (time function) | Time variable node, frame saving logic |

## Performance Optimization Strategy

Since this is a mobile-first application, performance optimization is critical:

### 1. GPU-Based Computation

**Most Important**: All texture generation and transformation should use GPU, not CPU

- **Shader-Based Nodes**: All texture operations (Noise, Blur, Displace, Blend) should use Fragment Shaders or Compute Shaders to render directly to RenderTexture
  - This drastically reduces CPU overhead
  - Consider dynamic shader generation based on node connections (similar to ShaderLab)
  - Or use fixed shader functions in chain/composite pattern

- **RenderTexture Pooling**:
  - Implement RenderTexture pool to reuse textures and avoid allocation/deallocation overhead
  - Efficiently manage intermediate results between nodes

- **Resolution Management**:
  - **Proxy Preview**: Use low resolution (256x256 or 512x512) for real-time viewport preview
  - **High-Res Export**: Only render at high resolution (2048x2048) during final export
  - Separate preview and export pipelines

### 2. Memory Management

Mobile devices have limited RAM, making memory management crucial:

- **GC Minimization**:
  - Minimize object allocations during node connections and data transfers
  - Reuse struct types and pre-allocated Lists to reduce garbage collection pressure
  - Avoid creating new Vector/Color objects in loops

- **Lightweight Serialization**:
  - Use JSON or ScriptableObject for node graph save format
  - Keep file sizes small and loading times fast
  - Exclude unnecessary data during serialization

### 3. UI/UX Optimization

Mobile touch environment requires responsive and efficient interactions:

- **Graph Responsiveness**:
  - Optimize UI interactions (node drag, connection drawing) to avoid CPU overhead
  - Maintain stable FPS during graph manipulation
  - Use Dirty Flag pattern - only update graph UI when data changes

- **Touch Controls**:
  - Implement smooth pinch-to-zoom and pan gestures
  - Enable easy navigation of complex node graphs
  - Optimize touch input handling for mobile

### 4. Rendering Pipeline

- **Batch Processing**: When multiple nodes update, batch RenderTexture operations to minimize state changes
- **Async Operations**: Use async texture loading and saving to prevent UI freezes
- **Progressive Rendering**: For complex graphs, consider progressive rendering with visual feedback

## Namespace Convention

All core code uses the `VFXComposer.Core` namespace for the node system and `VFXComposer.UI` for UI components.
