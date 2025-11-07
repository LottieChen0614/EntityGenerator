using EntityGenerator.Models;
using System.Text;

namespace EntityGenerator.Services;

/// <summary>
/// Entity 檔案生成服務
/// </summary>
public class EntityFileGenerator
{
    private readonly string _projectRootPath;

    public EntityFileGenerator(string projectRootPath)
    {
        _projectRootPath = projectRootPath;
    }

    /// <summary>
    /// 生成 Entity 檔案
    /// </summary>
    public void GenerateEntityFile(EntityInfo entity)
    {
        var content = GenerateEntityContent(entity);
        var fullPath = Path.Combine(_projectRootPath, entity.FilePath);

        Console.WriteLine($"[偵錯] 完整路徑：{fullPath}");

        // 建立目錄
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Console.WriteLine($"[偵錯] 已建立目錄：{directory}");
        }

        // 寫入檔案（UTF-8 with BOM）
        File.WriteAllText(fullPath, content, new UTF8Encoding(true));

        Console.WriteLine($"✓ 已生成：{entity.FilePath}");
    }

    /// <summary>
    /// 生成 Entity 類別內容
    /// </summary>
    private string GenerateEntityContent(EntityInfo entity)
    {
        var sb = new StringBuilder();

        // using 區塊
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using Project_Model;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");

        // 如果是表身且有外鍵，加入 JsonIgnore
        if (entity.IsDetail && entity.ForeignKeyField != null)
        {
            sb.AppendLine("using System.Text.Json.Serialization;");
        }

        sb.AppendLine();

        // namespace
        sb.AppendLine($"namespace {entity.Namespace}");
        sb.AppendLine("{");

        // 類別 XML 註解
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// {entity.Description}");
        sb.AppendLine("    /// </summary>");

        // Table 屬性
        sb.AppendLine($"    [Table(\"{entity.TableName}\")]");
        sb.AppendLine($"    [Comment(\"{entity.Description}\")]");

        // 類別定義
        sb.AppendLine($"    public class {entity.ClassName}");
        sb.AppendLine("    {");

        // 表身外鍵關聯區塊
        if (entity.IsDetail && entity.ForeignKeyField != null)
        {
            sb.AppendLine(GenerateForeignKeyRegion(entity));
        }

        // 主鍵欄位
        if (entity.PrimaryKeyField != null)
        {
            sb.Append(GenerateFieldCode(entity.PrimaryKeyField, isKey: true));
        }

        // 業務欄位
        foreach (var field in entity.BusinessFields)
        {
            sb.AppendLine();
            sb.Append(GenerateFieldCode(field));
        }

        // 建立者欄位
        foreach (var field in entity.CreatorFields)
        {
            sb.AppendLine();
            sb.Append(GenerateFieldCode(field));
        }

        // 異動者欄位
        foreach (var field in entity.EditorFields)
        {
            sb.AppendLine();
            sb.Append(GenerateFieldCode(field));
        }

        // 類別結束
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成外鍵關聯區塊
    /// </summary>
    private string GenerateForeignKeyRegion(EntityInfo entity)
    {
        var sb = new StringBuilder();

        sb.AppendLine("        #region 資料庫關聯");
        sb.AppendLine();
        sb.AppendLine("        #region 外來鍵，[ForeignKey(\"外來鍵參照欄位\")]，若沒設定ForeignKey Code First會自動生成欄位");
        sb.AppendLine();

        // 計算 MasterModuleName
        var masterModuleName = entity.ModuleName;

        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// {entity.FolderName}{masterModuleName}");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        [ForeignKey(\"{entity.ForeignKeyField!.Name}\")]");
        sb.AppendLine("        [JsonIgnore]");
        sb.AppendLine($"        public virtual CTab_{entity.FolderName}{masterModuleName} CTab_{entity.FolderName}{masterModuleName} {{ get; set; }}");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();
        sb.AppendLine("        #endregion");

        return sb.ToString();
    }

    /// <summary>
    /// 生成欄位程式碼
    /// </summary>
    private string GenerateFieldCode(FieldInfo field, bool isKey = false)
    {
        var sb = new StringBuilder();

        // XML 註解
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// {field.Comment}");
        sb.AppendLine("        /// </summary>");

        if (!string.IsNullOrEmpty(field.Example))
        {
            sb.AppendLine($"        /// <example>{field.Example}</example>");
        }

        // 主鍵屬性
        if (isKey)
        {
            sb.AppendLine("        [Key]");
            sb.AppendLine("        [DatabaseGenerated(DatabaseGeneratedOption.None)]");
        }

        // Column 屬性
        sb.AppendLine($"        [Column(\"{field.Name}\", TypeName = {field.TypeName})]");

        // Comment 屬性：如果有 CommentExtra 才加上
        var commentText = string.IsNullOrEmpty(field.CommentExtra)
            ? field.Comment
            : $"{field.Comment}：「{field.CommentExtra}」";
        sb.AppendLine($"        [Comment(\"{commentText}\")]");

        // 屬性定義
        // 根據 IsNullable 決定是否加 ?（包括 string）
        var nullableSign = field.IsNullable ? "?" : "";
        var defaultValue = GetDefaultValue(field);

        sb.AppendLine($"        public {field.CSharpType}{nullableSign} {field.Name} {{ get; set; }}{defaultValue}");

        return sb.ToString();
    }

    /// <summary>
    /// 取得預設值
    /// </summary>
    private string GetDefaultValue(FieldInfo field)
    {
        // 非必填的 string 不需要預設值
        if (field.CSharpType == "string" && field.IsNullable)
        {
            return "";
        }

        // 必填的 string 需要預設值
        if (field.CSharpType == "string" && !field.IsNullable)
        {
            return " = string.Empty;";
        }

        return "";
    }

    /// <summary>
    /// 生成 DbSet 註冊程式碼（輸出到控制台）
    /// </summary>
    public void GenerateDbSetCode(List<EntityInfo> entities)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("DbSet 註冊程式碼（請手動加入 CEntityContext.cs）：");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var groupedByFolder = entities.GroupBy(e => e.FolderName);

        foreach (var group in groupedByFolder)
        {
            Console.WriteLine($"    #region {group.Key}相關");
            Console.WriteLine();

            foreach (var entity in group)
            {
                Console.WriteLine("    /// <summary>");
                Console.WriteLine($"    /// {entity.Description}");
                Console.WriteLine("    /// </summary>");
                Console.WriteLine($"    public DbSet<{entity.ClassName}> {entity.ClassName} {{ get; set; }}");
                Console.WriteLine();
            }

            Console.WriteLine("    #endregion");
            Console.WriteLine();
        }
    }
}
