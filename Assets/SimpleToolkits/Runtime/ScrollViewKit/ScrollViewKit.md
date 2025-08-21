# ScrollViewKit 系统

## 概述

ScrollViewKit 是一个高性能的 Unity 滚动视图系统，支持动态尺寸计算和虚拟化滚动。它提供了完整的解决方案，包括智能尺寸计算、生命周期管理、缓存机制和性能优化，适用于各种动态尺寸列表场景。

### 核心特性

- **高性能虚拟化滚动**：支持大量数据的流畅显示
- **智能尺寸计算**：基于 Unity 布局组件的自动尺寸计算
- **多种布局支持**：纵向、横向、网格布局
- **缓存优化**：智能缓存机制，避免重复计算
- **生命周期管理**：完整的单元格创建、绑定、回收管理
- **易用性**：简洁的 API 接口和专用构造函数

## 系统架构

### 核心接口

```csharp
// 变尺寸适配器接口
public interface IVariableSizeAdapter
{
    Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout);
}

// 单元格绑定器接口
public interface ICellBinder
{
    void OnCreated(RectTransform cell);
    void OnBind(int index, RectTransform cell);
    void OnRecycled(int index, RectTransform cell);
}

// 滚动布局接口
public interface IScrollLayout
{
    bool IsVertical { get; }
    bool ControlChildWidth { get; }
    bool ControlChildHeight { get; }
    Vector2 Spacing { get; }
    RectOffset Padding { get; }
    int ConstraintCount { get; }
}
```

### 主要组件

- **ScrollView**：主要的滚动视图控制器
- **BaseVariableSizeAdapter**：变尺寸适配器基类
- **StandardVariableSizeAdapter**：标准变尺寸适配器实现
- **IScrollLayout**：布局策略接口（VerticalLayout、HorizontalLayout、GridLayout）
- **ICellBinder**：单元格生命周期管理

### 布局系统说明

新的布局系统使用 MonoBehaviour 序列化字段，支持在 Unity Inspector 中直接配置：

#### 继承结构
```
ScrollLayout (抽象基类)
├── VerticalLayout (纵向布局)
├── HorizontalLayout (横向布局)  
└── GridLayout (网格布局)
```

#### 布局类型
- **VerticalLayout**：纵向布局，固定宽度，自适应高度
- **HorizontalLayout**：横向布局，固定高度，自适应宽度  
- **GridLayout**：网格布局，支持纵向/横向滚动

#### 基类功能 (ScrollLayout)
所有布局组件都继承自 ScrollLayout 基类，提供统一的：
- 内边距设置（padding）
- 尺寸控制（controlChildWidth / controlChildHeight）
- 反向排列（reverse）
- 接口实现（IScrollLayout）

#### 子类特有功能
- **VerticalLayout**：spacing (垂直间距)
- **HorizontalLayout**：spacing (水平间距)
- **GridLayout**：spacingX/spacingY (水平和垂直间距), constraintCount (约束数量), isVertical (滚动方向)

## 快速开始

### 1. 纵向布局示例

```csharp
// 1. 创建数据绑定器
var messageBinder = new ChatMessageBinder(messages);

// 2. 创建纵向布局适配器
var adapter = StandardVariableSizeAdapter.CreateForVertical(
    prefab: messageTemplate,
    countGetter: () => messages.Count,
    dataGetter: index => messages[index],
    binder: messageBinder,
    templateBinder: (rt, data) => {
        var message = (ChatMessage)data;
        var text = rt.GetComponent<TextMeshProUGUI>();
        text.text = message.Content;
    },
    fixedWidth: 300f,        // 固定宽度
    minHeight: 60f,          // 最小高度
    maxHeight: 300f,         // 最大高度
    enableCache: true        // 启用缓存
);

// 3. 创建布局策略
var content = scrollView.GetComponent<ScrollRect>().content;
if (content == null)
{
    Debug.LogError("无法找到 ScrollView 的 Content 对象！");
    return;
}

var layout = content.gameObject.AddComponent<VerticalLayout>();
// 在 Inspector 中配置布局参数
layout.spacing = 4f;
layout.padding = new RectOffset(16, 16, 16, 16);
layout.controlChildWidth = true;
layout.controlChildHeight = false;
layout.reverse = false;

// 4. 初始化 ScrollView
scrollView.Initialize(adapter);
```

### 2. 横向布局示例

```csharp
// 横向布局：固定高度，自适应宽度
var adapter = StandardVariableSizeAdapter.CreateForHorizontal(
    prefab: itemTemplate,
    countGetter: () => items.Count,
    dataGetter: index => items[index],
    binder: itemBinder,
    templateBinder: (rt, data) => {
        var item = (ItemData)data;
        var text = rt.GetComponent<TextMeshProUGUI>();
        text.text = item.Name;
    },
    fixedHeight: 100f,       // 固定高度
    minWidth: 60f,           // 最小宽度
    maxWidth: 200f,          // 最大宽度
    enableCache: true
);
```

