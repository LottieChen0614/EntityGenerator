# Entity 自動生成工具使用說明

## 📋 目錄

- [功能簡介](#功能簡介)
- [環境需求](#環境需求)
- [使用方式](#使用方式)
- [Excel 格式規範](#excel-格式規範)
- [輸出說明](#輸出說明)
- [常見問題](#常見問題)

---

## 功能簡介

此工具可以讀取 Excel 檔案中的資料表定義，自動生成符合專案規範的 Entity 類別檔案，或輸出 JSON 格式供 AI Agent 使用。

### 支援功能

- ✅ 讀取 Excel (.xlsx) 檔案
- ✅ 自動解析資料表結構
- ✅ 生成 Entity 類別檔案（含完整註解）
- ✅ 支援主鍵、外鍵、表身關聯
- ✅ 自動判斷 PropertyConfig 使用時機
- ✅ 輸出 DbSet 註冊程式碼
- ✅ 輸出 JSON 格式（供 AI Agent 使用）

---

## 專案結構

```
net_core_api-Beta1/
├── NET_Core_API/
│   ├── Backstage_API/              (後台 API 專案，包含 EPPlus 套件)
│   ├── Entity_Model/               (Entity 類別存放位置)
└── Tools/
    └── EntityGenerator/            (本工具)
        ├── Program.cs              (主程式，命令列參數解析)
        ├── Files/                  (範例 Excel 檔案目錄)
        │   └── *.xlsx              (Excel 資料表定義檔)
        ├── Models/                 (資料模型)
        │   ├── EntityInfo.cs       (實體資訊模型)
        │   └── FieldInfo.cs        (欄位資訊模型)
        └── Services/               (服務類別)
            ├── ExcelReader.cs      (Excel 讀取服務，使用 EPPlus)
            ├── EntityFileGenerator.cs  (Entity 檔案生成服務)
            └── JsonExporter.cs     (JSON 輸出服務)
```

---

## 環境需求

- .NET 8.0 SDK
- Windows 作業系統
- Excel 檔案格式：`.xlsx`

---

## 使用方式

### 模式 1：生成 Entity 檔案

直接生成 Entity 類別檔案到專案目錄。

```bash
cd "<完整路徑>/Tools/EntityGenerator"
dotnet run -- "<Excel檔案路徑>"
```

**範例：**
```bash
cd "d:test/net_core_api-Beta1/Tools/EntityGenerator"

dotnet run -- "Files/資料結構.xlsx"
dotnet run -- "D:/Database_Design.xlsx"
dotnet run -- "../../資料表定義.xlsx"
```

**輸出位置：**
- Entity 檔案：`NET_Core_API/Entity_Model/Entity/{FolderName}/{ModuleName}/`
- 控制台輸出：DbSet 註冊程式碼（需手動加入 CEntityContext.cs）

---

### 模式 2：輸出 JSON 到控制台

將解析結果以 JSON 格式輸出到控制台（stdout），供 AI Agent 讀取。

```bash
cd "完整路徑/Tools/EntityGenerator"
dotnet run -- "<Excel檔案路徑>" --json
```

**範例：**
```bash
cd "d:test/net_core_api-Beta1/Tools/EntityGenerator"
dotnet run -- "Files/資料結構.xlsx" --json
```

**用途：**
- AI Agent 可讀取 JSON 輸出
- 根據 JSON 資料動態生成 Entity 檔案
- 可搭配管道（pipe）傳遞給其他程式處理

---

### 模式 3：輸出 JSON 到檔案

將解析結果儲存為 JSON 檔案。

```bash
cd "完整路徑/Tools/EntityGenerator"
dotnet run -- "<Excel檔案路徑>" --output-json "<JSON檔案路徑>"
```

**範例：**
```bash
cd "d:test/net_core_api-Beta1/Tools/EntityGenerator"
dotnet run -- "Files/資料結構.xlsx" --output-json "entities.json"
dotnet run -- "Files/資料結構.xlsx" --output-json "D:/output/entities.json"
```

**用途：**
- 方便查看和偵錯
- 可供其他工具或程式讀取
- 保存解析結果以供後續使用

---

## Excel 格式規範

### Sheet 命名規則

**格式：** `{FolderName}_{ModuleName}({Description})`

**範例：**
- `Bga_Material(料件項目)` → 料件項目實體
- `Bga_Maintenance(保養項目)` → 保養項目實體
- `Bga_Malfunction_ErrCode(故障情況-錯誤)` → 表身實體

**容錯機制：**
- 如果 Sheet 名稱超過 Excel 長度限制而缺少右括號 `)`，工具會自動容錯處理
- 範例：`Bga_Malfunction_ErrCode(故障情況-錯誤` 仍可正常解析

### 欄位定義格式

每個 Sheet 的第一行為標題行，第二行開始為欄位定義：

| 欄位 | 說明 | 必填 | 範例 |
|------|------|------|------|
| Column Name | 欄位名稱 | ✅ | `Pk_Material` |
| Column TypeName | SQL Server 型別 | ✅ | `bigint`, `nvarchar(N)` |
| 資料長度 | 資料長度 | ⬜ | `50`, `100` |
| Comment | 欄位說明 | ✅ | `料件項目代號` |
| Comment補充說明 | 補充說明 | ⬜ | `1:啟用/0:停用` |
| Example | 範例值 | ⬜ | `1A6D1B54...` |
| PK | 是否為主鍵 | ✅ | `1` 或空白 |
| Required | 是否必填 | ✅ | `1` 或 `0` |
| 備註 | 備註 | ⬜ | `主鍵`, `代碼檔` |

### 資料型別對應

| SQL Server Type | C# Type | Column TypeName | 適用範圍 |
|----------------|---------|-----------------|---------|
| bigint | long | `PropertyConfig.TableID` | 主鍵、建立者/異動者 ID |
| bigint | long | `"bigint"` | 業務欄位 |
| datetime | long | `PropertyConfig.TableTime` | 建立者/異動者 Date |
| datetime | long | `"bigint"` | 業務欄位（時間轉 Ticks） |
| nvarchar(30) | string | `PropertyConfig.TableIP` | 建立者/異動者 IP |
| nvarchar(100) | string | `PropertyConfig.TableCode` | 建立者/異動者 Code |
| nvarchar(N) | string | `"nvarchar(N)"` | 業務欄位 |
| nvarchar(1) | string | `"character(1)"` | 狀態欄位 |
| uniqueidentifier | Guid | `PropertyConfig.TableID` | 主鍵 |
| uniqueidentifier | Guid | `"uniqueidentifier"` | 業務欄位 |
| int | int | `"int"` | 業務欄位 |
| decimal | decimal | `"decimal(18,2)"` | 業務欄位 |
| bit | bool | `"bit"` | 業務欄位 |

### PropertyConfig 使用規則

**僅限以下欄位可使用 PropertyConfig：**

1. **主鍵欄位**
   - `Pk_*` → `PropertyConfig.TableID`

2. **建立者欄位（4 個）**
   - `*_CreateId` → `PropertyConfig.TableID`
   - `*_CreateCode` → `PropertyConfig.TableCode`
   - `*_CreateDate` → `PropertyConfig.TableTime`
   - `*_CreateIp` → `PropertyConfig.TableIP`

3. **異動者欄位（4 個）**
   - `*_EditId` → `PropertyConfig.TableID`
   - `*_EditCode` → `PropertyConfig.TableCode`
   - `*_EditDate` → `PropertyConfig.TableTime`
   - `*_EditIp` → `PropertyConfig.TableIP`

**業務欄位（包含外鍵）一律使用字串型別**，例如：`"bigint"`, `"nvarchar(50)"`

---

## 輸出說明

### Entity 檔案格式

生成的 Entity 類別檔案包含：

```csharp
using Microsoft.EntityFrameworkCore;
using Project_Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entity_Model.Entity.Bga.Material
{
    /// <summary>
    /// 料件項目
    /// </summary>
    [Table("Bga_Material")]
    [Comment("料件項目")]
    public class CTab_BgaMaterial
    {
        /// <summary>
        /// 料件項目代號
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Pk_Material", TypeName = PropertyConfig.TableID)]
        [Comment("料件項目代號")]
        public long Pk_Material { get; set; }

        // ... 其他欄位
    }
}
```

**特點：**
- UTF-8 with BOM 編碼
- 完整的 XML 註解
- 符合 StyleCop 規範
- 自動判斷可為 null 的型別

### JSON 格式

輸出的 JSON 包含完整的實體資訊：

```json
[
  {
    "sheetName": "Bga_Material(料件項目)",
    "folderName": "Bga",
    "moduleName": "Material",
    "description": "料件項目",
    "prefix": "Material",
    "isDetail": false,
    "className": "CTab_BgaMaterial",
    "tableName": "Bga_Material",
    "fileName": "CTab_BgaMaterial.cs",
    "filePath": "NET_Core_API/Entity_Model/Entity/Bga/Material/CTab_BgaMaterial.cs",
    "namespace": "Entity_Model.Entity.Bga.Material",
    "fields": [
      {
        "name": "Pk_Material",
        "sqlType": "bigint",
        "comment": "料件項目代號",
        "isPrimaryKey": true,
        "isRequired": true,
        "cSharpType": "long",
        "typeName": "PropertyConfig.TableID",
        "isNullable": false
      }
      // ... 其他欄位
    ]
  }
]
```

### DbSet 註冊程式碼

執行模式 1 後，控制台會輸出以下程式碼，需手動加入 `CEntityContext.cs`：

```csharp
#region Bga相關

/// <summary>
/// 料件項目
/// </summary>
public DbSet<CTab_BgaMaterial> CTab_BgaMaterial { get; set; }

/// <summary>
/// 故障情況
/// </summary>
public DbSet<CTab_BgaMalfunction> CTab_BgaMalfunction { get; set; }

#endregion
```

---

## 常見問題

### Q1: 找不到 Excel 檔案

**錯誤訊息：** `❌ 錯誤：找不到檔案：xxx.xlsx`

**解決方式：**
- 確認檔案路徑是否正確
- 使用絕對路徑或相對路徑
- 路徑包含空格時，請使用雙引號：`"D:/My Documents/file.xlsx"`

### Q2: Sheet 名稱無法解析

**警告訊息：** `警告：跳過無法解析的 Sheet 名稱：xxx`

**解決方式：**
- 檢查 Sheet 名稱格式是否符合：`{FolderName}_{ModuleName}({Description})`
- 如果因 Excel 長度限制缺少右括號 `)`，工具會自動容錯處理

### Q3: 生成的檔案位置不正確

**解決方式：**
- 確認執行指令時位於正確的目錄：`Tools/EntityGenerator`
- 檢查控制台輸出的 `[偵錯] 專案根目錄` 是否正確

### Q4: 編譯錯誤

**常見原因：**
- 忘記將 DbSet 註冊程式碼加入 `CEntityContext.cs`
- Entity 類別命名空間不正確
- 缺少必要的 using 引用

**解決方式：**
- 按照控制台輸出的「後續步驟」操作
- 執行 `dotnet build` 確認編譯無誤

### Q5: JSON 格式亂碼

**解決方式：**
- 使用支援 UTF-8 的編輯器開啟 JSON 檔案
- 推薦使用 Visual Studio Code、Notepad++ 等編輯器

### Q6: 如何處理表身關聯？

**說明：**
- Sheet 名稱包含兩個底線時，自動判斷為表身
- 範例：`Bga_Malfunction_ErrCode` → `Malfunction` 是表頭，`ErrCode` 是表身
- 工具會自動生成外鍵關聯（ForeignKey）和導航屬性（Navigation Property）

---

## 版本資訊

- **版本：** 1.0.0
- **更新日期：** 2025-01-07
- **支援框架：** .NET 8.0
- **依賴套件：** EPPlus 7.3.0

---

## 技術支援

如有問題或建議，請聯繫開發團隊。
