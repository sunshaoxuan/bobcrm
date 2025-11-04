namespace BobCrm.Api.Contracts.DTOs;

// 前后端交互使用 udfColor（用户自定义颜色），映射到数据库的 PrimaryColor 字段
// 注意：C# record 参数名会作为 JSON 属性名（camelCase），所以这里不能用 PascalCase
public record UserPreferencesDto(string? theme, string? language, string? udfColor);

