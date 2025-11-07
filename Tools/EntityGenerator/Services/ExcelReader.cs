using EntityGenerator.Models;
using OfficeOpenXml;
using System.Text.RegularExpressions;

namespace EntityGenerator.Services;

/// <summary>
/// Excel 讀取服務
/// </summary>
public class ExcelReader
{
    /// <summary>
    /// 讀取 Excel 檔案並解析所有 Sheet
    /// </summary>
    public List<EntityInfo> ReadExcelFile(string excelFilePath)
    {
        var entities = new List<EntityInfo>();

        // 設定 EPPlus 授權模式
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage(new FileInfo(excelFilePath));

        foreach (var worksheet in package.Workbook.Worksheets)
        {
            var entity = ParseSheet(worksheet);
            if (entity != null)
            {
                entities.Add(entity);
            }
        }

        return entities;
    }

    /// <summary>
    /// 解析單一 Sheet
    /// </summary>
    private EntityInfo? ParseSheet(ExcelWorksheet worksheet)
    {
        var sheetName = worksheet.Name;

        // 解析 Sheet 名稱：{FolderName}_{ModuleName}({Description})
        // 正常格式：Bga_Material(料件項目)
        var match = Regex.Match(sheetName, @"^([^_]+)_(.+?)\((.+?)\)$");

        // 容錯機制：如果沒有右括號（Excel Sheet 名稱字數限制），只抓左括號後的內容作為 Description
        if (!match.Success)
        {
            // 嘗試容錯格式：{FolderName}_{ModuleName}({Description
            var fallbackMatch = Regex.Match(sheetName, @"^([^_]+)_(.+?)\((.+)$");
            if (fallbackMatch.Success)
            {
                Console.WriteLine($"警告：Sheet 名稱缺少右括號，使用容錯模式：{sheetName}");
                var fallbackEntity = new EntityInfo
                {
                    SheetName = sheetName,
                    FolderName = fallbackMatch.Groups[1].Value,
                    Description = fallbackMatch.Groups[3].Value
                };

                // 解析 ModuleName 和 DetailName
                var fallbackModulePart = fallbackMatch.Groups[2].Value;
                if (fallbackModulePart.Contains('_'))
                {
                    var parts = fallbackModulePart.Split('_');
                    fallbackEntity.ModuleName = parts[0];
                    fallbackEntity.DetailName = string.Join("", parts.Skip(1));
                }
                else
                {
                    fallbackEntity.ModuleName = fallbackModulePart;
                }

                // 讀取欄位定義
                fallbackEntity.Fields = ParseFields(worksheet);

                // 提取 Prefix
                fallbackEntity.Prefix = ExtractPrefix(fallbackEntity.Fields);

                // 修正建立者/異動者欄位的型別
                FixSystemFields(fallbackEntity);

                return fallbackEntity;
            }

            Console.WriteLine($"警告：跳過無法解析的 Sheet 名稱：{sheetName}");
            return null;
        }

        var normalEntity = new EntityInfo
        {
            SheetName = sheetName,
            FolderName = match.Groups[1].Value,
            Description = match.Groups[3].Value
        };

        // 解析 ModuleName 和 DetailName
        var modulePart = match.Groups[2].Value;
        if (modulePart.Contains('_'))
        {
            var parts = modulePart.Split('_');
            normalEntity.ModuleName = parts[0];
            normalEntity.DetailName = string.Join("", parts.Skip(1)); // 移除底線，組合成 PascalCase
        }
        else
        {
            normalEntity.ModuleName = modulePart;
        }

        // 讀取欄位定義
        normalEntity.Fields = ParseFields(worksheet);

        // 提取 Prefix
        normalEntity.Prefix = ExtractPrefix(normalEntity.Fields);

        // 修正建立者/異動者欄位的型別
        FixSystemFields(normalEntity);

        return normalEntity;
    }

