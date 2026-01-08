using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BobCrm.Api.Services;

/// <summary>
/// DDL执行服务
/// 负责执行DDL脚本并记录执行历史
/// </summary>
public class DDLExecutionService
{
    protected readonly AppDbContext _db;
    protected readonly ILogger<DDLExecutionService> _logger;

    public DDLExecutionService(AppDbContext db, ILogger<DDLExecutionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 执行DDL脚本
    /// </summary>
    /// <param name="entityDefinitionId">实体定义ID</param>
    /// <param name="scriptType">脚本类型（Create/Alter/Drop）</param>
    /// <param name="sqlScript">SQL脚本内容</param>
    /// <param name="createdBy">创建人</param>
    /// <returns>DDL脚本记录</returns>
    public virtual async Task<DDLScript> ExecuteDDLAsync(
        Guid entityDefinitionId,
        string scriptType,
        string sqlScript,
        string? createdBy = null)
    {
        var script = new DDLScript
        {
            EntityDefinitionId = entityDefinitionId,
            ScriptType = scriptType,
            SqlScript = sqlScript,
            Status = DDLScriptStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        try
        {
            _logger.LogInformation("[DDL] Executing {ScriptType} script for entity {EntityId}", scriptType, entityDefinitionId);

            // 执行DDL（使用原始SQL）
            await ExecuteSqlScriptAsync(sqlScript);

            script.Status = DDLScriptStatus.Success;
            script.ExecutedAt = DateTime.UtcNow;

            _logger.LogInformation("[DDL] ✓ {ScriptType} script executed successfully", scriptType);
        }
        catch (Exception ex)
        {
            script.Status = DDLScriptStatus.Failed;
            script.ErrorMessage = ex.Message;
            script.ExecutedAt = DateTime.UtcNow;

            _logger.LogError(ex, "[DDL] ✗ {ScriptType} script execution failed: {Error}", scriptType, ex.Message);
        }

        // 保存脚本记录
        await _db.DDLScripts.AddAsync(script);
        await _db.SaveChangesAsync();

        return script;
    }

    /// <summary>
    /// 批量执行DDL脚本（事务）
    /// </summary>
    public virtual async Task<List<DDLScript>> ExecuteDDLBatchAsync(
        Guid entityDefinitionId,
        List<(string ScriptType, string SqlScript)> scripts,
        string? createdBy = null)
    {
        var results = new List<DDLScript>();

        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            foreach (var (scriptType, sqlScript) in scripts)
            {
                var result = await ExecuteDDLAsync(entityDefinitionId, scriptType, sqlScript, createdBy);
                results.Add(result);

                if (result.Status == DDLScriptStatus.Failed)
                {
                    _logger.LogError("[DDL] Batch execution stopped due to failure");
                    await transaction.RollbackAsync();
                    return results;
                }
            }

            await transaction.CommitAsync();
            _logger.LogInformation("[DDL] ✓ Batch execution completed successfully: {Count} scripts", scripts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DDL] ✗ Batch execution failed: {Error}", ex.Message);
            await transaction.RollbackAsync();
            throw;
        }

        return results;
    }

    /// <summary>
    /// 回滚DDL（执行反向脚本）
    /// </summary>
    public async Task<DDLScript> RollbackDDLAsync(Guid scriptId, string rollbackScript, string? createdBy = null)
    {
        var originalScript = await _db.DDLScripts.FindAsync(scriptId);
        if (originalScript == null)
            throw new ArgumentException($"DDL script {scriptId} not found");

        _logger.LogInformation("[DDL] Rolling back script {ScriptId}", scriptId);

        var rollback = await ExecuteDDLAsync(
            originalScript.EntityDefinitionId,
            DDLScriptType.Rollback,
            rollbackScript,
            createdBy
        );

        if (rollback.Status == DDLScriptStatus.Success)
        {
            originalScript.Status = DDLScriptStatus.RolledBack;
            await _db.SaveChangesAsync();
        }

        return rollback;
    }

    /// <summary>
    /// 验证DDL脚本（只解析不执行）
    /// </summary>
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateDDLAsync(string sqlScript)
    {
        try
        {
            // 使用EXPLAIN来验证语法（不实际执行）
            // 注意：EXPLAIN对DDL语句支持有限，这里只做基础检查
            await ExecuteSqlScriptAsync($"EXPLAIN {sqlScript}");
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 获取实体的DDL历史
    /// </summary>
    public async Task<List<DDLScript>> GetDDLHistoryAsync(Guid entityDefinitionId)
    {
        return await _db.DDLScripts
            .Where(s => s.EntityDefinitionId == entityDefinitionId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 检查表是否存在
    /// </summary>
    public virtual async Task<bool> TableExistsAsync(string tableName)
    {
        var sql = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_name = {0}
            ) AS ""Value""";

        var exists = await _db.Database
            .SqlQueryRaw<bool>(sql, tableName.ToLower())
            .FirstOrDefaultAsync();

        return exists;
    }

    /// <summary>
    /// 获取表的列信息
    /// </summary>
    public virtual async Task<List<TableColumnInfo>> GetTableColumnsAsync(string tableName)
    {
        var sql = @"
            SELECT
                column_name as ColumnName,
                data_type as DataType,
                character_maximum_length as MaxLength,
                is_nullable as IsNullable,
                column_default as DefaultValue
            FROM information_schema.columns
            WHERE table_schema = 'public'
            AND table_name = {0}
            ORDER BY ordinal_position";

        return await _db.Database
            .SqlQueryRaw<TableColumnInfo>(sql, tableName.ToLower())
            .ToListAsync();
    }

    private async Task ExecuteSqlScriptAsync(string sqlScript)
    {
        var connection = _db.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        // Npgsql prepared/extended protocol disallows multi-statement SQL in a single command.
        // Our generated DDL often includes multiple statements (CREATE TABLE + COMMENT ...),
        // so execute them sequentially.
        var statements = SplitSqlStatements(sqlScript).ToList();
        _logger.LogInformation("[DDL] Split into {Count} statement(s).", statements.Count);
        for (var index = 0; index < statements.Count; index++)
        {
            var statement = statements[index];
            await using var command = connection.CreateCommand();
            command.CommandText = statement;

            var activeTransaction = _db.Database.CurrentTransaction?.GetDbTransaction();
            if (activeTransaction != null)
            {
                command.Transaction = activeTransaction;
            }

            await command.ExecuteNonQueryAsync();
        }
    }

    private static IEnumerable<string> SplitSqlStatements(string sqlScript)
    {
        if (string.IsNullOrWhiteSpace(sqlScript))
        {
            yield break;
        }

        var sb = new StringBuilder();
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inDollarQuote = false;

        for (var i = 0; i < sqlScript.Length; i++)
        {
            var ch = sqlScript[i];
            var next = i + 1 < sqlScript.Length ? sqlScript[i + 1] : '\0';

            if (!inSingleQuote && !inDoubleQuote)
            {
                if (!inDollarQuote && ch == '$' && next == '$')
                {
                    inDollarQuote = true;
                    sb.Append("$$");
                    i++;
                    continue;
                }

                if (inDollarQuote && ch == '$' && next == '$')
                {
                    inDollarQuote = false;
                    sb.Append("$$");
                    i++;
                    continue;
                }
            }

            if (!inDollarQuote)
            {
                if (!inDoubleQuote && ch == '\'' && !IsEscaped(sqlScript, i))
                {
                    inSingleQuote = !inSingleQuote;
                }
                else if (!inSingleQuote && ch == '"' && !IsEscaped(sqlScript, i))
                {
                    inDoubleQuote = !inDoubleQuote;
                }
            }

            if (!inSingleQuote && !inDoubleQuote && !inDollarQuote && ch == ';')
            {
                var statement = sb.ToString().Trim();
                sb.Clear();
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    yield return statement;
                }
                continue;
            }

            sb.Append(ch);
        }

        var tail = sb.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(tail))
        {
            yield return tail;
        }
    }

    private static bool IsEscaped(string input, int index)
    {
        var backslashes = 0;
        for (var i = index - 1; i >= 0 && input[i] == '\\'; i--)
        {
            backslashes++;
        }
        return backslashes % 2 == 1;
    }
}
