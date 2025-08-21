namespace SimpleToolkits.ScrollViewExample.Models
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// 普通消息
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// 用户消息
        /// </summary>
        User = 1,
        
        /// <summary>
        /// 系统消息
        /// </summary>
        System = 2,
        
        /// <summary>
        /// 错误消息
        /// </summary>
        Error = 3,
        
        /// <summary>
        /// 警告消息
        /// </summary>
        Warning = 4,
        
        /// <summary>
        /// 成功消息
        /// </summary>
        Success = 5
    }

    /// <summary>
    /// 聊天消息模型
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// 消息唯一标识符
        /// </summary>
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// 发送者名称
        /// </summary>
        public string Sender { get; set; } = string.Empty;

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 发送时间
        /// </summary>
        public string Time { get; set; } = System.DateTime.Now.ToString("HH:mm:ss");

        /// <summary>
        /// 是否为系统消息
        /// </summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// 消息类型
        /// </summary>
        public MessageType Type { get; set; } = MessageType.Normal;

        /// <summary>
        /// 消息优先级（用于排序）
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 消息标签（用于分类）
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// 消息是否已读
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// 消息创建时间戳
        /// </summary>
        public long Timestamp { get; set; } = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// 创建普通聊天消息
        /// </summary>
        public static ChatMessage CreateNormal(string sender, string content)
        {
            return new ChatMessage
            {
                Sender = sender,
                Content = content,
                Type = MessageType.Normal,
                IsSystem = false
            };
        }

        /// <summary>
        /// 创建用户消息
        /// </summary>
        public static ChatMessage CreateUser(string sender, string content)
        {
            return new ChatMessage
            {
                Sender = sender,
                Content = content,
                Type = MessageType.User,
                IsSystem = false
            };
        }

        /// <summary>
        /// 创建系统消息
        /// </summary>
        public static ChatMessage CreateSystem(string content)
        {
            return new ChatMessage
            {
                Sender = "系统",
                Content = content,
                Type = MessageType.System,
                IsSystem = true
            };
        }

        /// <summary>
        /// 创建错误消息
        /// </summary>
        public static ChatMessage CreateError(string content)
        {
            return new ChatMessage
            {
                Sender = "系统",
                Content = content,
                Type = MessageType.Error,
                IsSystem = true
            };
        }

        /// <summary>
        /// 创建警告消息
        /// </summary>
        public static ChatMessage CreateWarning(string content)
        {
            return new ChatMessage
            {
                Sender = "系统",
                Content = content,
                Type = MessageType.Warning,
                IsSystem = true
            };
        }

        /// <summary>
        /// 创建成功消息
        /// </summary>
        public static ChatMessage CreateSuccess(string content)
        {
            return new ChatMessage
            {
                Sender = "系统",
                Content = content,
                Type = MessageType.Success,
                IsSystem = true
            };
        }

        /// <summary>
        /// 克隆消息
        /// </summary>
        public ChatMessage Clone()
        {
            return new ChatMessage
            {
                Id = System.Guid.NewGuid().ToString(),
                Sender = this.Sender,
                Content = this.Content,
                Time = this.Time,
                IsSystem = this.IsSystem,
                Type = this.Type,
                Priority = this.Priority,
                Tag = this.Tag,
                IsRead = this.IsRead,
                Timestamp = this.Timestamp
            };
        }

        /// <summary>
        /// 获取消息的简短描述
        /// </summary>
        public string GetShortDescription(int maxLength = 50)
        {
            if (string.IsNullOrEmpty(Content)) return string.Empty;
            
            if (Content.Length <= maxLength) return Content;
            
            return Content.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// 判断是否为重要消息
        /// </summary>
        public bool IsImportant()
        {
            return Type == MessageType.Error || Type == MessageType.Warning || Priority > 0;
        }

        /// <summary>
        /// 获取消息的颜色标识
        /// </summary>
        public string GetColorHex()
        {
            return Type switch
            {
                MessageType.Normal => "#FFFFFF",
                MessageType.User => "#4CAF50",
                MessageType.System => "#9E9E9E",
                MessageType.Error => "#F44336",
                MessageType.Warning => "#FF9800",
                MessageType.Success => "#8BC34A",
                _ => "#FFFFFF"
            };
        }
    }
}