    /// <summary>
    /// 讀取欄位定義
    /// </summary>
    private List<FieldInfo> ParseFields(ExcelWorksheet worksheet)
    {
        var fields = new List<FieldInfo>();

        // 假設第一行是標題行
        // | Column Name | Column TypeName | 資料長度 | Comment | Comment補充說明 | Example | PK | Required | 備註 |
        //      1              2               3          4            5              6      7      8        9

        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            var columnName = worksheet.Cells[row, 1].Value?.ToString();
            if (string.IsNullOrWhiteSpace(columnName))
                continue;

            var sqlType = worksheet.Cells[row, 2].Value?.ToString() ?? "";
            var length = worksheet.Cells[row, 3].Value?.ToString();
            var comment = worksheet.Cells[row, 4].Value?.ToString() ?? "";
            var commentExtra = worksheet.Cells[row, 5].Value?.ToString();
            var example = worksheet.Cells[row, 6].Value?.ToString();
            var isPK = worksheet.Cells[row, 7].Value?.ToString() == "1";
            var isRequired = worksheet.Cells[row, 8].Value?.ToString() == "1";
            var remark = worksheet.Cells[row, 9].Value?.ToString();

            var field = new FieldInfo
            {
                Name = columnName,
                SqlType = sqlType,
                Length = length,
                Comment = comment,
                CommentExtra = commentExtra,
                Example = example,
                IsPrimaryKey = isPK,
                IsRequired = isRequired,
                Remark = remark
            };

            // 轉換 SQL 型別為 C# 型別
            ConvertSqlTypeToCSharp(field);

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>
    /// 轉換 SQL Server 型別為 C# 型別
    /// </summary>
    private void ConvertSqlTypeToCSharp(FieldInfo field)
    {
        var sqlType = field.SqlType.ToLower();

        // 判斷 C# 型別
        if (sqlType.Contains("bigint"))
        {
            field.CSharpType = "long";
        }
        else if (sqlType.Contains("int"))
        {
            field.CSharpType = "int";
        }
        else if (sqlType.Contains("datetime"))
        {
            field.CSharpType = "long"; // datetime 轉為 Ticks
        }
        else if (sqlType.Contains("decimal"))
        {
            field.CSharpType = "decimal";
        }
        else if (sqlType.Contains("bit"))
        {
            field.CSharpType = "bool";
        }
        else if (sqlType.Contains("uniqueidentifier"))
        {
            field.CSharpType = "Guid";
        }
        else if (sqlType.Contains("nvarchar") || sqlType.Contains("varchar") || sqlType.Contains("char"))
        {
            field.CSharpType = "string";
        }
        else
        {
            field.CSharpType = "string"; // 預設
        }

        // 判斷 TypeName（業務欄位一律使用字串型別）
        if (field.IsPrimaryKey)
        {
            // 主鍵使用 PropertyConfig.TableID（bigint 或 uniqueidentifier）
            if (sqlType.Contains("bigint") || sqlType.Contains("uniqueidentifier"))
            {
                field.TypeName = "PropertyConfig.TableID";
            }
            else if (sqlType.Contains("nvarchar"))
            {
                // 字串主鍵保持原型別
                var lengthMatch = Regex.Match(sqlType, @"nvarchar\((\d+)\)");
                var length = lengthMatch.Success ? lengthMatch.Groups[1].Value : "50";
                field.TypeName = $"\"nvarchar({length})\"";
            }
        }
        else
        {
            // 業務欄位使用字串型別
            if (sqlType.Contains("bigint"))
            {
                field.TypeName = "\"bigint\"";
            }
            else if (sqlType.Contains("int"))
            {
                field.TypeName = "\"int\"";
            }
            else if (sqlType.Contains("datetime"))
            {
                field.TypeName = "\"bigint\""; // datetime 存為 bigint
            }
            else if (sqlType.Contains("decimal"))
            {
                var decimalMatch = Regex.Match(sqlType, @"decimal\((\d+),(\d+)\)");
                if (decimalMatch.Success)
                {
                    field.TypeName = $"\"decimal({decimalMatch.Groups[1].Value},{decimalMatch.Groups[2].Value})\"";
                }
                else
                {
                    field.TypeName = "\"decimal(18,2)\"";
                }
            }
            else if (sqlType.Contains("bit"))
            {
                field.TypeName = "\"bit\"";
            }
            else if (sqlType.Contains("uniqueidentifier"))
            {
                field.TypeName = "\"uniqueidentifier\"";
            }
            else if (sqlType.Contains("nvarchar(1)"))
            {
                field.TypeName = "\"character(1)\""; // 狀態欄位
            }
            else if (sqlType.Contains("nvarchar"))
            {
                var lengthMatch = Regex.Match(sqlType, @"nvarchar\((\d+)\)");
                var length = lengthMatch.Success ? lengthMatch.Groups[1].Value : "50";
                field.TypeName = $"\"nvarchar({length})\"";
            }
            else
            {
                field.TypeName = $"\"{sqlType}\"";
            }
        }

        // 判斷是否可為 null
        field.IsNullable = !field.IsRequired;
    }

    /// <summary>
    /// 提取 Prefix（從第一個非主鍵業務欄位）
    /// </summary>
    private string ExtractPrefix(List<FieldInfo> fields)
    {
        var firstBusinessField = fields.FirstOrDefault(f => !f.IsPrimaryKey && f.Name.Contains('_'));
        if (firstBusinessField != null)
        {
            return firstBusinessField.Name.Split('_')[0];
        }

        // 如果找不到，從主鍵提取
        var pkField = fields.FirstOrDefault(f => f.IsPrimaryKey);
        if (pkField != null && pkField.Name.StartsWith("PK_"))
        {
            return pkField.Name.Substring(3); // 移除 "PK_"
        }

        return "Unknown";
    }

    /// <summary>
    /// 修正建立者/異動者欄位的型別
    /// </summary>
    private void FixSystemFields(EntityInfo entity)
    {
        foreach (var field in entity.Fields)
        {
            // 建立者欄位（必填）
            if (field.Name.EndsWith("_CreateId"))
            {
                field.CSharpType = "long";
                field.TypeName = "PropertyConfig.TableID";
                field.IsNullable = false;
            }
            else if (field.Name.EndsWith("_CreateCode"))
            {
                field.CSharpType = "string";
                field.TypeName = "PropertyConfig.TableCode";
                field.IsNullable = false;
            }
            else if (field.Name.EndsWith("_CreateDate"))
            {
                field.CSharpType = "long";
                field.TypeName = "PropertyConfig.TableTime";
                field.IsNullable = false;
            }
            else if (field.Name.EndsWith("_CreateIp"))
            {
                field.CSharpType = "string";
                field.TypeName = "PropertyConfig.TableIP";
                field.IsNullable = false;
            }
            // 異動者欄位（非必填）
            else if (field.Name.EndsWith("_EditId"))
            {
                field.CSharpType = "long";
                field.TypeName = "PropertyConfig.TableID";
                field.IsNullable = true;
            }
            else if (field.Name.EndsWith("_EditCode"))
            {
                field.CSharpType = "string";
                field.TypeName = "PropertyConfig.TableCode";
                field.IsNullable = true;
            }
            else if (field.Name.EndsWith("_EditDate"))
            {
                field.CSharpType = "long";
                field.TypeName = "PropertyConfig.TableTime";
                field.IsNullable = true;
            }
            else if (field.Name.EndsWith("_EditIp"))
            {
                field.CSharpType = "string";
                field.TypeName = "PropertyConfig.TableIP";
                field.IsNullable = true;
            }
        }
    }
}
