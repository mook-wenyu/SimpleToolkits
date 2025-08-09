using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class ExcelEditor
{
    /// <summary>
    /// 字段信息
    /// </summary>
    private class PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Comment { get; set; }
    }

    /// <summary>
    /// Excel 配置信息
    /// </summary>
    private class ExcelConfig
    {
        public string ConfigName { get; set; }
        public List<PropertyInfo> Properties { get; set; }
        public DataTable Sheet { get; set; }
    }

    private static List<ExcelConfig> _excelConfigs = new();
    private static readonly string[] _extensions = {".xlsx", ".xls"};
    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        Formatting = Formatting.Indented,
    };

    [MenuItem("Tools/Excel To Json")]
    public static void GenerateConfigs()
    {
        DeleteAllOldFiles();

        string excelDirPath = ExcelExporterSettings.Instance.ExcelFullPath;
        if (!Directory.Exists(excelDirPath)) Directory.CreateDirectory(excelDirPath);

        string[] excelFiles = Directory.EnumerateFiles(excelDirPath)
            .Where(file =>
            {
                string fileName = Path.GetFileName(file);
                string ext = Path.GetExtension(file);
                return !fileName.StartsWith("~$") &&
                       _extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
            })
            .ToArray();
        if (excelFiles.Length == 0)
        {
            Debug.LogError("配置文件夹为空");
            return;
        }

        foreach (string excelFile in excelFiles)
        {
            ReadExcel(excelFile);
        }

        foreach (var excelConfig in _excelConfigs)
        {
            GenerateConfigJson(excelConfig);
        }

        _excelConfigs.Clear();
        AssetDatabase.Refresh();

        EditorApplication.delayCall += () =>
        {
            Debug.Log("主动导出完成！");
        };
    }

    // 读取Excel
    private static void ReadExcel(string excelFilePath)
    {
        using var stream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        string fileName = Path.GetFileNameWithoutExtension(excelFilePath);

        var result = reader.AsDataSet();
        var sheet = result.Tables[0];   // 获取第一个工作表
        var rowComment = sheet.Rows[0]; // 字段注释
        var row = sheet.Rows[1];        // 字段名
        var rowType = sheet.Rows[2];    // 字段类型

        object rowId = row.ItemArray[0];
        if (rowId.ToString() != "id")
        {
            Debug.LogError($"导出Configs错误！{fileName}表中第一列不是id！");
            return;
        }

        List<PropertyInfo> properties = new();
        for (var i = 0; i < row.ItemArray.Length; i++)
        {
            string comment = rowComment.ItemArray[i].ToString().Trim();
            string field = row.ItemArray[i].ToString().Trim();
            string type = rowType.ItemArray[i].ToString().Trim();

            if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(type)) break;

            properties.Add(new PropertyInfo
            {
                Name = field,
                Type = type,
                Comment = comment
            });
        }
        GenerateConfigClass(properties, fileName);
        _excelConfigs.Add(new ExcelConfig
        {
            ConfigName = fileName,
            Properties = properties,
            Sheet = sheet
        });
    }

    /// <summary>
    /// 生成配置类文件
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="configName"></param>
    private static void GenerateConfigClass(List<PropertyInfo> properties, string configName)
    {
        string filePath = Path.Combine(ExcelExporterSettings.Instance.CsFullPath, $"{configName}Config.cs");
        string fileDir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);
        if (File.Exists(filePath))
        {
            Debug.LogError($"配置类已存在！{filePath}");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"public class {configName}Config : BaseConfig");
        sb.AppendLine("{");
        foreach (var property in properties)
        {
            if (property.Name == "id") continue;
            if (!string.IsNullOrEmpty(property.Comment))
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {property.Comment}");
                sb.AppendLine("    /// </summary>");
            }
            sb.AppendLine($"    public {property.Type} {property.Name};");
        }
        sb.AppendLine("}");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// 生成配置JSON文件
    /// </summary>
    /// <param name="excelConfig"></param>
    private static void GenerateConfigJson(ExcelConfig excelConfig)
    {
        string filePath = Path.Combine(ExcelExporterSettings.Instance.JsonFullPath, $"{excelConfig.ConfigName}Config.json");
        string fileDir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);

        Dictionary<string, BaseConfig> rawDataDict = new();

        for (var i = 3; i < excelConfig.Sheet.Rows.Count; i++)
        {
            var row = excelConfig.Sheet.Rows[i];
            if (row == null) break;

            StringBuilder sb = new();
            sb.Append("{");
            for (var j = 0; j <= row.ItemArray.Length && j < excelConfig.Properties.Count; j++)
            {
                object item = row.ItemArray[j];
                if (item == null) continue;

                string value = item.ToString().Replace(@"\", @"\\");

                if (string.IsNullOrEmpty(value))
                {
                    Debug.LogWarning($"有空值！{excelConfig.ConfigName}表中第{i + 1}行第{j + 1}列（字段名：{excelConfig.Properties[j].Name}）的值为空！");
                }

                if (excelConfig.Properties[j].Type == "bool")
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        value = "0";
                    }

                    // 布尔类型
                    if (value.ToLower() != "true" && value.ToLower() != "false")
                    {
                        value = value == "0" ? "false" : "true";
                    }
                    else
                    {
                        value = value.ToLower(); // "TRUE" -> "true", "FALSE" -> "false"
                    }

                    sb.Append($"\"{excelConfig.Properties[j].Name}\":{value}");
                }
                else if (excelConfig.Properties[j].Type == "int"
                         || excelConfig.Properties[j].Type == "long"
                         || excelConfig.Properties[j].Type == "float"
                         || excelConfig.Properties[j].Type == "double"
                         || excelConfig.Properties[j].Type == "decimal"
                         || excelConfig.Properties[j].Type == "byte"
                         || excelConfig.Properties[j].Type == "short"
                         || excelConfig.Properties[j].Type == "uint"
                         || excelConfig.Properties[j].Type == "ulong"
                         || excelConfig.Properties[j].Type == "sbyte"
                         || excelConfig.Properties[j].Type == "ushort"
                         || excelConfig.Properties[j].Type == "char"
                )
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        value = "0";
                    }

                    // 数值类型
                    sb.Append($"\"{excelConfig.Properties[j].Name}\":{value}");
                }
                else if (excelConfig.Properties[j].Type.Contains("[]"))
                {
                    // 处理数组类型（支持数字数组和字符串数组）
                    if (excelConfig.Properties[j].Type == "string[]")
                    {
                        // 使用 ParseCsvStyleArray 解析复杂字符串
                        var parsedValues = ParseCsvStyleArray(value);
                        var escapedValues = parsedValues
                            .Select(s => $"\"{s.Replace("\"", "\\\"")}\""); // 转义双引号
                        sb.Append($"\"{excelConfig.Properties[j].Name}\":[{string.Join(",", escapedValues)}]");
                    }
                    else
                    {
                        // 其他数组（如 int[]、float[]）
                        sb.Append($"\"{excelConfig.Properties[j].Name}\":[{value}]");
                    }
                }
                else
                {
                    // 字符串类型
                    value = value.Replace("\"", "\\\""); // 转义双引号
                    sb.Append($"\"{excelConfig.Properties[j].Name}\":\"{value}\"");
                }

                if (j < row.ItemArray.Length - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("}");

            var type = Type.GetType($"{excelConfig.ConfigName}Config, Assembly-CSharp");
            if (type == null)
            {
                Debug.LogError($"找不到类型: {excelConfig.ConfigName}Config，可能没有编译完成");
                continue;
            }

            try
            {
                if (JsonConvert.DeserializeObject(sb.ToString(), type) is not BaseConfig config || string.IsNullOrEmpty(config.id)) continue;
                rawDataDict.Add(config.id, config);
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析错误: {ex}");
                break;
            }
        }

        string json = JsonConvert.SerializeObject(rawDataDict, _jsonSerializerSettings);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    /// <summary>
    /// 解析类似 CSV 的字符串数组（支持引号包裹的逗号）
    /// </summary>
    private static List<string> ParseCsvStyleArray(string input)
    {
        if (string.IsNullOrEmpty(input)) return new List<string>();

        var result = new List<string>();
        var inQuotes = false;
        var currentItem = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '"')
            {
                // 检查是否是转义的引号（如 `""`）
                if (i + 1 < input.Length && input[i + 1] == '"')
                {
                    currentItem.Append('"');
                    i++; // 跳过下一个引号
                }
                else
                {
                    inQuotes = !inQuotes; // 进入/退出引号模式
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 遇到逗号且不在引号内，分割当前元素
                result.Add(currentItem.ToString());
                currentItem.Clear();
            }
            else
            {
                currentItem.Append(c);
            }
        }

        // 添加最后一个元素
        if (currentItem.Length > 0)
        {
            result.Add(currentItem.ToString());
        }

        return result;
    }

    /// <summary>
    /// 删除所有旧文件
    /// </summary>
    private static void DeleteAllOldFiles()
    {
        var settings = ExcelExporterSettings.Instance;

        if (Directory.Exists(settings.CsFullPath)) Directory.Delete(settings.CsFullPath, true);
        if (Directory.Exists(settings.JsonFullPath)) Directory.Delete(settings.JsonFullPath, true);
        Directory.CreateDirectory(settings.CsFullPath);
        Directory.CreateDirectory(settings.JsonFullPath);
    }
}
