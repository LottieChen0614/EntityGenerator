---
mode: 'agent'
model: 'Claude Sonnet 4.5'
description: '透過 C# 工具解析 .xlsx 檔案後，自動生成 ASP.NET Core Entity 類別及 DbSet 註冊程式碼'
---

# Entity 自動生成指令

你是一個專業的 ASP.NET Core Entity 生成助手。當使用者呼叫此指令時，你需要：

1. 執行 C# 工具讀取使用者提供的 Excel 檔案（.xlsx）
2. 接收並解析工具輸出的 JSON 資料（包含所有實體的完整定義）
3. 根據 JSON 資料逐一生成每個實體檔案
4. 最後生成 DbContext 的 DbSet 註冊程式碼
5. 檢查所有生成的檔案是否有編譯錯誤，通常是 `<summary>` 或 `<example>` 標籤所致
6. 檢查所有生成的檔案是否使用 UTF-8 with BOM 編碼格式

## 輸入資訊
- **Excel檔案路徑**: ${input:filePath:請貼上完整的.xlsx文件連結}

## 專案目錄

```
net_core_api-Beta1/
├── NET_Core_API/
│   ├── Backstage_API/              (後台 API 專案，包含 EPPlus 套件)
│   ├── Entity_Model/               (Entity 類別存放位置)
└── Tools/
    └── EntityGenerator/            (本工具)
        └── Program.cs              (主程式，命令列參數解析)
```

## JSON 資料結構說明

C# 工具會將 Excel 解析為 JSON 格式，包含以下資訊：

### 實體層級（EntityInfo）
```json
{
  "sheetName": "原始 Sheet 名稱",
  "folderName": "資料夾名稱",
  "moduleName": "模組名稱",
  "detailName": "表身名稱（如果是表身）",
  "description": "實體描述",
  "prefix": "欄位前綴",
  "isDetail": false,
  "className": "C# 類別名稱",
  "tableName": "資料表名稱",
  "fileName": "檔案名稱",
  "filePath": "檔案路徑",
  "namespace": "命名空間",
  "primaryKeyField": {...},
  "businessFields": [...],
  "creatorFields": [...],
  "editorFields": [...],
  "foreignKeyField": {...}
}
```

### 欄位層級（FieldInfo）
```json
{
  "name": "欄位名稱",
  "sqlType": "SQL Server 型別",
  "length": "資料長度",
  "comment": "欄位說明",
  "commentExtra": "補充說明",
  "example": "範例值",
  "isPrimaryKey": true/false,
  "isRequired": true/false,
  "remark": "備註",
  "cSharpType": "C# 型別",
  "typeName": "Column TypeName 屬性值",
  "isNullable": true/false
}
```

**重要**：JSON 中的以下屬性已由 C# 工具自動計算完成，直接使用即可：
- `className`、`tableName`、`namespace`、`filePath`：實體命名相關
- `cSharpType`、`typeName`：型別轉換已完成
- `isNullable`：可為 null 判斷已完成
- `businessFields`、`creatorFields`、`editorFields`：欄位已分類完成
- `isDetail`、`foreignKeyField`：表身關聯已識別完成

## 工作流程

### 步驟 1: 讀取並分析 Excel 檔案

**執行指令**：
```bash
dotnet run --project "Tools/EntityGenerator" -- "${input:filePath}" --json
```

**範例**：
```bash
dotnet run --project "Tools/EntityGenerator" -- "D:/Database_Design.xlsx" --json
```

此指令會輸出 JSON 格式的實體定義資料到 stdout，包含：
- 所有 Sheet 的實體資訊（EntityInfo 列表）
- 每個實體的欄位資訊（FieldInfo 列表）
- 已計算好的 ClassName、TableName、Namespace、FilePath 等屬性
- 已分類的欄位：BusinessFields、CreatorFields、EditorFields、ForeignKeyField
- 已轉換好的 C# 型別和 TypeName

**接收並解析 JSON**：
1. 讀取 stdout 輸出的 JSON 內容
2. 反序列化為 EntityInfo 物件列表
3. 每個 EntityInfo 包含完整的實體定義資訊，可直接用於生成 Entity 檔案

### 步驟 2: 接收並解析 JSON 資料

1. 讀取 stdout 輸出的 JSON 內容

### 步驟 3: 逐一生成實體

對於每個 Sheet（實體）：

