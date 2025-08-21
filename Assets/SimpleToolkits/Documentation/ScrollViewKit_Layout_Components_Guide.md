# Unity ScrollViewKit 布局组件使用指南

## 问题背景

之前开发者反映："VerticalLayout 等组件可以用代码添加，但在Unity Inspector面板中找不到"

## 根本原因分析

### 1. 继承问题（已修复）
- **之前**: 继承自 `Component`（抽象基类，不能作为组件添加）
- **现在**: 继承自 `MonoBehaviour`（可以作为组件添加）

### 2. 菜单显示问题（本次修复）
- **之前**: 缺少 `AddComponentMenu` 属性，组件隐藏在菜单底部
- **现在**: 添加了 `AddComponentMenu` 属性，组件显示在指定分类下

## 修复内容

### 添加 AddComponentMenu 属性
```csharp
[AddComponentMenu("SimpleToolkits/Scroll View/Vertical Layout")]
public class VerticalLayout : MonoBehaviour, IScrollLayout

[AddComponentMenu("SimpleToolkits/Scroll View/Horizontal Layout")]
public class HorizontalLayout : MonoBehaviour, IScrollLayout

[AddComponentMenu("SimpleToolkits/Scroll View/Grid Layout")]
public class GridLayout : MonoBehaviour, IScrollLayout
```

### 改进文档注释
- 为每个布局类添加了更清晰的用途说明
- 明确指出用于ScrollView的布局

## 使用方法

### 在Unity Inspector中添加组件
1. 选择GameObject（通常是ScrollView的Content对象）
2. 点击"Add Component"按钮
3. 导航到"SimpleToolkits/Scroll View/"分类
4. 选择需要的布局组件：
   - **Vertical Layout**: 纵向滚动布局
   - **Horizontal Layout**: 横向滚动布局
   - **Grid Layout**: 网格滚动布局

### 通过代码添加组件
```csharp
// 纵向布局
var verticalLayout = content.gameObject.AddComponent<VerticalLayout>();
verticalLayout.spacing = 4f;
verticalLayout.padding = new RectOffset(16, 16, 16, 16);

// 横向布局
var horizontalLayout = content.gameObject.AddComponent<HorizontalLayout>();
horizontalLayout.spacing = 4f;
horizontalLayout.padding = new RectOffset(16, 16, 16, 16);

// 网格布局
var gridLayout = content.gameObject.AddComponent<GridLayout>();
gridLayout.spacingX = 4f;
gridLayout.spacingY = 4f;
gridLayout.constraintCount = 2;
gridLayout.isVertical = true;
```

## 与Unity UI布局组件的区别

### SimpleToolkits布局组件
- **用途**: 专门用于ScrollView的虚拟化滚动
- **命名空间**: `SimpleToolkits`
- **位置**: SimpleToolkits/Scroll View/
- **特点**: 支持虚拟化、高性能、动态尺寸计算

### Unity UI布局组件
- **用途**: 用于普通UI布局
- **命名空间**: `UnityEngine.UI`
- **位置**: Layout/
- **特点**: 用于静态UI布局，不支持虚拟化滚动

## 常见问题

### Q: 为什么有两个VerticalLayout？
A: 一个是Unity UI的`VerticalLayoutGroup`，一个是我们的`VerticalLayout`。用途不同：
- Unity UI: 用于普通UI布局
- SimpleToolkits: 用于ScrollView虚拟化滚动

### Q: 如何选择正确的布局组件？
A: 如果您需要高性能的滚动视图，请使用SimpleToolkits的布局组件。如果只需要普通UI布局，请使用Unity UI的布局组件。

### Q: 组件参数如何配置？
A: 在Inspector中可以直接配置所有参数，包括：
- padding: 内边距
- spacing: 间距
- controlChildWidth/Height: 是否控制子对象尺寸
- reverse: 是否反向排列
- constraintCount: 约束数量（仅GridLayout）

## 验证修复

使用提供的测试脚本 `LayoutComponentTest.cs` 可以验证修复是否成功：
1. 将脚本添加到场景中的GameObject
2. 右键点击组件，选择"测试添加布局组件"
3. 检查Console输出，确认所有组件都能正常添加和配置

## 总结

通过这次修复，开发者现在可以：
- ✅ 在Unity Inspector中轻松找到布局组件
- ✅ 通过菜单系统方便地添加布局组件
- ✅ 在Inspector中直接配置布局参数
- ✅ 使用代码动态添加和配置布局组件
- ✅ 区分SimpleToolkits和Unity UI的布局组件