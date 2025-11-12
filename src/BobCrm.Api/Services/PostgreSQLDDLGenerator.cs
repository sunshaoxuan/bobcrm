using System.Text;
using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Services;

/// <summary>
/// PostgreSQL DDL生成器
/// 根据EntityDefinition生成CREATE/ALTER TABLE的DDL语句
/// </summary>
public class PostgreSQLDDLGenerator
{
    /// <summary>
    /// 生成CREATE TABLE语句
    /// </summary>
    public string GenerateCreateTableScript(EntityDefinition entity)
    {
        var tableName = entity.DefaultTableName;
        var sb = new StringBuilder();

        var displayName = MultilingualTextHelper.Resolve(entity.DisplayName, entity.EntityName);
        sb.AppendLine($"-- 创建表：{displayName} ({entity.EntityName})");
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS \"{tableName}\" (");

        var columns = new List<string>();

        // 首先添加接口定义的系统字段
        var interfaceColumns = GenerateInterfaceColumns(entity);
        columns.AddRange(interfaceColumns.Select(col => $"    {col}"));

        // 然后添加自定义字段
        foreach (var field in entity.Fields.OrderBy(f => f.SortOrder))
        {
            var columnDef = GenerateColumnDefinition(field);
            columns.Add($"    {columnDef}");
        }

        sb.AppendLine(string.Join(",\n", columns));
        sb.AppendLine(");");

        // 生成索引
        sb.AppendLine();
        sb.Append(GenerateIndexes(entity, tableName));

        // 生成外键
        var foreignKeys = GenerateForeignKeys(entity, tableName);
        if (!string.IsNullOrEmpty(foreignKeys))
        {
            sb.AppendLine();
            sb.Append(foreignKeys);
        }

        // 生成注释
        sb.AppendLine();
        sb.Append(GenerateComments(entity, tableName));

