using BobCrm.Api.Base;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 测试数据种子，用于开发环境快速填充测试数据
/// 测试通过后可以废弃此文件
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// 填充测试数据
    /// </summary>
    public static async Task SeedTestDataAsync(DbContext db)
    {
        // 检查是否已有客户数据，如果有则跳过
        if (await db.Set<Customer>().AnyAsync())
        {
            return;
        }

        // 创建测试客户
        var customer1 = new Customer { Code = "C001", Name = "示例客户A", Version = 1 };
        var customer2 = new Customer { Code = "C002", Name = "示例客户B", Version = 1 };
        
        await db.Set<Customer>().AddRangeAsync(customer1, customer2);
        await db.SaveChangesAsync();
        
        // 添加本地化名称（多语言测试数据）
        await db.Set<CustomerLocalization>().AddRangeAsync(
            new CustomerLocalization { CustomerId = customer1.Id, Language = "zh", Name = "示例客户A" },
            new CustomerLocalization { CustomerId = customer1.Id, Language = "ja", Name = "サンプル顧客A" },
            new CustomerLocalization { CustomerId = customer1.Id, Language = "en", Name = "Sample Customer A" },
            new CustomerLocalization { CustomerId = customer2.Id, Language = "zh", Name = "示例客户B" },
            new CustomerLocalization { CustomerId = customer2.Id, Language = "ja", Name = "サンプル顧客B" },
            new CustomerLocalization { CustomerId = customer2.Id, Language = "en", Name = "Sample Customer B" }
        );
        
        await db.SaveChangesAsync();
    }
    
    /// <summary>
    /// 清空所有测试数据（慎用！）
    /// </summary>
    public static async Task ClearTestDataAsync(DbContext db)
    {
        db.Set<CustomerLocalization>().RemoveRange(db.Set<CustomerLocalization>());
        db.Set<Customer>().RemoveRange(db.Set<Customer>());
        await db.SaveChangesAsync();
    }
}

