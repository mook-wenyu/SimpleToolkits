# StandardVariableSizeAdapter 系统

## 概述

StandardVariableSizeAdapter 是一个完全集成的标准变尺寸适配器，合并了原 LayoutAutoSizeProvider 的所有功能。它提供了完整的解决方案，包括智能尺寸计算、生命周期管理、缓存机制和性能优化，适用于各种动态尺寸列表场景。

## 主要特性

- **智能尺寸计算**：根据 IScrollLayout 布局模式自动优化尺寸计算
- **完全集成**：合并了生命周期管理、自动尺寸计算、缓存机制
- **自动尺寸计算**：基于 Unity 布局组件（HorizontalOrVerticalLayoutGroup、ContentSizeFitter、LayoutElement）
- **高性能**：支持智能缓存机制，避免重复计算
- **灵活性**：支持固定尺寸、自适应尺寸、最小/最大尺寸限制
- **易用性**：提供简洁的 API 接口和专用构造函数
- **布局感知**：根据纵向/横向/网格布局自动调整计算策略

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
        Func<int, object, Vector2> customSizeCalculator = null,
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
    customSizeCalculator: null,  // 自定义尺寸计算器
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

### v2.0.0
- **重大更新**：智能尺寸计算系统
- 新增固定和自适应尺寸参数支持
- 新增专用构造函数：CreateForVertical、CreateForHorizontal、CreateForGrid
- 优化 BaseVariableSizeAdapter 智能布局感知
- 根据 IScrollLayout 布局模式自动优化尺寸计算
- 更新扩展方法支持新的参数系统
- 优化示例代码和文档

### v1.0.0
- 初始版本
- 支持 AutoSizeProvider 基础功能
- 集成缓存机制
- 支持多种布局场景