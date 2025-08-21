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
- **AudioKit**: Audio management and playback
- **DataStorageKit**: Multi-backend data storage (PlayerPrefs, JSON files)
- **ExcelKit**: Configuration management with Excel-to-JSON pipeline
- **GameKit**: Core game management systems
- **LocaleKit**: Multi-language support system
- **PathfindingKit**: A* pathfinding implementation
- **PoolKit**: Object pooling for performance optimization
- **ResKit**: Resource loading and management with YooAsset integration
- **ScrollViewKit**: High-performance scroll view implementation with AutoSizeProvider system
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

## ScrollViewKit 详细说明

### 架构特点

ScrollViewKit 是一个高性能的滚动视图系统，支持动态尺寸计算和虚拟化滚动。

### 核心接口

**统一的变尺寸适配器接口**：
```csharp
public interface IVariableSizeAdapter
{
    Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout);
}
```

### StandardVariableSizeAdapter 系统

- **StandardVariableSizeAdapter**: 增强的标准变尺寸适配器，合并了原 AutoSizeProvider 功能
- **BaseVariableSizeAdapter**: 基础变尺寸适配器，提供核心功能和智能尺寸计算
- **特点**: 高性能、智能布局感知、支持固定和自适应尺寸参数、适用于各种布局场景

### 智能尺寸计算

适配器根据 IScrollLayout 布局模式自动优化尺寸计算：

- **纵向布局**：通常固定宽度，高度自适应
- **横向布局**：通常固定高度，宽度自适应  
- **网格布局**：支持固定宽高或按约束计算

### 使用模式

```csharp
// 1. 纵向布局：固定宽度，自适应高度
var adapter = StandardVariableSizeAdapter.CreateForVertical(
    prefab: messageTemplate,
    countGetter: () => messages.Count,
    dataGetter: index => messages[index],
    binder: messageBinder,
    templateBinder: (rt, data) => {
        // 绑定数据到模板
    },
    fixedWidth: 300f,
    minHeight: 60f,
    maxHeight: 300f
);

// 2. 横向布局：固定高度，自适应宽度
var adapter = StandardVariableSizeAdapter.CreateForHorizontal(
    prefab: messageTemplate,
    countGetter: () => messages.Count,
    dataGetter: index => messages[index],
    binder: messageBinder,
    templateBinder: (rt, data) => {
        // 绑定数据到模板
    },
    fixedHeight: 300f,
    minWidth: 100f,
    maxWidth: 500f
);

// 3. 网格布局：固定宽高
var adapter = StandardVariableSizeAdapter.CreateForGrid(
    prefab: itemTemplate,
    countGetter: () => items.Count,
    dataGetter: index => items[index],
    binder: itemBinder,
    templateBinder: (rt, data) => {
        // 绑定数据到模板
    },
    fixedWidth: 100f,
    fixedHeight: 100f
);

// 4. 初始化 ScrollView
scrollView.Initialize(adapter);
```

### 参数说明

- **fixedWidth/fixedHeight**: 固定尺寸（≤0 表示自适应，>0 表示固定）
- **minWidth/minHeight**: 最小尺寸限制
- **maxWidth/maxHeight**: 最大尺寸限制
- **enableCache**: 是否启用尺寸缓存
- **maxCacheSize**: 最大缓存数量

### 性能优化

- 智能尺寸计算，根据布局模式自动优化
- 缓存机制避免重复计算
- 支持预热缓存提高初始性能
- 智能布局重建减少性能开销
- 对象池管理提高内存效率
- 支持自定义尺寸计算器

## Dependencies

- **Unity 2022.3.62f1** or compatible
- **YooAsset**: Asset management and bundling
- **UniTask**: Async operations
- **PrimeTween**: Animation system
- **NPOI**: Excel processing (via NuGet)
- **Newtonsoft.Json**: JSON serialization
- **TextMeshPro**: Text rendering

## Build Considerations

- Uses URP (Universal Render Pipeline)
- Asset bundling via YooAsset
- Platform-specific builds configured through Unity Editor
- No automated build scripts - use Unity's build system
- Supports multiple languages with resource-based localization