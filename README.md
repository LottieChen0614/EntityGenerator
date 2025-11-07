# Utils - Entity 自動生成工具集

本專案提供自動化工具，協助從 Excel 設計文件生成 ASP.NET Core Entity 類別及 DbContext 註冊程式碼。

## 📁 專案結構

```
Utils/
├── .github/
│   └── prompts/
│       └── generate-entity.prompt.md    # GitHub Copilot 自動生成指令
├── NET_Core_API/                        # 後端 ASP.NET Core Web API 專案
│   └── Entity_Model/                    
│       └── Entity/                      # 生成的 Entity 類別存放位置
├── Tools/
│   └── EntityGenerator/                 # C# 命令列工具（詳見內部 README）
└── README.md                            # 本文件
```

## 🚀 快速開始

### 前置需求

1. **Visual Studio Code** with **GitHub Copilot Chat** 擴充功能
2. **.NET 8.0 SDK** 或更高版本
3. 目標專案結構（如 `net_core_api-Beta1/`）需包含：
   - `Entity_Model/` 資料夾（存放生成的 Entity）
   - `CEntityContext.cs`（DbContext 類別檔案）

### 使用步驟

#### 1. 準備 Excel 設計文件

確保您的資料庫設計 Excel 檔案（.xlsx）格式正確。
##### Sheet 命名規則：

```
{資料夾名稱}_{模組名稱}({描述})

範例：
- Bga_Material(料件項目)
- Bga_Fix_Material(維修單-維修內容)
```

##### 欄位順序：

Column Name / Column TypeName /	資料長度 / Comment /	Comment補充說明	/ Example /	PK / Required /	備註


#### 2. 在 VS Code 中開啟 Copilot Chat

按下 `Ctrl + Shift + I`（或 `Cmd + Shift + I`）開啟 GitHub Copilot Chat。

#### 3. 使用斜線命令

在 Copilot Chat 中輸入：

```
/generate-entity : filePath=D:\完整路徑\資料結構.xlsx
```

#### 4. 自動生成

Copilot 會自動執行以下流程：

1. ✅ 呼叫 EntityGenerator 工具解析 Excel
2. ✅ 接收並分析 JSON 資料
3. ✅ 逐一生成每個 Entity 類別檔案
4. ✅ 生成 DbContext 的 DbSet 註冊程式碼
5. ✅ 檢查並確保所有檔案使用 UTF-8 with BOM 編碼

## 📝 生成內容

### Entity 類別特性

每個生成的 Entity 包含：

- ✨ 完整的 XML 註解文件
- 🔑 主鍵標註（關閉自動編號）
- 📊 業務欄位（包含 Column、Comment 屬性）
- 👤 建立者/異動者系統欄位
- 🔗 表身外鍵關聯（如適用）

### 檔案位置

生成的檔案會依照資料夾結構自動建立：

```
Entity_Model/
└── Entity/
    └── {FolderName}/
        └── {ModuleName}/
            └── CTab_{FolderName}{ModuleName}{DetailName}.cs
```

### DbContext 註冊

會自動在 `CEntityContext.cs` 的 `#region DbSet` 區塊中新增：

```csharp
#region {FolderName}相關

/// <summary>
/// {描述}
/// </summary>
public DbSet<CTab_{FolderName}{ModuleName}> CTab_{FolderName}{ModuleName} { get; set; }

#endregion
```

## 🛠️ EntityGenerator 工具

本專案使用 `Tools/EntityGenerator/` 目錄中的 C# 命令列工具來解析 Excel。

如需了解工具的技術細節、開發或維護資訊，請參閱：

```
Tools/EntityGenerator/README.md
```

## ⚙️ 設定檔說明

### `.github/prompts/generate-entity.prompt.md`

這是 GitHub Copilot 的指令檔案，定義了：

- 🤖 AI Agent 的工作流程
- 📋 Entity 生成規範
- 🎯 命名規則與型別對應
- 🔄 JSON 資料結構定義

**不建議手動修改**，除非需要調整生成邏輯或支援新的資料型別。

## 💡 提示與技巧

1. **批次生成**：一次性處理整個 Excel 檔案中的所有 Sheet
2. **增量更新**：新增 Sheet 後重新執行，只會新增尚未存在的 Entity
3. **編碼格式**：工具會自動檢查並轉換為 UTF-8 with BOM
4. **錯誤處理**：如果生成失敗，檢查 Excel 格式是否符合規範

## 📚 相關資源

- [EntityGenerator 工具文件](Tools/EntityGenerator/README.md)
- [GitHub Copilot 官方文件](https://docs.github.com/en/copilot)

## 🤝 貢獻

如需改進或回報問題，請聯絡專案維護者。

---

**最後更新**：2025年11月7日