## 核心组件

### 1. StandardVariableSizeAdapter（完全集成的适配器）

```csharp
public sealed class StandardVariableSizeAdapter : BaseVariableSizeAdapter
{
    // 完整构造函数 - 支持固定和自适应尺寸参数
    public StandardVariableSizeAdapter(
        RectTransform prefab,
        Func<int> countGetter,
        Func<int, object> dataGetter,
        ICellBinder binder,
        Action<RectTransform, object> templateBinder,
        float fixedWidth = -1f,    // ≤0 表示自适应
        float fixedHeight = -1f,   // ≤0 表示自适应
        float minWidth = 0f,
        float minHeight = 0f,
        float maxWidth = -1f,
        float maxHeight = -1f,
        bool useLayoutGroups = true,
        bool enableCache = true,
        int maxCacheSize = 1000,
        bool forceRebuild = false);
    
    // 专用构造函数 - 纵向布局（固定宽度，自适应高度）
    public static StandardVariableSizeAdapter CreateForVertical(
        RectTransform prefab,
        Func<int> countGetter,
        Func<int, object> dataGetter,
        ICellBinder binder,
        Action<RectTransform, object> templateBinder,
        float fixedWidth,
        float minHeight = 60f,
        float maxHeight = 300f,
        bool enableCache = true,
        int maxCacheSize = 1000);
    
    // 专用构造函数 - 横向布局（固定高度，自适应宽度）
    public static StandardVariableSizeAdapter CreateForHorizontal(
        RectTransform prefab,
        Func<int> countGetter,
        Func<int, object> dataGetter,
        ICellBinder binder,
        Action<RectTransform, object> templateBinder,
        float fixedHeight,
        float minWidth = 60f,
        float maxWidth = 300f,
        bool enableCache = true,
        int maxCacheSize = 1000);
    
    // 专用构造函数 - 网格布局（固定宽高）
    public static StandardVariableSizeAdapter CreateForGrid(
        RectTransform prefab,
        Func<int> countGetter,
        Func<int, object> dataGetter,
        ICellBinder binder,
        Action<RectTransform, object> templateBinder,
        float fixedWidth,
        float fixedHeight,
        bool enableCache = true,
        int maxCacheSize = 1000);
    
    // 兼容构造函数 - 使用外部尺寸提供器
    public StandardVariableSizeAdapter(RectTransform prefab, Func<int> countGetter, ICellBinder binder, IVariableSizeAdapter sizeProvider);
}
```

### 2. ICellBinder（生命周期管理接口）

```csharp
public interface ICellBinder
{
    void OnCreated(RectTransform cell);
    void OnBind(int index, RectTransform cell);
    void OnRecycled(int index, RectTransform cell);
}
```

## 使用示例

### 1. 纵向布局（固定宽度，自适应高度）

```csharp
// 1. 创建业务绑定器
var messageBinder = new ChatMessageBinder(messages);

// 2. 创建纵向布局适配器
var adapter = StandardVariableSizeAdapter.CreateForVertical(
    prefab: messageTemplate,
    countGetter: () => messages.Count,
    dataGetter: index => messages[index],
    binder: messageBinder,
    templateBinder: (rt, data) => {
        var message = (ChatMessage)data;
        var text = rt.GetComponent<TextMeshProUGUI>();
        text.text = message.Content;
    },
    fixedWidth: 300f,        // 固定宽度
    minHeight: 60f,          // 最小高度
    maxHeight: 500f,         // 最大高度
    enableCache: true        // 启用缓存
);

// 3. 初始化 ScrollView
scrollView.Initialize(adapter);
```

### 2. 横向布局（固定高度，自适应宽度）

```csharp
// 创建横向布局适配器
var adapter = StandardVariableSizeAdapter.CreateForHorizontal(
    prefab: messageTemplate,
    countGetter: () => messages.Count,
    dataGetter: index => messages[index],
    binder: messageBinder,
    templateBinder: (rt, data) => {
        var message = (ChatMessage)data;
        var text = rt.GetComponent<TextMeshProUGUI>();
        text.text = message.Content;
    },
    fixedHeight: 300f,       // 固定高度
    minWidth: 100f,          // 最小宽度
    maxWidth: 500f,          // 最大宽度
    enableCache: true
);

scrollView.Initialize(adapter);
```

### 3. 网格布局（固定宽高）

```csharp
// 创建网格布局适配器
var adapter = StandardVariableSizeAdapter.CreateForGrid(
    prefab: itemTemplate,
    countGetter: () => items.Count,
    dataGetter: index => items[index],
    binder: itemBinder,
    templateBinder: (rt, data) => {
        var item = (ItemData)data;
        var icon = rt.GetComponent<Image>();
        var text = rt.GetComponent<TextMeshProUGUI>();
        icon.sprite = item.Icon;
        text.text = item.Name;
    },
    fixedWidth: 100f,        // 固定宽度
    fixedHeight: 100f,       // 固定高度
    enableCache: true
);

scrollView.Initialize(adapter);
```

