using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

public class EditorUtils
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
        public ISheet Sheet { get; set; }
    }

    private static readonly List<ExcelConfig> _excelConfigs = new();
    private static readonly string[] _extensions = {".xlsx", ".xls"};
    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        Formatting = Formatting.Indented,
    };

    [MenuItem("Simple Toolkit/Excel To Json")]
    public static void GenerateConfigs()
    {
        DeleteAllOldFiles();

        string excelDirPath = SimpleToolkitSettings.Instance.ExcelRelativePath;
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
        AssetDatabase.Refresh();

        foreach (var excelConfig in _excelConfigs)
        {
            GenerateConfigJson(excelConfig);
        }
        AssetDatabase.Refresh();

        EditorApplication.delayCall += () =>
        {
            foreach (var excelConfig in _excelConfigs)
            {
                GenerateConfigJson(excelConfig);
            }

            AssetDatabase.Refresh();
            _excelConfigs.Clear();
            Debug.Log("主动导出完成！");
        };
    }

    // 读取Excel
    private static void ReadExcel(string excelFilePath)
    {
        IWorkbook wk;
        string extension = Path.GetExtension(excelFilePath);
        string fileName = Path.GetFileNameWithoutExtension(excelFilePath);
        _excelConfigs.Clear();

        using var stream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (extension.Equals(".xls"))
        {
            wk = new HSSFWorkbook(stream);
        }
        else
        {
            wk = new XSSFWorkbook(stream);
        }

        for (var i = 0; i < wk.NumberOfSheets; i++)
        {
            // 读取第i个工作表
            var sheet = wk.GetSheetAt(i);
            ReadExcelSheets(sheet, fileName);
        }
    }

    private static void ReadExcelSheets(ISheet sheet, string fileName)
    {
        var rowComment = sheet.GetRow(0); // 字段注释
        var row = sheet.GetRow(1);        // 字段名
        var rowType = sheet.GetRow(2);    // 字段类型

        object rowId = row.GetCell(0);
        if (rowId.ToString() != "id")
        {
            Debug.LogError($"导出Configs错误！{fileName} - {sheet.SheetName}表中第一列不是id！");
            return;
        }

        List<PropertyInfo> properties = new();
        for (var i = 0; i < row.LastCellNum; i++)
        {
            string comment = rowComment.GetCell(i).ToString().Trim();
            string field = row.GetCell(i).ToString().Trim();
            string type = rowType.GetCell(i).ToString().Trim();

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
        string filePath = Path.Combine(SimpleToolkitSettings.Instance.CsRelativePath, $"{configName}Config.cs");
        string fileDir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(fileDir)) fileDir = Path.Combine(Application.dataPath, "Scripts", "Configs");
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
        string filePath = Path.Combine(SimpleToolkitSettings.Instance.JsonRelativePath, $"{excelConfig.ConfigName}Config.json");
        string fileDir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(fileDir)) fileDir = Path.Combine(Application.dataPath, "Resources", "JsonConfigs");
        if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);

        Dictionary<string, BaseConfig> rawDataDict = new();

        for (var i = 3; i <= excelConfig.Sheet.LastRowNum; i++)
        {
            var row = excelConfig.Sheet.GetRow(i);
            if (row == null) break;

            StringBuilder sb = new();
            sb.Append("{");

            for (var j = 0; j <= row.LastCellNum && j < excelConfig.Properties.Count; j++)
            {
                var cell = row.GetCell(j);
                if (cell == null) continue;

                string value;
                if (cell.CellType == CellType.Formula)
                {
                    value = cell.CachedFormulaResultType == CellType.Numeric
                        ? cell.NumericCellValue.ToString(NumberFormatInfo.CurrentInfo)
                        : cell.StringCellValue.Replace(@"\", @"\\");
                }
                else
                {
                    value = cell.ToString().Replace(@"\", @"\\");
                }

                if (string.IsNullOrEmpty(value))
                {
                    if (excelConfig.Properties[j].Type != "string" && excelConfig.Properties[j].Type != "string[]")
                    {
                        value = "0";
                    }
                    Debug.LogWarning($"有空值！{excelConfig.ConfigName} - {excelConfig.Sheet.SheetName}表中第{i + 1}行第{j + 1}列（字段名：{excelConfig.Properties[j].Name}）的值为空！");
                }

                if (excelConfig.Properties[j].Type == "bool")
                {
                    // 布尔类型
                    if (value.ToLower() != "true" && value.ToLower() != "false")
                    {
                        value = value == "0" ? "false" : "true";
                    }
                    else
                    {
                        value = value.ToLower(); // "TRUE" -> "true", "FALSE" -> "false"
                    }
                }

                if (excelConfig.Properties[j].Type.Contains("[]"))
                {
                    // 处理数组类型（支持数字数组和字符串数组）
                    if (excelConfig.Properties[j].Type == "string[]")
                    {
                        // 使用 ParseCsvStyleArray 解析复杂字符串
                        var parsedValues = ParseCsvStyleArray(value);
                        var escapedValues = parsedValues
                            .Select(s => $"\"{s.Replace("\"", "\\\"")}\""); // 转义双引号
                        value = $"[{string.Join(",", escapedValues.ToArray())}]";
                    }
                    else
                    {
                        // 其他数组（如 int[]、float[]）
                        value = $"[{value}]";
                    }
                }
                else
                {
                    // 字符串类型
                    value = value.Replace("\"", "\\\""); // 转义双引号
                    value = $"\"{value}\"";
                }

                sb.Append($"\"{excelConfig.Properties[j].Name}\":{value}");
                if (j < row.LastCellNum - 1) sb.Append(",");
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
                result.Add(currentItem.ToString().Trim());
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
            result.Add(currentItem.ToString().Trim());
        }

        return result;
    }

    /// <summary>
    /// 删除所有旧文件
    /// </summary>
    private static void DeleteAllOldFiles()
    {
        var settings = SimpleToolkitSettings.Instance;

        if (Directory.Exists(settings.CsRelativePath)) Directory.Delete(settings.CsRelativePath, true);
        if (Directory.Exists(settings.JsonRelativePath)) Directory.Delete(settings.JsonRelativePath, true);
        Directory.CreateDirectory(settings.CsRelativePath);
        Directory.CreateDirectory(settings.JsonRelativePath);
    }
}
