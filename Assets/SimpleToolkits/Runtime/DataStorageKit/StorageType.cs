namespace SimpleToolkits
{
    /// <summary>
    /// 存储方式类型
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// 自动选择（根据平台自动选择最适合的存储方式）
        /// </summary>
        Auto,

        /// <summary>
        /// PlayerPrefs 存储（适用于 Web 平台）
        /// </summary>
        PlayerPrefs,

        /// <summary>
        /// JSON 文件存储（存储到 persistentDataPath）
        /// </summary>
        JsonFile
    }
}
