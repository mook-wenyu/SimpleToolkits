# BaseVariableSizeAdapter 系统

## 概述

BaseVariableSizeAdapter 是一个增强的基础变尺寸适配器基类，合并了原 AutoSizeProvider 的功能。它提供了基于 Unity 布局组件的自动尺寸计算系统，旨在简化 ScrollView 中动态尺寸项的开发。

## 主要特性

- **自动尺寸计算**：基于 Unity 布局组件（HorizontalOrVerticalLayoutGroup、ContentSizeFitter、LayoutElement）
- **高性能**：支持缓存机制，避免重复计算
- **灵活性**：支持固定尺寸、自适应尺寸、最小/最大尺寸限制
- **易用性**：提供简洁的 API 接口，降低使用门槛
- **统一架构**：将原有分离的 AutoSizeProvider 和 BaseVariableSizeAdapter 合并为单一基类

## 核心组件

### 1. BaseVariableSizeAdapter（增强的抽象基类）

```csharp
public abstract class BaseVariableSizeAdapter : BaseScrollAdapter, IVariableSizeAdapter
{
    // 主要方法
    public virtual Vector2 GetItemSize(int index, Vector2 viewportSize, IScrollLayout layout);
    
    // 抽象方法（子类必须实现）
    protected abstract int GetItemCount();
    protected abstract Vector2 GetBaseSize(int index, Vector2 viewportSize, IScrollLayout layout);
    protected abstract object GetDataForLayout(int index);
    
    // 公共方法
    public void ForceRebuildLayout();
    public void ClearCache();
    public void UpdateTemplate(RectTransform newTemplate);
}
```

### 2. LayoutAutoSizeProvider（具体实现）

```csharp
public class LayoutAutoSizeProvider : BaseVariableSizeAdapter
{
    // 构造函数
    public LayoutAutoSizeProvider(
        RectTransform template,
        Func<int> countGetter,
        Func<int, object> dataGetter,
        Action<RectTransform, object> templateBinder,
        Vector2 fixedSize,
        Vector2 minSize,
        Vector2 maxSize,
        bool useLayoutGroups = true,
        bool enableCache = true,
        int maxCacheSize = 1000,
        Func<int, object, Vector2> customSizeCalculator = null,
        bool forceRebuild = false
    );
}
```

## 使用示例

### 基本用法

```csharp
// 1. 创建尺寸提供器（现在继承自BaseVariableSizeAdapter）
var sizeProvider = new LayoutAutoSizeProvider(
    template: messageTemplate,
    countGetter: () => messages.Count,
    dataGetter: index => messages[index],
    templateBinder: (rt, data) => {
        var message = (ChatMessage)data;
        var text = rt.GetComponent<TextMeshProUGUI>();
        text.text = message.Content;
    },
    fixedSize: new Vector2(300, -1), // 固定宽度，自适应高度
    minSize: new Vector2(300, 60),
    maxSize: new Vector2(300, 500)
);

// 2. 创建适配器
var adapter = new StandardVariableSizeAdapter(
    prefab: messageTemplate,
    countGetter: () => messages.Count,
    binder: messageBinder,
    sizeProvider: sizeProvider
);

// 3. 初始化 ScrollView
scrollView.Initialize(adapter);
```

### 直接继承BaseVariableSizeAdapter

```csharp
public class CustomSizeAdapter : BaseVariableSizeAdapter
{
    private readonly List<CustomData> _dataList;
    
    public CustomSizeAdapter(RectTransform prefab, List<CustomData> dataList)
        : base(prefab, prefab, () => dataList.Count)
    {
        _dataList = dataList;
    }
    
    protected override int GetItemCount() => _dataList.Count;
    
    protected override Vector2 GetBaseSize(int index, Vector2 viewportSize, IScrollLayout layout)
    {
        // 返回基础尺寸
        return new Vector2(200, 100); // 固定尺寸
    }
    
    protected override object GetDataForLayout(int index)
    {
        return _dataList[index];
    }
    
    protected override void OnBind(int index, RectTransform cell)
    {
        // 绑定数据到cell
        var data = _dataList[index];
        var text = cell.GetComponent<TextMeshProUGUI>();
        text.text = data.Name;
    }
}
```

### 高级用法

```csharp
// 带缓存的尺寸提供器
var sizeProvider = new LayoutAutoSizeProvider(
    template: messageTemplate,
    countGetter: () => messages.Count,
    dataGetter: index => messages[index],
    templateBinder: BindMessageData,
    fixedSize: new Vector2(0, 100), // 横向列表：自适应宽度，固定高度
    minSize: new Vector2(100, 100),
    maxSize: new Vector2(500, 100),
    useLayoutGroups: true,
    enableCache: true,
    maxCacheSize: 1000,
    forceRebuild: false
);

// 预热缓存
sizeProvider.PreheatCache(layout, viewportSize, 0, messages.Count);
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

### v1.0.0
- 初始版本
- 支持 AutoSizeProvider 基础功能
- 集成缓存机制
- 支持多种布局场景