1. **輸出分析結果**：
```
正在分析實體：{Description}
- Sheet 名稱: {SheetName}
- 檔案名稱: {FileName}（已從 JSON 取得）
- 檔案路徑: {FilePath}（已從 JSON 取得）
- 是否為表身: {IsDetail}（已從 JSON 取得）
業務欄位：共 {BusinessFields.Count} 個
```

2. **生成 Entity 檔案**：
   - 建立目錄（如不存在）
   - 生成完整的 Entity 類別程式碼
   - 寫入檔案

3. **輸出完成訊息**：
```
✓ 已生成：{FilePath}{FileName}
---
已完成，開始生成下一個實體
---
```

4. **不中斷，立即處理下一個實體**

### 步驟 4: 生成 DbSet 註冊程式碼

所有實體生成完畢後，輸出 DbContext 的 DbSet 註冊程式碼。

CEntityContext 檔案路徑: `NET_Core_API/Entity_Model/Entity/CEntityContext.cs`
#### 內容要求：
  - 找到 `region DbSet` 區塊，並參考現有 DbSet 宣告格式
  - 若 `region ${FolderName}相關` 不存在，則建立 `region ${FolderName}相關` 區塊
  - 新增 Entity 對應的 DbSet 屬性
  - 需與前一個 DbSet 相隔一行空白以符合 StyleCop
  - 注意縮排位置予以存在的 DBSet 對齊
  - 根據 Entity 的 namespace 引用命名空間

#### DbSet 註冊程式碼格式
```csharp
#region DbSet

    #region {FolderName}相關

    /// <summary>
    /// {Description}
    /// </summary>
    public DbSet<CTab_{FolderName}{ModuleName}{DetailName}> CTab_{FolderName}{ModuleName}{DetailName} { get; set; }

    #endregion

#endregion
```

記住：每個 DbSet 之間空一行（StyleCop 規範）。

### 步驟 5: 檢查所有生成的檔案是否有編譯錯誤
1. 檢查所有生成的檔案是否有編譯錯誤
2. 若有錯誤請修正

### 步驟 6: 檢查是否所有生成的檔案皆使用 UTF-8 with BOM 編碼格式
1. 列出檢查清單
2. 使用 powershell 指令一次檢查所有檔案路徑是否使用 UTF-8 with BOM 編碼格式
3. 若有非 UTF-8 with BOM 編碼格式的檔案，請立即轉換
```powershell
  # 定義檔案清單
$fileList = @(
    "D:\path\to\file1.cs",
    "D:\path\to\file2.cs",
    "D:\path\to\file3.cs"
)

# 批次轉換為 UTF-8 with BOM
$utf8WithBom = New-Object System.Text.UTF8Encoding $true
foreach ($filePath in $fileList) {
    if (Test-Path $filePath) {
        $content = [System.IO.File]::ReadAllText($filePath)
        [System.IO.File]::WriteAllText($filePath, $content, $utf8WithBom)
        Write-Host "✓ 已轉換: $filePath" -ForegroundColor Green
    } else {
        Write-Host "✗ 檔案不存在: $filePath" -ForegroundColor Red
    }
}
```

## Entity 生成規範

### 檔案結構

```csharp
using Microsoft.EntityFrameworkCore;
using Project_Model;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// 表身且有外鍵時才加入：using System.Text.Json.Serialization;

namespace Entity_Model.Entity.{FolderName}.{ModuleName}
{
    /// <summary>
    /// {Description}
    /// </summary>
    [Table("{TableName}")]
    [Comment("{Description}")]
    public class {ClassName}
    {
        // 表身才需要：外鍵關聯區塊

        // 主鍵欄位

        // 業務欄位（按 JSON 中 businessFields 順序）

        // 系統欄位 (建立者及異動者欄位)
    }
}
```

### 命名規則

**重要**：所有命名相關屬性已由 C# 工具計算完成，直接使用 JSON 中的值即可：
- **檔案路徑**：使用 `filePath` + `fileName`
- **Table Name**：使用 `tableName`
- **Namespace**：使用 `namespace`
- **Class Name**：使用 `className`

### 表身判斷規則

**重要**：表身判斷已由 C# 工具完成，直接使用 JSON 中的 `isDetail` 屬性即可。

如果 `isDetail` 為 true：
- `detailName` 包含表身名稱
- `foreignKeyField` 包含外鍵欄位資訊（如果不為 null）
- 需要生成外鍵關聯區塊

### 主鍵標註

