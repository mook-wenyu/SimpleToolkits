# TypeText 标签系统完整指南

SimpleToolkits 的 TypeText 组件提供了一个高性能、简洁的标签系统，专注于方括号标签处理。

## ✨ 特点

- **高性能**: 使用 ReadOnlySpan<char> 优化解析性能
- **简洁架构**: 只支持方括号标签 `[tag]`，避免复杂性
- **灵活扩展**: ActionTagProcessor 提供全局动作委托系统
- **易于使用**: 统一的标签语法，开箱即用

## 🚀 快速开始

### 基础使用

```csharp
// 获取 TextMeshProUGUI 组件
var textComponent = GetComponent<TextMeshProUGUI>();

// 使用扩展方法开始打字效果
await textComponent.TypeTextAsync("Hello World!", 0.05f);

// 或者直接使用组件
var typeText = textComponent.GetOrAddTypeTextComponent();
await typeText.StartTypingAsync("Hello World!", 0.05f);
```

### 跳过和控制

```csharp
// 检查是否可以跳过
if (textComponent.IsTypeTextSkippable())
{
    // 跳过打字效果
    await textComponent.SkipTypeTextAsync();
}

// 停止打字效果
await textComponent.StopTypeTextAsync();

// 立即设置文本（无动画）
textComponent.SetTextInstant("Instant text");
```

## 📝 内置标签详解

### 1. 控制标签（方括号 `[]`）

#### 速度控制
- `[speed=0.1]` - 设置打字速度（秒/字符），影响后续所有文字
- `[speedregion=0.1]文字[/speedregion]` - 区域速度控制，只影响标签内的文字，结束后恢复之前的速度
- 示例：`"正常速度 [speedregion=0.02]这里很慢[/speedregion] 回到正常速度"`

#### 暂停和等待
- `[pause=2.0]` - 暂停指定时间（秒）
- 示例：`"Wait... [pause=1.5]Done!"`

#### 文本控制
- `[clear]` - 清空已显示的文本
- `[br]` 或 `[n]` - 换行
- 示例：`"Line 1[br]Line 2"`

#### 动作标签
- `[action=id]` - 触发全局动作委托
- 示例：`"Play sound [action=beep] here!"`

### 动作标签使用示例

```csharp
// 注册动作监听器
ActionTagProcessor.OnAction += (actionId) => {
    switch(actionId) {
        case "beep":
            // 播放声音
            AudioManager.PlaySound("beep");
            break;
        case "shake":
            // 震动效果
            CameraShake.Shake(0.5f);
            break;
        case "questAccept":
            // 接受任务
            QuestManager.AcceptQuest("main_quest_1");
            break;
    }
};

// 使用动作标签
var text = "[action=beep]Beep sound![action=shake] Camera shakes!";
await textComponent.TypeTextAsync(text);
```

### 2. 样式标签（方括号 `[]`，自动转换为TextMeshPro格式）

#### 颜色和大小
- `[color=red]文本[/color]` - 颜色（转换为 `<color=red>`）
- `[size=20]文本[/size]` - 字体大小（转换为 `<size=20>`）

#### 文本样式
- `[b]粗体[/b]` - 粗体（转换为 `<b>`）
- `[i]斜体[/i]` - 斜体（转换为 `<i>`）
- `[u]下划线[/u]` - 下划线（转换为 `<u>`）

### 速度控制对比示例

```csharp
// 使用 [speed] 标签 - 永久改变速度
var text1 = "正常速度文字 [speed=0.02]变慢了，后面都是慢的 [speed=0.1]变快了，后面都是快的";

// 使用 [speedregion] 标签 - 临时改变速度
var text2 = "正常速度 [speedregion=0.02]只有这里慢[/speedregion] 恢复正常 [speedregion=0.1]只有这里快[/speedregion] 又恢复正常";

// 嵌套使用
var text3 = "[speed=0.1]快速模式 [speedregion=0.02]临时慢下来[/speedregion] 恢复快速模式";

await textComponent.TypeTextAsync(text2);
```

### 组合标签示例

```csharp
var styledText = "This is [color=red][b]red bold[/b][/color] and [i][size=20]big italic[/size][/i] text!";
await textComponent.TypeTextAsync(styledText);
```

### 3. TextMeshPro 原生标签兼容性

**重要**: 现在系统只处理方括号标签 `[tag]`。对于 TextMeshPro 原生标签，推荐使用我们的方括号版本：

- 使用 `[color=red]文本[/color]` 而不是 `<color=red>文本</color>`
- 使用 `[size=20]文本[/size]` 而不是 `<size=20>文本</size>`
- 使用 `[b]粗体[/b]` 而不是 `<b>粗体</b>`

这样做的好处：
- 统一的标签语法，更易理解和维护
- 支持标签验证和错误检查
- 与打字机效果完全集成


## 🎨 自定义标签开发

### 1. 简单标签处理器

```csharp
public class MyCustomTagProcessor : BaseTagProcessor
{
    public override string TagName => "mycustom";

    public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
    {
        // 处理 [mycustom=parameter] 标签
        Debug.Log($"Custom tag with parameter: {tagContent}");
        
        // 返回处理结果
        return new TagProcessResult(true, 0, true); // 处理成功，跳过显示
    }
}

// 注册自定义标签
TypeTextUtility.RegisterTagProcessor(new MyCustomTagProcessor());
```

### 2. 异步标签处理器

```csharp
public class DelayTagProcessor : BaseTagProcessor, IAsyncTagProcessor
{
    public override string TagName => "delay";

    public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
    {
        return new TagProcessResult(false, 0); // 让异步版本处理
    }

    public async UniTask<TagProcessResult> ProcessTagAsync(TagProcessContext context, string tagContent)
    {
        var seconds = ParseParameter<float>(tagContent, 1.0f);
        await UniTask.Delay(TimeSpan.FromSeconds(seconds));
        
        return new TagProcessResult(true, 0, true);
    }
}
```

