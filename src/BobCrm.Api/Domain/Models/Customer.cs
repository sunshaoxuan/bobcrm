using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Domain.Attributes;
using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Domain;

/// <summary>
/// 客户实体 - 实现IBizEntity以支持统一的实体定义系统
/// </summary>
public class Customer : IBizEntity
{
    public int Id { get; set; }
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    [Localizable(Required = false, MaxLength = 256, Hint = "客户名称")]
    public string Name { get; set; } = string.Empty;

    public int Version { get; set; } = 1;
    public string? ExtData { get; set; }

    /// <summary>
    /// 提供Customer实体的初始定义
    /// 系统启动时会自动调用此方法并同步到EntityDefinition表
    /// </summary>
    public static EntityDefinition GetInitialDefinition()
    {
        var type = typeof(Customer);
        var definition = new EntityDefinition
        {
            Namespace = type.Namespace ?? "BobCrm.Api.Domain",
            EntityName = type.Name,
            FullTypeName = type.FullName ?? "BobCrm.Api.Domain.Customer",
            EntityRoute = "customer",
            DisplayName = new Dictionary<string, string?>
            {
                { "ja", "顧客" },
                { "zh", "客户" },
                { "en", "Customer" }
            },
            Description = new Dictionary<string, string?>
            {
                { "ja", "顧客情報を管理します" },
                { "zh", "管理客户信息" },
                { "en", "Manage customer information" }
            },
            ApiEndpoint = "/api/customers",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            IsRootEntity = true,
            IsEnabled = true,
            Order = 1,
            Icon = "user",
            Category = "core"
        };

        // 定义字段元数据
        definition.Fields = new List<FieldMetadata>
        {
            new FieldMetadata
            {
                PropertyName = "Id",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "ID" },
                    { "zh", "ID" },
                    { "en", "ID" }
                },
                DataType = FieldDataType.Integer,
                IsRequired = true,
                SortOrder = 1
            },
            new FieldMetadata
            {
                PropertyName = "Code",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "顧客コード" },
                    { "zh", "客户代码" },
                    { "en", "Customer Code" }
                },
                DataType = FieldDataType.String,
                Length = 64,
                IsRequired = true,
                SortOrder = 2
            },
            new FieldMetadata
            {
                PropertyName = "Name",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "顧客名" },
                    { "zh", "客户名称" },
                    { "en", "Customer Name" }
                },
                DataType = FieldDataType.String,
                Length = 256,
                IsRequired = true,
                SortOrder = 3
            },
            new FieldMetadata
            {
                PropertyName = "Version",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "バージョン" },
                    { "zh", "版本" },
                    { "en", "Version" }
                },
                DataType = FieldDataType.Integer,
                IsRequired = true,
                DefaultValue = "1",
                SortOrder = 4
            },
            new FieldMetadata
            {
                PropertyName = "ExtData",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "拡張データ" },
                    { "zh", "扩展数据" },
                    { "en", "Extended Data" }
                },
                DataType = FieldDataType.Text,
                IsRequired = false,
                SortOrder = 5
            }
        };

        // 定义实现的接口
        definition.Interfaces = new List<EntityInterface>
        {
            new EntityInterface
            {
                InterfaceType = EntityInterfaceType.Base,
                IsEnabled = true,
                IsLocked = false
            },
            new EntityInterface
            {
                InterfaceType = EntityInterfaceType.Archive,
                IsEnabled = true,
                IsLocked = false
            },
            new EntityInterface
            {
                InterfaceType = EntityInterfaceType.Version,
                IsEnabled = true,
                IsLocked = false
            }
        };

        return definition;
    }
}

