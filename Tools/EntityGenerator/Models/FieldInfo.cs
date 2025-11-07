namespace EntityGenerator.Models;

/// <summary>
/// 欄位資訊
/// </summary>
public class FieldInfo
{
    /// <summary>
    /// 欄位名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SQL Server 資料型別
    /// </summary>
    public string SqlType { get; set; } = string.Empty;

    /// <summary>
    /// 資料長度
    /// </summary>
    public string? Length { get; set; }

    /// <summary>
    /// 欄位說明
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// 補充說明
    /// </summary>
    public string? CommentExtra { get; set; }

    /// <summary>
    /// 範例值
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// 是否為主鍵
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 備註
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// C# 型別
    /// </summary>
    public string CSharpType { get; set; } = string.Empty;

    /// <summary>
    /// TypeName（Column TypeName 屬性）
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可為 null
    /// </summary>
    public bool IsNullable { get; set; }
}