### 3. 样式转换标签

```csharp
public class WaveTagProcessor : BaseTagProcessor
{
    public override string TagName => "wave";
    public override bool RequiresClosingTag => true;

    public override TagProcessResult ProcessTag(TagProcessContext context, string tagContent)
    {
        // 转换为 TextMeshPro 波浪效果
        var waveTag = "<wave>";
        return new TagProcessResult(true, waveTag.Length, false, waveTag);
    }

    public override TagProcessResult ProcessClosingTag(TagProcessContext context)
    {
        return new TagProcessResult(true, 7, false, "</wave>");
    }

    public override bool ShouldRemoveFromDisplayText(string text, int startIndex)
    {
        return true; // 移除方括号版本，替换为角括号版本
    }
}
```

## 🛠️ 标签配置

系统默认启用所有内置标签处理器，无需额外配置。您可以直接使用 TypeTextComponent 或注册自定义标签处理器。

### 代码配置

```csharp
// 获取标签注册器
var registry = TagProcessorRegistry.Instance;

// 注册多个处理器
registry.RegisterProcessors(
    new MyCustomTagProcessor(),
    new WaveTagProcessor(),
    new DelayTagProcessor()
);

// 检查标签是否存在
if (registry.HasProcessor("mycustom"))
{
    Debug.Log("Custom tag is registered!");
}

// 获取统计信息
Debug.Log(registry.GetStatistics());
```

## 🔍 文本验证

### 验证标签格式

```csharp
// 验证文本格式
var result = textComponent.ValidateTypeText(text);
if (!result.isValid)
{
    foreach (var error in result.errors)
    {
        Debug.LogError($"Validation error: {error}");
    }
}

// 或使用工具类
var result2 = TypeTextUtility.ValidateTextFormat(text);
```

### 获取文本中使用的标签

```csharp
var usedTags = TypeTextUtility.GetUsedTags(text);
foreach (var tag in usedTags)
{
    Debug.Log($"Used tag: {tag}");
}
```

## 📊 性能优化

### 文本预处理

```csharp
// 清理文本（移除控制标签，保留样式标签）
var cleanText = TypeTextUtility.CleanText(originalText);

// 检查处理器是否存在，避免不必要的查找
if (TypeTextUtility.HasTagProcessor("customtag"))
{
    // 使用标签
}
```

### 批量标签注册

```csharp
// 一次性注册多个标签
TypeTextUtility.RegisterTagProcessors(
    new TagProcessor1(),
    new TagProcessor2(),
    new TagProcessor3()
);
```

## 🎮 实际使用场景

### 对话系统

```csharp
public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string[] dialogues = {
        "[color=blue]NPC[/color]: Hello there!",
        "[color=blue]NPC[/color]: [pause=1.0]Would you like a quest?",
        "[color=green]Player[/color]: Sure!",
        "[action=questAccept][color=blue]NPC[/color]: Great! Here's your quest."
    };

    private async UniTask PlayDialogue(int index)
    {
        await dialogueText.TypeTextAsync(dialogues[index], 0.05f);
    }
}
```

### 游戏提示系统

```csharp
public class TutorialSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tutorialText;

    public async UniTask ShowTutorial()
    {
        var tutorial = 
            "[size=30][color=yellow]Tutorial[/color][/size][br]" +
            "[pause=0.5]Press [b][color=red]WASD[/color][/b] to move[br]" +
            "[pause=0.5]Press [b][color=blue]SPACE[/color][/b] to jump[br]" +
            "[pause=0.5]Press any key to continue...";

        await tutorialText.TypeTextAsync(tutorial, 0.03f);
    }
}
```

## 🐛 调试和故障排除

### Inspector 调试方法

在 TypeTextComponent 上右键，可以看到以下调试选项：
- **Validate Current Text** - 验证当前文本
- **Show Tag Statistics** - 显示标签统计
- **Test Text Processing** - 测试文本处理

### 常见问题

1. **标签不工作**：检查是否正确注册了标签处理器
2. **文本验证失败**：检查标签是否正确配对（开始/结束标签）
3. **异步标签卡住**：确保异步标签处理器正确实现了 `IAsyncTagProcessor`
4. **性能问题**：避免在每帧中创建新的 TagParsingEngine，使用组件内置的引擎

### 日志输出

```csharp
// 启用调试日志
Debug.Log(TypeTextUtility.GetTagStatistics());

// 显示处理过程
var engine = new TagParsingEngine();
var tags = engine.ParseTags(text);
foreach (var tag in tags)
{
    Debug.Log($"Found tag: {tag.tagName} = {tag.parameter}");
}
```

## 📈 扩展建议

### 常用自定义标签示例

1. **动画标签**：`[bounce]`, `[fade]`, `[slide]`
2. **交互标签**：`[button=onclick]`, `[link=url]`
3. **游戏特定**：`[damage=100]`, `[item=sword]`, `[skill=fireball]`
4. **多媒体**：`[video=clip]`, `[image=sprite]`

### 与其他系统集成

- **音频系统**：在 SoundTagProcessor 中集成 AudioKit
- **本地化**：创建 `[locale=key]` 标签支持多语言

---

这个标签系统为 SimpleToolkits 的对话系统提供了强大而灵活的文本处理能力，既支持常见的文本效果，也支持复杂的自定义扩展。通过合理使用内置标签和创建自定义标签，可以实现丰富多彩的对话和文本展示效果。