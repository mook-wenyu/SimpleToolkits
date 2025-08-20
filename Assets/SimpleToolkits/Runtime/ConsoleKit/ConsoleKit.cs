using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleToolkits
{
    public class ConsoleKit : MonoBehaviour
    {

#if UNITY_ANDROID || UNITY_IOS
    private bool _touching = false;
#endif

        private const int Margin = 20;
        private Rect _windowRect = new(Margin, Margin + 540 / 2f, 960 * 0.5f - (2 * Margin),
            540 - (2 * Margin));

        private Vector2 _scrollPos;
        private readonly List<ConsoleMessage> _entries = new();

        // 命令系统
        private readonly Dictionary<string, CommandInfo> _registeredCommands = new();
        private readonly List<QuickButtonInfo> _quickButtons = new();
        private string _inputText = "";
        private bool _focusInputField = false;

        // GUI样式
        private GUIStyle _labelStyle;
        private GUIStyle _inputFieldStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        /// <summary>
        /// Update回调
        /// </summary>
        public event Action OnUpdateCallback;
        /// <summary>
        /// OnGUI回调
        /// </summary>
        public event Action OnGUICallback;

        private bool _showGUI;
        public bool ShowGUI
        {
            get => _showGUI;
            set
            {
                if (value)
                {
                    _scrollPos = Vector2.up * (_entries.Count * 100.0f);
                    _focusInputField = true;
                }
                _showGUI = value;
            }
        }

        private void Awake()
        {
            Application.logMessageReceived += HandleLog;
            RegisterBuiltinCommands();
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            var entry = new ConsoleMessage(message, stackTrace, type);
            _entries.Add(entry);
        }

        // Update is called once per frame
        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            if (Input.GetKeyUp(KeyCode.F12))
            {
                this.ShowGUI = !this._showGUI;
            }
#elif UNITY_ANDROID || UNITY_IOS
        if (_touching && Input.touchCount == 4) {
            _touching = true;
            this.ShowGUI = !this._showGUI;
        } else if (Input.touchCount == 0) {
            _touching = false;
        }
#endif

            this.OnUpdateCallback?.Invoke();
        }

        private void OnGUI()
        {
            if (!_showGUI)
                return;

            OnGUICallback?.Invoke();

            InitializeStyles();

            var cachedMatrix = GUI.matrix;
            _windowRect = GUILayout.Window(int.MaxValue / 2, _windowRect, DrawConsoleWindow, "控制台");
            GUI.matrix = cachedMatrix;
        }

        /// <summary>
        /// 初始化GUI样式
        /// </summary>
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _labelStyle = new GUIStyle
            {
                fontSize = 10,
                normal =
                {
                    textColor = Color.white
                }
            };

            _inputFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                normal =
                {
                    textColor = Color.white,
                    background = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.8f))
                },
                focused =
                {
                    textColor = Color.white,
                    background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.8f))
                }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                normal =
                {
                    textColor = Color.white
                }
            };

            _stylesInitialized = true;
        }

        /// <summary>
        /// 创建纯色纹理
        /// </summary>
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 显示已记录消息的窗口。
        /// </summary>
        private void DrawConsoleWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry =
                    _entries
                        [i]; // If this message is the same as the last one and the collapse feature is chosen, skip it 
                if (i > 0 && entry.message == _entries[i - 1].message)
                {
                    continue;
                }

                GUI.contentColor = entry.type switch
                {
                    LogType.Error or LogType.Exception => Color.red,
                    LogType.Warning => Color.yellow,
                    _ => Color.white
                };

                if (entry.type == LogType.Exception)
                {
                    GUILayout.Label(entry.message + " || " + entry.stackTrace, _labelStyle);
                }
                else
                {
                    GUILayout.Label(entry.message, _labelStyle);
                }
            }

            GUI.contentColor = Color.white;
            GUILayout.EndScrollView();

            // 命令输入区域
            GUILayout.BeginHorizontal();
            GUILayout.Label("命令:", GUILayout.Width(40));

            GUI.SetNextControlName("CommandInput");

            // 先处理键盘事件，在TextField之前
            var shouldExecuteCommand = false;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                // 检查当前焦点是否在输入框上
                if (GUI.GetNameOfFocusedControl() == "CommandInput" || _focusInputField)
                {
                    shouldExecuteCommand = true;
                    Event.current.Use(); // 立即消费事件，防止TextField处理
                }
            }

            var newInputText = GUILayout.TextField(_inputText, _inputFieldStyle);

            // 处理输入框内容变化
            if (newInputText != _inputText)
            {
                _inputText = newInputText;
            }

            // 执行命令（在TextField之后）
            if (shouldExecuteCommand && !string.IsNullOrEmpty(_inputText.Trim()))
            {
                Debug.Log($"> {_inputText}");
                ExecuteCommand(_inputText);
                _inputText = "";
                // 保持焦点在输入框
                _focusInputField = true;
            }

            GUILayout.EndHorizontal();

            // 快捷按钮区域
            DrawQuickButtons();

            // 自动聚焦到输入框
            if (_focusInputField)
            {
                GUI.FocusControl("CommandInput");
                _focusInputField = false;
            }
        }

        /// <summary>
        /// 绘制快捷按钮区域
        /// </summary>
        private void DrawQuickButtons()
        {
            if (_quickButtons.Count == 0)
                return;

            GUILayout.Space(5);
            GUILayout.Label("快捷操作:", _labelStyle);

            // 计算按钮布局
            const float buttonHeight = 25f;
            const float buttonSpacing = 5f;
            const float maxButtonWidth = 100f;

            var windowWidth = _windowRect.width - 20f; // 减去窗口边距
            var currentLineWidth = 0f;
            var buttonsInCurrentLine = 0;

            foreach (var button in _quickButtons)
            {
                var buttonWidth = Mathf.Min(maxButtonWidth, GUI.skin.button.CalcSize(new GUIContent(button.buttonText)).x + 10f);

                // 检查是否需要换行
                if (buttonsInCurrentLine > 0 && currentLineWidth + buttonWidth + buttonSpacing > windowWidth)
                {
                    GUILayout.EndHorizontal();
                    currentLineWidth = 0f;
                    buttonsInCurrentLine = 0;
                }

                // 开始新行
                if (buttonsInCurrentLine == 0)
                {
                    GUILayout.BeginHorizontal();
                }

                // 绘制按钮
                if (GUILayout.Button(button.buttonText, _buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                {
                    ExecuteQuickButton(button);
                }

                currentLineWidth += buttonWidth + buttonSpacing;
                buttonsInCurrentLine++;
            }

            // 结束最后一行
            if (buttonsInCurrentLine > 0)
            {
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 执行快捷按钮命令
        /// </summary>
        private void ExecuteQuickButton(QuickButtonInfo buttonInfo)
        {
            if (_registeredCommands.TryGetValue(buttonInfo.commandName, out var commandInfo))
            {
                try
                {
                    // 显示执行的命令
                    var commandText = buttonInfo.commandName;
                    if (buttonInfo.args.Length > 0)
                    {
                        commandText += " " + string.Join(" ", buttonInfo.args);
                    }
                    Debug.Log($"> {commandText}");

                    // 执行命令
                    commandInfo.command?.Invoke(buttonInfo.args);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"执行快捷按钮 '{buttonInfo.buttonText}' 时发生错误: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"快捷按钮 '{buttonInfo.buttonText}' 绑定的命令 '{buttonInfo.commandName}' 不存在");
            }
        }

        #region 命令系统
        /// <summary>
        /// 注册控制台命令
        /// </summary>
        /// <param name="commandName">命令名称</param>
        /// <param name="description">命令描述</param>
        /// <param name="command">命令执行方法</param>
        public void RegisterCommand(string commandName, string description, Action<string[]> command)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                Debug.LogError("命令名称不能为空");
                return;
            }

            var commandInfo = new CommandInfo(commandName.ToLower(), description, command);
            _registeredCommands[commandName.ToLower()] = commandInfo;
        }

        /// <summary>
        /// 注销控制台命令
        /// </summary>
        /// <param name="commandName">命令名称</param>
        public void UnregisterCommand(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
                return;

            if (_registeredCommands.Remove(commandName.ToLower())) { }
        }

        /// <summary>
        /// 执行控制台命令
        /// </summary>
        /// <param name="input">用户输入</param>
        private void ExecuteCommand(string input)
        {
            if (string.IsNullOrEmpty(input.Trim()))
                return;

            if (ParseCommand(input, out var commandName, out var args))
            {
                if (_registeredCommands.TryGetValue(commandName, out var commandInfo))
                {
                    try
                    {
                        commandInfo.command?.Invoke(args);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"执行命令 '{commandName}' 时发生错误: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"未知命令: {commandName}。输入 'help' 查看可用命令。");
                }
            }
        }

        /// <summary>
        /// 解析命令输入
        /// </summary>
        /// <param name="input">用户输入</param>
        /// <param name="commandName">命令名称</param>
        /// <param name="args">命令参数</param>
        /// <returns>是否解析成功</returns>
        private bool ParseCommand(string input, out string commandName, out string[] args)
        {
            commandName = "";
            args = Array.Empty<string>();

            if (string.IsNullOrEmpty(input))
                return false;

            var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            commandName = parts[0].ToLower();
            if (parts.Length > 1)
            {
                args = new string[parts.Length - 1];
                Array.Copy(parts, 1, args, 0, args.Length);
            }

            return true;
        }

        /// <summary>
        /// 注册控制台快捷按钮
        /// </summary>
        /// <param name="buttonText">按钮显示文本</param>
        /// <param name="commandName">要执行的命令名称</param>
        /// <param name="args">命令参数</param>
        public void RegisterQuickButton(string buttonText, string commandName, string[] args = null)
        {
            if (string.IsNullOrEmpty(buttonText) || string.IsNullOrEmpty(commandName))
            {
                Debug.LogError("按钮文本和命令名称不能为空");
                return;
            }

            // 检查命令是否存在
            if (!_registeredCommands.ContainsKey(commandName.ToLower()))
            {
                Debug.LogError($"命令 '{commandName}' 不存在，请先注册命令");
                return;
            }

            // 检查按钮是否已存在
            for (var i = 0; i < _quickButtons.Count; i++)
            {
                if (_quickButtons[i].buttonText == buttonText)
                {
                    Debug.LogWarning($"快捷按钮 '{buttonText}' 已存在，将被替换");
                    _quickButtons.RemoveAt(i);
                    break;
                }
            }

            var buttonInfo = new QuickButtonInfo(buttonText, commandName.ToLower(), args);
            _quickButtons.Add(buttonInfo);
        }

        /// <summary>
        /// 注销快捷按钮
        /// </summary>
        /// <param name="buttonText">按钮显示文本</param>
        public void UnregisterQuickButton(string buttonText)
        {
            if (string.IsNullOrEmpty(buttonText))
                return;

            for (var i = 0; i < _quickButtons.Count; i++)
            {
                if (_quickButtons[i].buttonText == buttonText)
                {
                    _quickButtons.RemoveAt(i);
                    Debug.Log($"已注销快捷按钮: {buttonText}");
                    return;
                }
            }
        }
        #endregion

        #region 内置命令
        /// <summary>
        /// 注册内置命令
        /// </summary>
        private void RegisterBuiltinCommands()
        {
            RegisterCommand("help", "显示所有可用命令", HelpCommand);
            RegisterCommand("clear", "清除控制台内容", ClearCommand);
            RegisterCommand("echo", "回显文本", EchoCommand);

            // 注册示例快捷按钮
            RegisterQuickButton("清除日志", "clear");
            RegisterQuickButton("显示帮助", "help");
            RegisterQuickButton("问候", "echo", new[] {"你好，世界！"});
        }

        /// <summary>
        /// 帮助命令
        /// </summary>
        private void HelpCommand(string[] args)
        {
            Debug.Log("=== 可用命令 ===");
            foreach (var kvp in _registeredCommands)
            {
                var cmd = kvp.Value;
                Debug.Log($"{cmd.name} - {cmd.description}");
            }
            Debug.Log("===============");
        }

        /// <summary>
        /// 清除命令
        /// </summary>
        private void ClearCommand(string[] args)
        {
            _entries.Clear();
        }

        /// <summary>
        /// 回显命令
        /// </summary>
        private void EchoCommand(string[] args)
        {
            if (args.Length == 0)
            {
                Debug.Log("用法: echo <文本>");
                return;
            }

            var message = string.Join(" ", args);
            Debug.Log(message);
        }
        #endregion
    }
}