        return sb.ToString();
    }

    /// <summary>
    /// 生成ALTER TABLE语句（添加新字段）
    /// </summary>
    public string GenerateAlterTableAddColumns(EntityDefinition entity, List<FieldMetadata> newFields)
    {
        var tableName = entity.DefaultTableName;
        var sb = new StringBuilder();

        var displayName = MultilingualTextHelper.Resolve(entity.DisplayName, entity.EntityName);
        sb.AppendLine($"-- 修改表：{displayName} - 添加新字段");

        foreach (var field in newFields.OrderBy(f => f.SortOrder))
        {
            var columnDef = GenerateColumnDefinition(field);
            sb.AppendLine($"ALTER TABLE \"{tableName}\" ADD COLUMN IF NOT EXISTS {columnDef};");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成ALTER TABLE语句（修改字段长度）
    /// </summary>
    public string GenerateAlterTableModifyColumns(EntityDefinition entity, Dictionary<FieldMetadata, int> fieldLengthChanges)
    {
        var tableName = entity.DefaultTableName;
        var sb = new StringBuilder();

        var displayName = MultilingualTextHelper.Resolve(entity.DisplayName, entity.EntityName);
        sb.AppendLine($"-- 修改表：{displayName} - 修改字段长度");

        foreach (var (field, newLength) in fieldLengthChanges)
        {
            var columnName = field.PropertyName;
            var pgType = MapFieldTypeToPgType(field.DataType, newLength);
            sb.AppendLine($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{columnName}\" TYPE {pgType};");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成单个字段定义
    /// </summary>
    private string GenerateColumnDefinition(FieldMetadata field)
    {
        var columnName = field.PropertyName;
        var pgType = MapFieldTypeToPgType(field.DataType, field.Length, field.Precision, field.Scale);
        var nullable = field.IsRequiredExplicitlySet
            ? (field.IsRequired ? "NOT NULL" : "NULL")
            : "NULL";
        var defaultValue = string.IsNullOrEmpty(field.DefaultValue) ? "" : $" DEFAULT {FormatDefaultValue(field)}";

        return $"\"{columnName}\" {pgType} {nullable}{defaultValue}";
    }

    /// <summary>
    /// 映射字段类型到PostgreSQL类型
    /// </summary>
    private string MapFieldTypeToPgType(string fieldType, int? length = null, int? precision = null, int? scale = null)
    {
        return fieldType switch
        {
            FieldDataType.String => length.HasValue ? $"VARCHAR({length})" : "TEXT",  // 注意：Text是String的别名
            FieldDataType.Int32 => "INTEGER",
            FieldDataType.Int64 => "BIGINT",
            FieldDataType.Decimal => precision.HasValue && scale.HasValue
                ? $"NUMERIC({precision},{scale})"
                : "NUMERIC(18,2)",
            FieldDataType.Boolean => "BOOLEAN",
            FieldDataType.DateTime => "TIMESTAMP WITHOUT TIME ZONE",
            FieldDataType.Date => "DATE",
            FieldDataType.Guid => "UUID",
            _ => "TEXT"
        };
    }

    /// <summary>
    /// 格式化默认值
    /// </summary>
    private string FormatDefaultValue(FieldMetadata field)
    {
        if (string.IsNullOrEmpty(field.DefaultValue))
            return "NULL";

        return field.DataType switch
        {
            FieldDataType.String => $"'{field.DefaultValue}'",  // 注意：Text是String的别名
            FieldDataType.Boolean => field.DefaultValue.ToLower() == "true" ? "TRUE" : "FALSE",
            FieldDataType.DateTime => field.DefaultValue.ToUpper() == "NOW" ? "CURRENT_TIMESTAMP" : $"'{field.DefaultValue}'",
            FieldDataType.Date => field.DefaultValue.ToUpper() == "TODAY" ? "CURRENT_DATE" : $"'{field.DefaultValue}'",
            FieldDataType.Guid => field.DefaultValue.ToUpper() == "NEWID" ? "gen_random_uuid()" : $"'{field.DefaultValue}'",
            _ => field.DefaultValue
        };
    }

    /// <summary>
    /// 根据实体接口生成数据库列
    /// </summary>
    private List<string> GenerateInterfaceColumns(EntityDefinition entity)
    {
        var columns = new List<string>();
        var addedColumns = new HashSet<string>(); // 防止重复添加

        foreach (var entityInterface in entity.Interfaces.Where(i => i.IsEnabled))
        {
            switch (entityInterface.InterfaceType)
            {
                case EntityInterfaceType.Base:
                    // IEntity 字段（包含逻辑删除）
                    if (addedColumns.Add("Id"))
                        columns.Add("\"Id\" SERIAL PRIMARY KEY");
                    if (addedColumns.Add("IsDeleted"))
                        columns.Add("\"IsDeleted\" BOOLEAN NOT NULL DEFAULT FALSE");
                    if (addedColumns.Add("DeletedAt"))
                        columns.Add("\"DeletedAt\" TIMESTAMP WITHOUT TIME ZONE NULL");
                    if (addedColumns.Add("DeletedBy"))
                        columns.Add("\"DeletedBy\" VARCHAR(100) NULL");
                    break;

                case EntityInterfaceType.Archive:
                    // IArchive 字段
                    if (addedColumns.Add("Code"))
                        columns.Add("\"Code\" VARCHAR(100) NOT NULL");
                    if (addedColumns.Add("Name"))
                        columns.Add("\"Name\" VARCHAR(200) NOT NULL");
                    break;

                case EntityInterfaceType.Audit:
                    // IAuditable 字段
                    if (addedColumns.Add("CreatedAt"))
                        columns.Add("\"CreatedAt\" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP");
                    if (addedColumns.Add("CreatedBy"))
                        columns.Add("\"CreatedBy\" VARCHAR(100) NULL");
                    if (addedColumns.Add("UpdatedAt"))
                        columns.Add("\"UpdatedAt\" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP");
                    if (addedColumns.Add("UpdatedBy"))
                        columns.Add("\"UpdatedBy\" VARCHAR(100) NULL");
                    if (addedColumns.Add("Version"))
                        columns.Add("\"Version\" INTEGER NOT NULL DEFAULT 1");
                    break;

                case EntityInterfaceType.Version:
                    // IVersioned 字段
                    if (addedColumns.Add("Version"))
                        columns.Add("\"Version\" INTEGER NOT NULL DEFAULT 1");
                    break;

                case EntityInterfaceType.TimeVersion:
                    // ITimeVersioned 字段
                    if (addedColumns.Add("ValidFrom"))
                        columns.Add("\"ValidFrom\" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP");
                    if (addedColumns.Add("ValidTo"))
                        columns.Add("\"ValidTo\" TIMESTAMP WITHOUT TIME ZONE NULL");
                    if (addedColumns.Add("VersionNo"))
                        columns.Add("\"VersionNo\" INTEGER NOT NULL DEFAULT 1");
                    break;

                case EntityInterfaceType.Organization:
                    if (addedColumns.Add("OrganizationId"))
                        columns.Add("\"OrganizationId\" UUID NOT NULL");
                    if (addedColumns.Add("OrganizationCode"))
                        columns.Add("\"OrganizationCode\" VARCHAR(64) NOT NULL");
                    if (addedColumns.Add("OrganizationName"))
                        columns.Add("\"OrganizationName\" VARCHAR(200) NULL");
                    if (addedColumns.Add("OrganizationPathCode"))
                        columns.Add("\"OrganizationPathCode\" VARCHAR(128) NOT NULL");
                    break;
            }
        }

        return columns;
    }

    /// <summary>
    /// 生成索引语句
    /// </summary>
    private string GenerateIndexes(EntityDefinition entity, string tableName)
    {
        var sb = new StringBuilder();

        // 主键索引（假设Id字段）
        var idField = entity.Fields.FirstOrDefault(f => f.PropertyName == "Id");
        if (idField != null)
        {
            sb.AppendLine($"-- 主键");
            sb.AppendLine($"ALTER TABLE \"{tableName}\" ADD PRIMARY KEY (\"Id\");");
        }

        // 为实体引用字段创建索引
        var refFields = entity.Fields.Where(f => f.IsEntityRef && f.ReferencedEntityId.HasValue);
        foreach (var field in refFields)
        {
            var indexName = $"IX_{tableName}_{field.PropertyName}";
            sb.AppendLine($"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" (\"{field.PropertyName}\");");
        }

        // 为Code字段创建唯一索引（如果有Archive接口）
        var hasArchive = entity.Interfaces.Any(i => i.InterfaceType == EntityInterfaceType.Archive && i.IsEnabled);
        if (hasArchive)
        {
            var codeField = entity.Fields.FirstOrDefault(f => f.PropertyName == "Code");
            if (codeField != null)
            {
                var indexName = $"UX_{tableName}_Code";
                sb.AppendLine($"CREATE UNIQUE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" (\"Code\");");
            }
        }

        var hasOrganization = entity.Interfaces.Any(i => i.InterfaceType == EntityInterfaceType.Organization && i.IsEnabled);
        if (hasOrganization)
        {
            var orgIdField = entity.Fields.FirstOrDefault(f => f.PropertyName == "OrganizationId");
            if (orgIdField != null)
            {
                var indexName = $"IX_{tableName}_OrganizationId";
                sb.AppendLine($"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" (\"OrganizationId\");");
            }

            var orgPathField = entity.Fields.FirstOrDefault(f => f.PropertyName == "OrganizationPathCode");
            if (orgPathField != null)
            {
                var indexName = $"IX_{tableName}_OrganizationPathCode";
                sb.AppendLine($"CREATE INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" (\"OrganizationPathCode\");");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成外键约束
    /// </summary>
    private string GenerateForeignKeys(EntityDefinition entity, string tableName)
    {
        var sb = new StringBuilder();
        var refFields = entity.Fields.Where(f =>
            f.IsEntityRef && (f.ReferencedEntityId.HasValue || !string.IsNullOrEmpty(f.TableName)));

        if (!refFields.Any())
            return string.Empty;

        sb.AppendLine("-- 外键约束");

        foreach (var field in refFields)
        {
            // 注意：这里需要从ReferencedEntityId查询实际的表名
            // 简化实现，假设可以从field.TableName获取
            if (!string.IsNullOrEmpty(field.TableName))
            {
                var fkName = $"FK_{tableName}_{field.PropertyName}";
                sb.AppendLine($"ALTER TABLE \"{tableName}\" ADD CONSTRAINT \"{fkName}\" " +
                    $"FOREIGN KEY (\"{field.PropertyName}\") REFERENCES \"{field.TableName}\" (\"Id\") ON DELETE RESTRICT;");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成表和列注释
    /// </summary>
    private string GenerateComments(EntityDefinition entity, string tableName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-- 表注释");
        var entityDisplayName = MultilingualTextHelper.Resolve(entity.DisplayName, entity.EntityName);
        sb.AppendLine($"COMMENT ON TABLE \"{tableName}\" IS '{entityDisplayName}';");

        foreach (var field in entity.Fields)
        {
            var fieldDisplayName = MultilingualTextHelper.Resolve(field.DisplayName, field.PropertyName);
            sb.AppendLine($"COMMENT ON COLUMN \"{tableName}\".\"{field.PropertyName}\" IS '{fieldDisplayName}';");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成DROP TABLE语句
    /// </summary>
    public string GenerateDropTableScript(EntityDefinition entity)
    {
        var tableName = entity.DefaultTableName;
        var sb = new StringBuilder();

        var displayName = MultilingualTextHelper.Resolve(entity.DisplayName, entity.EntityName);
        sb.AppendLine($"-- 删除表：{displayName} ({entity.EntityName})");
        sb.AppendLine($"DROP TABLE IF EXISTS \"{tableName}\" CASCADE;");

        return sb.ToString();
    }

    /// <summary>
    /// 生成接口字段（Base, Archive, Audit等）
    /// </summary>
    public List<FieldMetadata> GenerateInterfaceFields(EntityInterface entityInterface)
    {
        var fields = new List<FieldMetadata>();

        switch (entityInterface.InterfaceType)
        {
            case EntityInterfaceType.Base:
                fields.Add(new FieldMetadata
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
                });
                break;

            case EntityInterfaceType.Archive:
                fields.Add(new FieldMetadata
                {
                    PropertyName = "Code",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "コード" },
                        { "zh", "代码" },
                        { "en", "Code" }
                    },
                    DataType = FieldDataType.String,
                    Length = 64,
                    IsRequired = true,
                    SortOrder = 10
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "Name",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "名称" },
                        { "zh", "名称" },
                        { "en", "Name" }
                    },
                    DataType = FieldDataType.String,
                    Length = 256,
                    IsRequired = true,
                    SortOrder = 11
                });
                break;

            case EntityInterfaceType.Audit:
                fields.Add(new FieldMetadata
                {
                    PropertyName = "CreatedAt",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "作成日時" },
                        { "zh", "创建时间" },
                        { "en", "Created At" }
                    },
                    DataType = FieldDataType.DateTime,
                    IsRequired = true,
                    DefaultValue = "NOW",
                    SortOrder = 100
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "CreatedBy",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "作成者" },
                        { "zh", "创建人" },
                        { "en", "Created By" }
                    },
                    DataType = FieldDataType.String,
                    Length = 100,
                    IsRequired = false,
                    SortOrder = 101
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "UpdatedAt",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "更新日時" },
                        { "zh", "更新时间" },
                        { "en", "Updated At" }
                    },
                    DataType = FieldDataType.DateTime,
                    IsRequired = true,
                    DefaultValue = "NOW",
                    SortOrder = 102
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "UpdatedBy",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "更新者" },
                        { "zh", "更新人" },
                        { "en", "Updated By" }
                    },
                    DataType = FieldDataType.String,
                    Length = 100,
                    IsRequired = false,
                    SortOrder = 103
                });
                fields.Add(new FieldMetadata
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
                    SortOrder = 104
                });
                break;

            case EntityInterfaceType.Version:
                fields.Add(new FieldMetadata
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
                    SortOrder = 200
                });
                break;

            case EntityInterfaceType.TimeVersion:
                fields.Add(new FieldMetadata
                {
                    PropertyName = "ValidFrom",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "有効開始日時" },
                        { "zh", "生效起始时间" },
                        { "en", "Valid From" }
                    },
                    DataType = FieldDataType.DateTime,
                    IsRequired = true,
                    DefaultValue = "NOW",
                    SortOrder = 210
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "ValidTo",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "有効終了日時" },
                        { "zh", "生效结束时间" },
                        { "en", "Valid To" }
                    },
                    DataType = FieldDataType.DateTime,
                    IsRequired = false,
                    SortOrder = 211
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "VersionNo",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "バージョン番号" },
                        { "zh", "版本号" },
                        { "en", "Version Number" }
                    },
                    DataType = FieldDataType.Integer,
                    IsRequired = true,
                    DefaultValue = "1",
                    SortOrder = 212
                });
                break;

            case EntityInterfaceType.Organization:
                fields.Add(new FieldMetadata
                {
                    PropertyName = "OrganizationId",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "組織ID" },
                        { "zh", "组织ID" },
                        { "en", "Organization Id" }
                    },
                    DataType = FieldDataType.Guid,
                    IsRequired = true,
                    SortOrder = 300,
                    IsEntityRef = true,
                    TableName = "OrganizationNodes"
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "OrganizationCode",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "組織コード" },
                        { "zh", "组织编码" },
                        { "en", "Organization Code" }
                    },
                    DataType = FieldDataType.String,
                    Length = 64,
                    IsRequired = true,
                    SortOrder = 301
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "OrganizationName",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "組織名" },
                        { "zh", "组织名称" },
                        { "en", "Organization Name" }
                    },
                    DataType = FieldDataType.String,
                    Length = 200,
                    IsRequired = false,
                    SortOrder = 302
                });
                fields.Add(new FieldMetadata
                {
                    PropertyName = "OrganizationPathCode",
                    DisplayName = new Dictionary<string, string?>
                    {
                        { "ja", "組織Pathコード" },
                        { "zh", "组织路径编码" },
                        { "en", "Organization Path Code" }
                    },
                    DataType = FieldDataType.String,
                    Length = 128,
                    IsRequired = true,
                    SortOrder = 303
                });
                break;
        }

        return fields;
    }
}
