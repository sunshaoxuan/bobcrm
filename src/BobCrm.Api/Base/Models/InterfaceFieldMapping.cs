using System.Collections.Generic;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 接口字段映射 - 每种接口对应的字段定义
/// </summary>
public static class InterfaceFieldMapping
{
    /// <summary>
    /// 获取接口类型对应的字段列表
    /// </summary>
    public static List<InterfaceFieldDefinition> GetFields(string interfaceType)
    {
        return interfaceType switch
        {
            InterfaceType.Base => new List<InterfaceFieldDefinition>
            {
                // NOTE: 与 PostgreSQLDDLGenerator.GenerateInterfaceColumns + CSharpCodeGenerator.GenerateInterfaces 保持一致
                new() { PropertyName = "Id", DataType = FieldDataType.Integer, IsRequired = true, IsPrimaryKey = true },
                new() { PropertyName = "IsDeleted", DataType = FieldDataType.Boolean, IsRequired = true, DefaultValue = "false" },
                new() { PropertyName = "DeletedAt", DataType = FieldDataType.DateTime, IsRequired = false },
                new() { PropertyName = "DeletedBy", DataType = FieldDataType.String, Length = 100, IsRequired = false }
            },
            InterfaceType.Archive => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "Code", DataType = FieldDataType.String, Length = 50, IsRequired = true },
                new() { PropertyName = "Name", DataType = FieldDataType.String, Length = 255, IsRequired = true }
            },
            InterfaceType.Audit => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "CreatedAt", DataType = FieldDataType.DateTime, IsRequired = true },
                new() { PropertyName = "CreatedBy", DataType = FieldDataType.String, Length = 100, IsRequired = false },
                new() { PropertyName = "UpdatedAt", DataType = FieldDataType.DateTime, IsRequired = true },
                new() { PropertyName = "UpdatedBy", DataType = FieldDataType.String, Length = 100, IsRequired = false }
            },
            InterfaceType.Version => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "Version", DataType = FieldDataType.Int32, IsRequired = true, DefaultValue = "1" }
            },
            InterfaceType.TimeVersion => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "ValidFrom", DataType = FieldDataType.DateTime, IsRequired = true },
                new() { PropertyName = "ValidTo", DataType = FieldDataType.DateTime, IsRequired = true },
                new() { PropertyName = "VersionNo", DataType = FieldDataType.Int32, IsRequired = true, DefaultValue = "1" }
            },
            InterfaceType.Organization => new List<InterfaceFieldDefinition>
            {
                new()
                {
                    PropertyName = "OrganizationId",
                    DataType = FieldDataType.Guid,
                    IsRequired = true,
                    IsEntityRef = true,
                    ReferenceTable = "OrganizationNodes"
                }
            },
            _ => new List<InterfaceFieldDefinition>()
        };
    }
}
