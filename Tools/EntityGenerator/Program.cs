using EntityGenerator.Services;

namespace EntityGenerator;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("========================================");
        Console.WriteLine("Entity 自動生成工具（Excel 版）");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // 檢查參數
        if (args.Length == 0)
        {
            Console.WriteLine("使用方式：");
            Console.WriteLine("  模式 1：生成 Entity 檔案");
            Console.WriteLine("    dotnet run -- <Excel檔案路徑>");
            Console.WriteLine();
            Console.WriteLine("  模式 2：輸出 JSON 到 stdout");
            Console.WriteLine("    dotnet run -- <Excel檔案路徑> --json");
            Console.WriteLine();
            Console.WriteLine("  模式 3：輸出 JSON 到檔案");
            Console.WriteLine("    dotnet run -- <Excel檔案路徑> --output-json <JSON檔案路徑>");
            Console.WriteLine();
            Console.WriteLine("範例：");
            Console.WriteLine("  dotnet run -- \"Files/保健安資料結構.xlsx\"");
            Console.WriteLine("  dotnet run -- \"Files/保健安資料結構.xlsx\" --json");
            Console.WriteLine("  dotnet run -- \"Files/保健安資料結構.xlsx\" --output-json \"entities.json\"");
            return;
        }

        var excelFilePath = args[0];

        // 解析命令列參數
        bool isJsonMode = args.Contains("--json");
        string? jsonOutputPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--output-json" && i + 1 < args.Length)
            {
                jsonOutputPath = args[i + 1];
                isJsonMode = true;
                break;
            }
        }

        // 檢查檔案是否存在
        if (!File.Exists(excelFilePath))
        {
            Console.WriteLine($"❌ 錯誤：找不到檔案：{excelFilePath}");
            return;
        }

        Console.WriteLine($"正在讀取 Excel 檔案：{excelFilePath}");
        Console.WriteLine();

        try
        {
            // 讀取 Excel
            var excelReader = new ExcelReader();
            var entities = excelReader.ReadExcelFile(excelFilePath);

            Console.WriteLine($"共解析到 {entities.Count} 個 Sheet（實體）");
            Console.WriteLine();

            // 根據模式執行不同邏輯
            if (isJsonMode)
            {
                // JSON 模式
                var jsonExporter = new JsonExporter();

                if (!string.IsNullOrEmpty(jsonOutputPath))
                {
                    // 輸出到指定檔案
                    jsonExporter.ExportToFile(entities, jsonOutputPath);
                }
                else
                {
                    // 輸出到 stdout
                    Console.WriteLine("========================================");
                    Console.WriteLine("JSON 輸出：");
                    Console.WriteLine("========================================");
                    Console.WriteLine();
                    jsonExporter.ExportToConsole(entities);
                }
            }
            else
            {
                // Entity 生成模式（原有功能）
                Console.WriteLine("開始生成 Entity 檔案...");
                Console.WriteLine();

                // 取得專案根目錄（從 bin/Debug/net8.0 往上到專案根目錄）
                // bin/Debug/net8.0 -> EntityGenerator -> Tools -> 專案根目錄
                var projectRootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../.."));

                Console.WriteLine($"[偵錯] 專案根目錄：{projectRootPath}");
                Console.WriteLine();

                // 生成 Entity 檔案
                var generator = new EntityFileGenerator(projectRootPath);

            foreach (var entity in entities)
            {
                Console.WriteLine("========================================");
                Console.WriteLine($"正在分析實體：{entity.Description}");
                Console.WriteLine($"- Sheet 名稱: {entity.SheetName}");
                Console.WriteLine($"- FolderName: {entity.FolderName}");
                Console.WriteLine($"- ModuleName: {entity.ModuleName}");
                if (entity.IsDetail)
                {
                    Console.WriteLine($"- DetailName: {entity.DetailName}");
                }
                Console.WriteLine($"- Prefix: {entity.Prefix}");
                Console.WriteLine($"- 檔案名稱: {entity.FileName}");
                Console.WriteLine($"- 檔案路徑: {entity.FilePath}");
                Console.WriteLine($"- 主鍵型別: {entity.PrimaryKeyField?.CSharpType ?? "無"}");
                Console.WriteLine($"- 是否為表身: {entity.IsDetail}");

                if (entity.IsDetail && entity.ForeignKeyField != null)
                {
                    Console.WriteLine($"- 外鍵欄位: {entity.ForeignKeyField.Name}");
                }

                Console.WriteLine("業務欄位：");
                foreach (var field in entity.BusinessFields)
                {
                    var nullable = field.IsNullable && field.CSharpType != "string" ? "?" : "";
                    Console.WriteLine($"  - {field.Name} ({field.CSharpType}{nullable}) - {field.Comment}");
                }

                Console.WriteLine("建立者/異動者欄位：");
                foreach (var field in entity.CreatorFields.Concat(entity.EditorFields))
                {
                    var nullable = field.IsNullable && field.CSharpType != "string" ? "?" : "";
                    Console.WriteLine($"  - {field.Name} ({field.CSharpType}{nullable}) - {field.Comment}");
                }

                Console.WriteLine();
                Console.WriteLine("[生成檔案...]");

                // 生成 Entity 檔案
                generator.GenerateEntityFile(entity);

                Console.WriteLine("---");
                Console.WriteLine("已完成，開始生成下一個實體");
                Console.WriteLine("---");
                Console.WriteLine();
            }

                // 輸出 DbSet 註冊程式碼
                generator.GenerateDbSetCode(entities);

                Console.WriteLine("========================================");
                Console.WriteLine("✓ 所有 Entity 生成完畢！");
                Console.WriteLine("========================================");
                Console.WriteLine();
                Console.WriteLine("後續步驟：");
                Console.WriteLine("1. 檢查生成的 Entity 檔案");
                Console.WriteLine("2. 將 DbSet 註冊程式碼加入 CEntityContext.cs");
                Console.WriteLine("3. 執行專案編譯確認無誤");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ 發生錯誤：{ex.Message}");
            Console.WriteLine();
            Console.WriteLine("詳細錯誤：");
            Console.WriteLine(ex.ToString());
        }
    }
}
