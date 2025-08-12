using System;

/// <summary>
/// YooAsset 资源包信息类
/// </summary>
[Serializable]
public class YooPackageInfo
{
    /// <summary>
    /// 资源包名称
    /// </summary>
    public string packageName;
    /// <summary>
    /// 主服务器地址
    /// </summary>
    public string hostServerURL;
    /// <summary>
    /// 备用服务器地址
    /// </summary>
    public string fallbackHostServerURL;
    /// <summary>
    /// 是否为默认资源包
    /// </summary>
    public bool isDefaultPackage;

    public YooPackageInfo() { }

    public YooPackageInfo(string packageName, string hostServerURL = "", string fallbackHostServerURL = "", bool isDefaultPackage = false)
    {
        this.packageName = packageName;
        this.hostServerURL = hostServerURL;
        this.fallbackHostServerURL = fallbackHostServerURL;
        this.isDefaultPackage = isDefaultPackage;
    }
}
