using EntityGenerator.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EntityGenerator.Services;

/// <summary>
/// JSON 輸出服務
/// </summary>
public class JsonExporter
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonExporter()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true, // 格式化輸出
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 支援中文不轉義
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // 忽略 null 值
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // 使用 camelCase 命名
        };
    }

    /// <summary>
    /// 輸出 JSON 到 stdout
    /// </summary>
    public void ExportToConsole(List<EntityInfo> entities)
    {
        var json = JsonSerializer.Serialize(entities, _jsonOptions);
        Console.WriteLine(json);
    }

    /// <summary>
    /// 輸出 JSON 到檔案
    /// </summary>
    public void ExportToFile(List<EntityInfo> entities, string filePath)
    {
        var json = JsonSerializer.Serialize(entities, _jsonOptions);
        File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

        Console.WriteLine($"✓ JSON 已輸出到：{filePath}");
        Console.WriteLine($"  共 {entities.Count} 個實體");
    }
}