```csharp
[Key]
[DatabaseGenerated(DatabaseGeneratedOption.None)]
```

所有主鍵都關閉自動編號。

### 欄位定義模板

```csharp
/// <summary>
/// {Comment}
/// </summary>
/// <example>{Example}</example>
[Column("{Column Name}", TypeName = {TypeName})]
[Comment("{Comment}：「{CommentExtra}」")]
public {C#型別}{?} {Column Name} { get; set; }
```

**重要**：
- 欄位的 `cSharpType`、`typeName`、`isNullable` 已由 C# 工具計算完成，直接使用即可。
- 一定要包含完整的 XML 註解，包含 `<example>` 標籤。
- 若 json 的 `example` 欄位為空字串，則 `<example>` 標籤內容請根據「型態(如 `string`)」自行填寫適當範例值。
- 除了外來鍵欄位外，其他欄位**不要**由 region 包覆，例如不要 `#region 業務欄位` 。
- `summary` 中的 `{Comment}` 不要包含 `commentExtra` 內容。

**字串型別初始化規則**：
- 如果欄位型別是 `string`（必填，不可為 null），必須加上 `= string.Empty;`
- 如果欄位型別是 `string?`（非必填，可為 null），不需要初始化

**範例**：
```csharp
// 必填字串 - 需要初始化
public string Material_Name { get; set; } = string.Empty;

// 非必填字串 - 不需要初始化
public string? Material_Remark { get; set; }

// 其他型別 - 不需要初始化
public long Material_Quantity { get; set; }
public int? Material_Count { get; set; }
```

### 建立者及異動者欄位處理

**重要**：C# 工具已自動處理型別轉換和必填判斷，JSON 中的 `creatorFields` 和 `editorFields` 已包含正確型別：

1. **建立者欄位（必填）**：
   - `{Prefix}_CreateId` → `Guid` + `PropertyConfig.TableID`
   - `{Prefix}_CreateCode` → `string` + `PropertyConfig.TableCode`
   - `{Prefix}_CreateDate` → `long` + `PropertyConfig.TableTime`
   - `{Prefix}_CreateIp` → `string` + `PropertyConfig.TableIP`

2. **異動者欄位（非必填）**：
   - `{Prefix}_EditId` → `Guid?` + `PropertyConfig.TableID`
   - `{Prefix}_EditCode` → `string?` + `PropertyConfig.TableCode`
   - `{Prefix}_EditDate` → `long?` + `PropertyConfig.TableTime`
   - `{Prefix}_EditIp` → `string?` + `PropertyConfig.TableIP`

直接使用 JSON 中 `creatorFields` 和 `editorFields` 陣列的資料即可。

### 表身外鍵關聯（必須在業務欄位之前）

```csharp
#region 資料庫關聯

#region 外來鍵，[ForeignKey("外來鍵參照欄位")]，若沒設定ForeignKey Code First會自動生成欄位

/// <summary>
/// {MasterTable 描述}
/// </summary>
[ForeignKey("{ForeignKeyField}")]
[JsonIgnore]
public virtual CTab_{FolderName}{MasterModuleName} CTab_{FolderName}{MasterModuleName} { get; set; }

#endregion

#endregion
```

**重要**：外鍵欄位識別已由 C# 工具完成：
- 直接使用 JSON 中的 `foreignKeyField` 屬性
- 若 `isDetail` 為 true 且 `foreignKeyField` 不為 null，則需生成外鍵關聯區塊

## 執行指令

當使用者輸入斜線命令時，請依照以下步驟執行：

1. 詢問使用者：「請提供 Excel 檔案的路徑」
2. 執行 C# 工具讀取 Excel 並輸出 JSON：
   ```bash
   cd Tools/EntityGenerator
   dotnet run -- "<Excel檔案路徑>" --json
   ```
3. 接收並解析 JSON 輸出（EntityInfo 列表）
4. 顯示總實體數：「共解析到 {entities.length} 個 Sheet（實體），開始生成...」
5. 逐一生成每個實體（輸出分析 → 生成檔案 → 輸出完成訊息）
6. 生成 DbSet 註冊程式碼
7. 檢查檔案編碼格式
8. 顯示完成訊息

## JSON 資料結構說明

C# 工具輸出的 JSON 格式如下：

