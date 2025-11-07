namespace EntityGenerator.Models;

/// <summary>
/// 實體資訊
/// </summary>
public class EntityInfo
{
    /// <summary>
    /// Sheet 名稱
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// 資料夾名稱
    /// </summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// 模組名稱
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 表身名稱（如果是表身）
    /// </summary>
    public string? DetailName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Prefix（從欄位名稱提取）
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// 是否為表身
    /// </summary>
    public bool IsDetail => !string.IsNullOrEmpty(DetailName);

    /// <summary>
    /// 類別名稱
    /// </summary>
    public string ClassName => $"CTab_{FolderName}{ModuleName}{DetailName}";

    /// <summary>
    /// 資料表名稱
    /// </summary>
    public string TableName => IsDetail
        ? $"{FolderName}_{ModuleName}_{DetailName}"
        : $"{FolderName}_{ModuleName}";

    /// <summary>
    /// 檔案名稱
    /// </summary>
    public string FileName => $"{ClassName}.cs";

    /// <summary>
    /// 檔案路徑（相對於專案根目錄）
    /// </summary>
    public string FilePath => $"NET_Core_API/Entity_Model/Entity/{FolderName}/{ModuleName}/{FileName}";

    /// <summary>
    /// Namespace
    /// </summary>
    public string Namespace => $"Entity_Model.Entity.{FolderName}.{ModuleName}";

    /// <summary>
    /// 所有欄位
    /// </summary>
    public List<FieldInfo> Fields { get; set; } = new();

    /// <summary>
    /// 主鍵欄位
    /// </summary>
    public FieldInfo? PrimaryKeyField => Fields.FirstOrDefault(f => f.IsPrimaryKey);

    /// <summary>
    /// 業務欄位（排除主鍵和建立者/異動者欄位）
    /// </summary>
    public List<FieldInfo> BusinessFields => Fields
        .Where(f => !f.IsPrimaryKey && !IsSystemField(f.Name))
        .ToList();

    /// <summary>
    /// 建立者欄位
    /// </summary>
    public List<FieldInfo> CreatorFields => Fields
        .Where(f => f.Name.Contains("_CreateId") ||
                    f.Name.Contains("_CreateCode") ||
                    f.Name.Contains("_CreateDate") ||
                    f.Name.Contains("_CreateIp"))
        .ToList();

    /// <summary>
    /// 異動者欄位
    /// </summary>
    public List<FieldInfo> EditorFields => Fields
        .Where(f => f.Name.Contains("_EditId") ||
                    f.Name.Contains("_EditCode") ||
                    f.Name.Contains("_EditDate") ||
                    f.Name.Contains("_EditIp"))
        .ToList();

    /// <summary>
    /// 外鍵欄位（如果是表身）
    /// </summary>
    public FieldInfo? ForeignKeyField => IsDetail
        ? Fields.FirstOrDefault(f => f.Name.StartsWith("FK_") || f.Name.StartsWith("Fk_") || f.Name.StartsWith("CFK_"))
        : null;

    /// <summary>
    /// 判斷是否為系統欄位
    /// </summary>
    private bool IsSystemField(string fieldName)
    {
        return fieldName.Contains("_CreateId") ||
               fieldName.Contains("_CreateCode") ||
               fieldName.Contains("_CreateDate") ||
               fieldName.Contains("_CreateIp") ||
               fieldName.Contains("_EditId") ||
               fieldName.Contains("_EditCode") ||
               fieldName.Contains("_EditDate") ||
               fieldName.Contains("_EditIp");
    }
}