### 4. 使用完整构造函数（自定义参数）

```csharp
// 使用完整构造函数进行精细控制
var adapter = new StandardVariableSizeAdapter(
    prefab: messageTemplate,
    countGetter: () => messages.Count,
    dataGetter: index => messages[index],
    binder: messageBinder,
    templateBinder: (rt, data) => {
        var message = (ChatMessage)data;
        var text = rt.GetComponent<TextMeshProUGUI>();
        text.text = message.Content;
    },
    fixedWidth: 300f,        // 固定宽度
    fixedHeight: -1f,        // 高度自适应（≤0 表示自适应）
    minWidth: 300f,          // 最小宽度
    minHeight: 60f,          // 最小高度
    maxWidth: 300f,          // 最大宽度
    maxHeight: 500f,         // 最大高度
    useLayoutGroups: true,   // 使用布局组
    enableCache: true,       // 启用缓存
    maxCacheSize: 1000,      // 最大缓存大小
    forceRebuild: false      // 是否强制重建布局
);

scrollView.Initialize(adapter);
```

### 5. 使用扩展方法（更简洁）

```csharp
// 使用扩展方法快速创建
var adapter = StandardVariableSizeAdapterExtensions.CreateForList(
    template: messageTemplate,
    dataList: messages,
    binder: messageBinder,
    templateBinder: (rt, data) => {
        var message = (ChatMessage)data;
        var text = rt.GetComponent<TextMeshProUGUI>();
        text.text = message.Content;
    },
    fixedWidth: 300f,        // 固定宽度
    fixedHeight: -1f,        // 高度自适应
    minHeight: 60f,          // 最小高度
    maxHeight: 500f          // 最大高度
);

scrollView.Initialize(adapter);
```

### 6. 兼容用法（使用外部尺寸提供器）

```csharp
// 1. 创建外部尺寸提供器
var sizeProvider = new CustomSizeProvider();

// 2. 创建适配器
var adapter = new StandardVariableSizeAdapter(
    prefab: messageTemplate,
    countGetter: () => messages.Count,
    binder: messageBinder,
    sizeProvider: sizeProvider
);

scrollView.Initialize(adapter);
```

## 性能优化

### 1. 缓存机制

```csharp
// 启用缓存
var sizeProvider = new LayoutAutoSizeProvider(
    // ... 其他参数
    enableCache: true,
    maxCacheSize: 1000
);

// 预热缓存
sizeProvider.PreheatCache(layout, viewportSize, 0, count);

// 清理缓存
sizeProvider.ClearCache();
```

### 2. 布局重建优化

```csharp
// 避免频繁重建布局
var sizeProvider = new LayoutAutoSizeProvider(
    // ... 其他参数
    forceRebuild: false
);

// 手动强制重建
sizeProvider.ForceRebuildLayout();
```

## 接口说明

### IVariableSizeAdapter

```csharp
public interface IVariableSizeAdapter
{
    /// <summary>
    /// 计算指定索引项的尺寸
    /// </summary>
    /// <param name="index">数据索引</param>
    /// <param name="viewportSize">视口尺寸</param>
    /// <param name="layout">布局策略</param>
    /// <returns>该项的尺寸</returns>
    Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout);
}
```

## 最佳实践

### 1. 模板设置

- 确保模板预制体包含必要的布局组件
- 对于文本内容，使用 ContentSizeFitter 自动调整尺寸
- 使用 LayoutElement 设置最小/首选尺寸

### 2. 数据绑定

- 保持数据绑定逻辑简单高效
- 避免在绑定过程中进行复杂计算
- 使用缓存机制减少重复操作

### 3. 性能考虑

- 对于大量数据，启用缓存机制
- 合理设置最大缓存大小
- 避免在每一帧都重建布局

## 常见问题

### Q: 如何处理文本换行？

A: 确保文本组件启用了 ContentSizeFitter，并设置合适的布局参数。

### Q: 如何提高性能？

A: 启用缓存机制，预热缓存，避免频繁重建布局。

### Q: 如何处理动态内容？

A: 在数据更新时调用 `ClearCache()` 清理缓存，然后刷新 ScrollView。

## 版本历史

### v2.1.0
- **API 简化**：删除 customSizeCalculator 参数
- 简化尺寸计算逻辑，统一使用 templateBinder
- 减少功能重叠，提高代码可维护性
- 优化示例代码和文档

**设计说明**：
- 删除 customSizeCalculator 参数是因为它与 templateBinder 功能重叠
- Unity 布局系统已经能处理绝大多数尺寸计算需求
- 对于特殊需求，可以通过继承 BaseVariableSizeAdapter 来实现
- 这样的简化让 API 更清晰，减少用户困惑

### v1.0.0
- 初始版本
- 支持 AutoSizeProvider 基础功能
- 集成缓存机制
- 支持多种布局场景