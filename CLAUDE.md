# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 开发原则

**代码质量要求**：
- 始终注重**高性能**：优先考虑性能优化，使用对象池、缓存、异步加载等技术
- 确保**可维护性**：编写清晰的代码结构，遵循SOLID原则
- 保证**可扩展性**：设计模块化架构，支持功能扩展
- 提高**可读性**：编写自解释的代码，使用有意义的命名
- 增强**易用性**：提供简洁的API接口，降低使用门槛

**技术规范**：
- 遵循 Unity 2022.3 的最佳实践
- 严格遵守 C# 编程规范
- 协程使用 UniTask 代替 Unity 协程，提高异步性能
- 慎用 try-catch，优先使用错误预防而非异常处理
- 允许破坏性更新，不保持向后兼容性

**文档和沟通**：
- 始终编写中文注释
- 始终使用中文回复

## Project Overview

Simple Toolkits is a comprehensive Unity game development framework that provides modular, reusable components for common game development tasks. Built for Unity 2022.3.62f1, it offers a collection of "kits" that can be used independently or together.

## Key Architecture

### Core Framework Structure
- **Modular Design**: Each functionality is organized into separate "kits" (AIKit, AudioKit, DataStorageKit, etc.)
- **Manager Pattern**: Uses `GKMgr.cs` as a central manager for accessing different kit managers
- **Configuration System**: Excel-to-JSON configuration pipeline with automatic C# class generation
- **Resource Management**: Integrated with YooAsset for asset bundling and loading
- **Singleton Pattern**: Comprehensive singleton system for both MonoBehaviour and regular classes

### Kit Components
- **AIKit**: Finite State Machine (FSM) and GOAP systems
- **ActivityQueueKit**: Activity queue management for sequential task execution
- **AudioKit**: Audio management and playback
- **ConsoleKit**: In-game console system for debugging
- **DataStorageKit**: Multi-backend data storage (PlayerPrefs, JSON files)
- **ExcelKit**: Configuration management with Excel-to-JSON pipeline
- **GameKit**: Core game management systems
- **LocaleKit**: Multi-language support system
- **PathfindingKit**: A* pathfinding implementation
- **PoolKit**: Object pooling for performance optimization
- **ResKit**: Resource loading and management with YooAsset integration
- **SceneKit**: Scene management utilities
- **UIPanelKit**: UI panel management system
- **WebKit**: Web request handling

## Development Commands

### Excel Configuration Generation
```bash
# Generate C# classes and JSON configs from Excel files
# Use Unity Editor menu: Simple Toolkits/Excel To Json
# Or watch for automatic generation via ExcelWatcher
```

### Building and Testing
```bash
# Open Unity Editor
unity.exe -projectPath "D:\UnityProjects\SimpleToolkits"

# Build project (via Unity Editor Build Settings)
# No command-line build scripts configured - use Unity's built-in build system
```

### Package Management
```bash
# NuGet packages are managed via Unity's NuGet integration
# Packages are located in Packages/ directory
# Unity packages managed via Unity Package Manager
```

## Configuration System

### Excel-to-JSON Pipeline
1. **Excel Files**: Place in `Assets/ExcelConfigs/`
2. **Structure**: Row 1 (comments), Row 2 (field names), Row 3 (types), Row 4+ (data)
3. **Auto-generation**: Uses `ExcelWatcher.cs` to monitor changes and regenerate
4. **Output**: C# classes in `Assets/Scripts/Configs/`, JSON in `Assets/GameRes/JsonConfigs/`

### Settings Management
- **SimpleToolkitsSettings**: Central configuration asset in `Assets/Resources/`
- **YooAsset Integration**: Configurable play modes and package management
- **Multi-language Support**: Built-in localization system

## Key Development Patterns

### Manager Access Pattern
```csharp
// Access any kit manager through GKMgr
var audioManager = GKMgr.Instance.GetObject<AudioKit>();
var configManager = GKMgr.Instance.GetObject<ConfigManager>();
```

### Configuration Usage
```csharp
// Get configuration data
var config = ConfigManager.Instance.Get<ExampleConfig>("id");
var allConfigs = ConfigManager.Instance.GetAll<ExampleConfig>();
```

### Singleton Pattern
```csharp
// MonoBehaviour singleton
public class MyManager : MonoSingleton<MyManager>

// Regular class singleton  
public class MyService : ISingleton, SingletonCreator<MyService>
```

## Important File Locations

- **Core Settings**: `Assets/Resources/SimpleToolkitsSettings.asset`
- **Excel Configs**: `Assets/ExcelConfigs/`
- **Generated Classes**: `Assets/Scripts/Configs/`
- **Generated JSON**: `Assets/GameRes/JsonConfigs/`
- **Runtime Source**: `Assets/SimpleToolkits/Runtime/`
- **Editor Tools**: `Assets/SimpleToolkits/Editor/`

## UI Scroll View Implementation

### LoopScrollRect Package
项目使用第三方包 `me.qiankanglai.loopscrollrect` 来实现高性能的无限滚动列表。

### 核心特性
- **虚拟化滚动**: 仅渲染可见区域的项目，大幅提升性能
- **无限滚动**: 支持无限长度的数据列表
- **多方向支持**: 支持垂直和水平滚动
- **多列/多行**: 支持网格布局的多列或多行显示
- **动态尺寸**: 支持不同尺寸的列表项

### 使用模式
```csharp
// 垂直滚动列表
public class VerticalScrollExample : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
{
    public LoopVerticalScrollRect scrollRect;
    public GameObject itemPrefab;
    
    void Start()
    {
        scrollRect.prefabSource = this;
        scrollRect.dataSource = this;
        scrollRect.totalCount = dataList.Count;
        scrollRect.RefillCells();
    }
    
    // 实现 LoopScrollPrefabSource 接口
    public GameObject GetObject(int index)
    {
        return Instantiate(itemPrefab);
    }
    
    public void ReturnObject(Transform trans)
    {
        Destroy(trans.gameObject);
    }
    
    // 实现 LoopScrollDataSource 接口
    public void ProvideData(Transform transform, int idx)
    {
        // 绑定数据到UI组件
        var item = transform.GetComponent<ItemComponent>();
        item.SetData(dataList[idx]);
    }
}
```

## Dependencies

- **Unity 2022.3.62f1** or compatible
- **YooAsset**: Asset management and bundling
- **UniTask**: Async operations
- **PrimeTween**: Animation system
- **LoopScrollRect**: High-performance infinite scrolling lists
- **NPOI**: Excel processing (via NuGet)
- **Newtonsoft.Json**: JSON serialization
- **TextMeshPro**: Text rendering

## Build Considerations

- Uses URP (Universal Render Pipeline)
- Asset bundling via YooAsset
- Platform-specific builds configured through Unity Editor
- No automated build scripts - use Unity's build system
- Supports multiple languages with resource-based localization