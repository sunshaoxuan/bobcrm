using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// Seeds the <c>view_state</c> enum used by <see cref="TemplateStateBinding"/> to link templates to view states.
/// </summary>
public class ViewStateSeeder
{
    private const string EnumCode = "view_state";
    private readonly AppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewStateSeeder"/> class.
    /// </summary>
    /// <param name="db">Application database context.</param>
    public ViewStateSeeder(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Ensures the <c>view_state</c> enum and its options exist with complete i18n and system protection.
    /// </summary>
    public async Task EnsureViewStatesAsync()
    {
        var displayName = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["zh"] = "视图状态",
            ["en"] = "View State",
            ["ja"] = "ビューステート"
        };

        var description = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["zh"] = "定义实体在不同场景下的视图状态",
            ["en"] = "Defines view states for entities in different scenarios",
            ["ja"] = "異なるシナリオでのエンティティのビューステートを定義"
        };

        var options = new[]
        {
            CreateOption("List", 1,
                ("列表", "List", "リスト"),
                ("显示实体列表（数据网格）", "Display entity list (data grid)", "エンティティの一覧（データグリッド）を表示")),
            CreateOption("DetailView", 2,
                ("详情", "Detail View", "詳細表示"),
                ("以只读模式查看实体详细信息", "View entity details in read-only mode", "エンティティ詳細を参照モードで表示")),
            CreateOption("DetailEdit", 3,
                ("编辑", "Detail Edit", "詳細編集"),
                ("以编辑模式查看实体详细信息", "Edit entity details", "エンティティ詳細を編集")),
            CreateOption("Create", 4,
                ("创建", "Create", "作成"),
                ("创建新实体记录", "Create a new entity record", "新しいエンティティレコードを作成"))
        };

        var existing = await _db.EnumDefinitions
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Code == EnumCode);

        if (existing == null)
        {
            var enumDef = new EnumDefinition
            {
                Code = EnumCode,
                DisplayName = displayName,
                Description = description,
                IsEnabled = true,
                IsSystem = true,
                Options = options.ToList()
            };

            await _db.EnumDefinitions.AddAsync(enumDef);
        }
        else
        {
            existing.DisplayName = displayName;
            existing.Description = description;
            existing.IsSystem = true;
            existing.IsEnabled = true;

            foreach (var option in options)
            {
                var existingOption = existing.Options.FirstOrDefault(o => o.Value == option.Value);
                if (existingOption == null)
                {
                    option.EnumDefinitionId = existing.Id;
                    existing.Options.Add(option);
                }
                else
                {
                    existingOption.DisplayName = option.DisplayName;
                    existingOption.Description = option.Description;
                    existingOption.SortOrder = option.SortOrder;
                    existingOption.IsEnabled = true;
                    existingOption.IsSystem = true;
                    existingOption.ColorTag = option.ColorTag;
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    private static EnumOption CreateOption(
        string value,
        int sortOrder,
        (string zh, string en, string ja) names,
        (string zh, string en, string ja) descriptions)
    {
        return new EnumOption
        {
            Value = value,
            DisplayName = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["zh"] = names.zh,
                ["en"] = names.en,
                ["ja"] = names.ja
            },
            Description = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["zh"] = descriptions.zh,
                ["en"] = descriptions.en,
                ["ja"] = descriptions.ja
            },
            SortOrder = sortOrder,
            IsEnabled = true,
            IsSystem = true
        };
    }
}
