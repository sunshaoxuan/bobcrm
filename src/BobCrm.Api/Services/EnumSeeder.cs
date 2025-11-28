using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 系统枚举种子数据初始化服务
/// 负责在系统启动时确保所有系统枚举定义存在于数据库中
/// </summary>
public class EnumSeeder
{
    private readonly AppDbContext _db;

    public EnumSeeder(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 确保所有系统枚举已初始化
    /// </summary>
    public async Task EnsureSystemEnumsAsync()
    {
        // 1. 表单模板使用类型
        await EnsureEnumAsync("form_template_usage", new Dictionary<string, string?>
        {
            { "zh", "表单模板使用类型" },
            { "en", "Form Template Usage Type" },
            { "ja", "フォームテンプレート使用タイプ" }
        }, new[]
        {
            ("DETAIL", new Dictionary<string, string?> { { "zh", "详情" }, { "en", "Detail" }, { "ja", "詳細" } }, 0),
            ("EDIT", new Dictionary<string, string?> { { "zh", "编辑" }, { "en", "Edit" }, { "ja", "編集" } }, 1),
            ("LIST", new Dictionary<string, string?> { { "zh", "列表" }, { "en", "List" }, { "ja", "リスト" } }, 2),
            ("COMBINED", new Dictionary<string, string?> { { "zh", "组合" }, { "en", "Combined" }, { "ja", "組み合わせ" } }, 3)
        });

        // 2. 布局模式
        await EnsureEnumAsync("layout_mode", new Dictionary<string, string?>
        {
            { "zh", "布局模式" },
            { "en", "Layout Mode" },
            { "ja", "レイアウトモード" }
        }, new[]
        {
            ("LEFT_RIGHT_SPLIT", new Dictionary<string, string?> 
            { 
                { "zh", "左列表右明细" }, 
                { "en", "Left-Right Split" }, 
                { "ja", "左リスト右詳細" } 
            }, 1),
            ("TOP_BOTTOM_SPLIT", new Dictionary<string, string?> 
            { 
                { "zh", "上列表下明细" }, 
                { "en", "Top-Bottom Split" }, 
                { "ja", "上リスト下詳細" } 
            }, 2),
            ("LIST_ONLY", new Dictionary<string, string?> 
            { 
                { "zh", "仅列表" }, 
                { "en", "List Only" }, 
                { "ja", "リストのみ" } 
            }, 3)
        });

        // 3. 详情显示模式
        await EnsureEnumAsync("detail_display_mode", new Dictionary<string, string?>
        {
            { "zh", "详情显示模式" },
            { "en", "Detail Display Mode" },
            { "ja", "詳細表示モード" }
        }, new[]
        {
            ("INLINE", new Dictionary<string, string?> 
            { 
                { "zh", "内嵌显示" }, 
                { "en", "Inline" }, 
                { "ja", "インライン表示" } 
            }, 1),
            ("MODAL", new Dictionary<string, string?> 
            { 
                { "zh", "模态框" }, 
                { "en", "Modal" }, 
                { "ja", "モーダル" } 
            }, 2),
            ("PAGE", new Dictionary<string, string?> 
            { 
                { "zh", "独立页面" }, 
                { "en", "Page" }, 
                { "ja", "独立ページ" } 
            }, 3)
        });

        // 4. 模态框大小
        await EnsureEnumAsync("modal_size", new Dictionary<string, string?>
        {
            { "zh", "模态框大小" },
            { "en", "Modal Size" },
            { "ja", "モーダルサイズ" }
        }, new[]
        {
            ("SMALL", new Dictionary<string, string?> 
            { 
                { "zh", "小" }, 
                { "en", "Small" }, 
                { "ja", "小" } 
            }, 1),
            ("MEDIUM", new Dictionary<string, string?> 
            { 
                { "zh", "中" }, 
                { "en", "Medium" }, 
                { "ja", "中" } 
            }, 2),
            ("LARGE", new Dictionary<string, string?> 
            { 
                { "zh", "大" }, 
                { "en", "Large" }, 
                { "ja", "大" } 
            }, 3),
            ("EXTRA_LARGE", new Dictionary<string, string?> 
            { 
                { "zh", "超大" }, 
                { "en", "Extra Large" }, 
                { "ja", "特大" } 
            }, 4)
        });

        // 5. 通用布尔值枚举
        await EnsureEnumAsync("boolean", new Dictionary<string, string?>
        {
            { "zh", "是/否" },
            { "en", "Yes/No" },
            { "ja", "はい/いいえ" }
        }, new[]
        {
            ("TRUE", new Dictionary<string, string?> { { "zh", "是" }, { "en", "Yes" }, { "ja", "はい" } }, 0),
            ("FALSE", new Dictionary<string, string?> { { "zh", "否" }, { "en", "No" }, { "ja", "いいえ" } }, 1)
        });

        // 6. 性别枚举（通用）
        await EnsureEnumAsync("gender", new Dictionary<string, string?>
        {
            { "zh", "性别" },
            { "en", "Gender" },
            { "ja", "性別" }
        }, new[]
        {
            ("MALE", new Dictionary<string, string?> { { "zh", "男" }, { "en", "Male" }, { "ja", "男性" } }, 0),
            ("FEMALE", new Dictionary<string, string?> { { "zh", "女" }, { "en", "Female" }, { "ja", "女性" } }, 1),
            ("OTHER", new Dictionary<string, string?> { { "zh", "其他" }, { "en", "Other" }, { "ja", "その他" } }, 2)
        });

        // 7. 优先级枚举（通用）
        await EnsureEnumAsync("priority", new Dictionary<string, string?>
        {
            { "zh", "优先级" },
            { "en", "Priority" },
            { "ja", "優先度" }
        }, new (string Value, Dictionary<string, string?> DisplayName, int SortOrder, string? ColorTag)[]
        {
            ("LOW", new Dictionary<string, string?> { { "zh", "低" }, { "en", "Low" }, { "ja", "低" } }, 0, "green"),
            ("MEDIUM", new Dictionary<string, string?> { { "zh", "中" }, { "en", "Medium" }, { "ja", "中" } }, 1, "blue"),
            ("HIGH", new Dictionary<string, string?> { { "zh", "高" }, { "en", "High" }, { "ja", "高" } }, 2, "orange"),
            ("URGENT", new Dictionary<string, string?> { { "zh", "紧急" }, { "en", "Urgent" }, { "ja", "緊急" } }, 3, "red")
        });

        // 8. 通用状态枚举
        await EnsureEnumAsync("status", new Dictionary<string, string?>
        {
            { "zh", "状态" },
            { "en", "Status" },
            { "ja", "ステータス" }
        }, new (string Value, Dictionary<string, string?> DisplayName, int SortOrder, string? ColorTag)[]
        {
            ("ACTIVE", new Dictionary<string, string?> { { "zh", "激活" }, { "en", "Active" }, { "ja", "有効" } }, 0, "green"),
            ("INACTIVE", new Dictionary<string, string?> { { "zh", "未激活" }, { "en", "Inactive" }, { "ja", "無効" } }, 1, "gray"),
            ("PENDING", new Dictionary<string, string?> { { "zh", "待处理" }, { "en", "Pending" }, { "ja", "保留中" } }, 2, "blue"),
            ("ARCHIVED", new Dictionary<string, string?> { { "zh", "已归档" }, { "en", "Archived" }, { "ja", "アーカイブ済み" } }, 3, "gray")
        });

        // 9. 客户类型枚举（示例业务枚举）
        await EnsureEnumAsync("customer_type", new Dictionary<string, string?>
        {
            { "zh", "客户类型" },
            { "en", "Customer Type" },
            { "ja", "顧客タイプ" }
        }, new (string Value, Dictionary<string, string?> DisplayName, int SortOrder, string? ColorTag)[]
        {
            ("INDIVIDUAL", new Dictionary<string, string?>
            {
                { "zh", "个人客户" },
                { "en", "Individual" },
                { "ja", "個人顧客" }
            }, 0, "blue"),
            ("ENTERPRISE", new Dictionary<string, string?>
            {
                { "zh", "企业客户" },
                { "en", "Enterprise" },
                { "ja", "企業顧客" }
            }, 1, "purple"),
            ("PARTNER", new Dictionary<string, string?>
            {
                { "zh", "合作伙伴" },
                { "en", "Partner" },
                { "ja", "パートナー" }
            }, 2, "green"),
            ("VIP", new Dictionary<string, string?>
            {
                { "zh", "VIP客户" },
                { "en", "VIP Customer" },
                { "ja", "VIP顧客" }
            }, 3, "gold")
        });

        await _db.SaveChangesAsync();
    }

    private async Task EnsureEnumAsync(
        string code,
        Dictionary<string, string?> displayName,
        IEnumerable<(string Value, Dictionary<string, string?> DisplayName, int SortOrder, string? ColorTag)> options)
    {
        var existing = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == code);

        if (existing == null)
        {
            // 创建新枚举定义
            var enumDef = new EnumDefinition
            {
                Code = code,
                DisplayName = displayName,
                IsSystem = true,
                IsEnabled = true
            };

            foreach (var (value, optDisplayName, sortOrder, colorTag) in options)
            {
                enumDef.Options.Add(new EnumOption
                {
                    Value = value,
                    DisplayName = optDisplayName,
                    SortOrder = sortOrder,
                    IsEnabled = true,
                    IsSystem = true,
                    ColorTag = colorTag
                });
            }

            _db.EnumDefinitions.Add(enumDef);
        }
        else
        {
            // 更新现有枚举（仅更新显示名，不修改选项值）
            existing.DisplayName = displayName;

            foreach (var (value, optDisplayName, sortOrder, colorTag) in options)
            {
                var existingOption = existing.Options.FirstOrDefault(o => o.Value == value);
                if (existingOption == null)
                {
                    // 添加新选项
                    existing.Options.Add(new EnumOption
                    {
                        EnumDefinitionId = existing.Id,
                        Value = value,
                        DisplayName = optDisplayName,
                        SortOrder = sortOrder,
                        IsEnabled = true,
                        IsSystem = true,
                        ColorTag = colorTag
                    });
                }
                else
                {
                    // 更新现有选项
                    existingOption.DisplayName = optDisplayName;
                    existingOption.SortOrder = sortOrder;
                    existingOption.IsSystem = true;
                    if (!string.IsNullOrEmpty(colorTag))
                    {
                        existingOption.ColorTag = colorTag;
                    }
                }
            }
        }
    }

    // 重载方法（无颜色标签）
    private async Task EnsureEnumAsync(
        string code,
        Dictionary<string, string?> displayName,
        IEnumerable<(string Value, Dictionary<string, string?> DisplayName, int SortOrder)> options)
    {
        await EnsureEnumAsync(
            code,
            displayName,
            options.Select(o => (o.Value, o.DisplayName, o.SortOrder, (string?)null)));
    }
}