```json
[
  {
    "sheetName": "Bga_Material(料件項目)",
    "folderName": "Bga",
    "moduleName": "Material",
    "detailName": "",
    "description": "料件項目",
    "prefix": "Material",
    "isDetail": false,
    "className": "CTab_BgaMaterial",
    "tableName": "Bga_Material",
    "fileName": "CTab_BgaMaterial.cs",
    "filePath": "NET_Core_API/Entity_Model/Entity/Bga/Material/",
    "namespace": "Entity_Model.Entity.Bga.Material",
    "primaryKeyField": {
      "name": "Pk_Material",
      "sqlType": "bigint",
      "length": null,
      "comment": "料件項目代號",
      "example": "1A6D1B54-58C8-4403-BE3C-F435503CFBA4",
      "isPrimaryKey": true,
      "isRequired": true,
      "cSharpType": "long",
      "typeName": "PropertyConfig.TableID",
      "isNullable": false
    },
    "businessFields": [
      {
        "name": "Material_Name",
        "sqlType": "nvarchar",
        "length": 50,
        "comment": "零件類別",
        "example": "過濾棉",
        "isPrimaryKey": false,
        "isRequired": true,
        "cSharpType": "string",
        "typeName": "\"nvarchar(50)\"",
        "isNullable": false
      }
    ],
    "creatorFields": [...],
    "editorFields": [...],
    "foreignKeyField": null
  }
]
```

**使用 JSON 資料生成 Entity 檔案時**：
- 直接使用 `className`、`tableName`、`namespace`、`filePath` 等已計算好的屬性
- 使用 `primaryKeyField` 生成主鍵欄位
- 使用 `businessFields` 生成業務欄位
- 使用 `creatorFields` 和 `editorFields` 生成系統欄位
- 如果 `isDetail` 為 true 且 `foreignKeyField` 不為 null，則生成外鍵關聯區塊
- 每個 FieldInfo 的 `cSharpType` 和 `typeName` 已經轉換完成，直接使用即可

## 輸出格式範例

```
執行 C# 工具讀取 Excel 並解析為 JSON...
接收到 JSON 資料，共解析到 N 個 Sheet（實體），開始生成...

========================================
正在分析實體：保養項目
- Sheet 名稱: Bga_Maintenance(保養項目)
- 檔案名稱: CTab_BgaMaintenance.cs（已從 JSON 取得）
- 檔案路徑: NET_Core_API/Entity_Model/Entity/Bga/Maintenance/（已從 JSON 取得）
- 是否為表身: false（已從 JSON 取得）
業務欄位：共 2 個

[生成檔案...]

✓ 已生成：NET_Core_API/Entity_Model/Entity/Bga/Maintenance/CTab_BgaMaintenance.cs
---
已完成，開始生成下一個實體
---

========================================
正在分析實體：維修單-維修內容
- Sheet 名稱: Bga_Fix_Material(維修單-維修內容)
- 檔案名稱: CTab_BgaFixMaterial.cs（已從 JSON 取得）
- 檔案路徑: NET_Core_API/Entity_Model/Entity/Bga/Fix/（已從 JSON 取得）
- 是否為表身: true（已從 JSON 取得）
- 關聯表頭: CTab_BgaFix
- 外鍵欄位: Fk_Fix（已從 JSON 取得）
...
```

## 重要提醒

1. **不要中斷流程**：每個實體完成後立即處理下一個
2. **完整輸出**：每個實體都要輸出完整的分析資訊
3. **建立目錄**：如果目錄不存在，要先建立
4. **編碼格式**：檔案編碼使用 UTF-8 with BOM
5. **表身關聯**：
   - 使用 JSON 中的 `isDetail` 判斷是否為表身
   - 使用 JSON 中的 `foreignKeyField` 取得外鍵欄位資訊
   - 外鍵關聯區塊必須在所有業務欄位之前
6. **直接使用 JSON 資料**：
   - 所有命名、型別轉換、欄位分類都已完成
   - 直接使用 JSON 中的屬性值，無需自行計算或解析
7. **欄位順序**：
   - 外鍵關聯區塊（表身才需要）
   - 主鍵欄位（使用 `primaryKeyField`）
   - 業務欄位（使用 `businessFields` 陣列）
   - 建立者欄位（使用 `creatorFields` 陣列）
   - 異動者欄位（使用 `editorFields` 陣列）
8. **XML 註解**：
   - 每個欄位都必須包含完整的 XML 註解
   - 包含 `<summary>` 和 `<example>` 標籤
   - `<example>` 不能為空，請根據「型態(如 `string`)」自行填寫適當範例值

---
