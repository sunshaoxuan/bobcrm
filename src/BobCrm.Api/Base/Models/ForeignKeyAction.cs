namespace BobCrm.Api.Base.Models;

/// <summary>
/// 外键删除行为（抽象）
/// </summary>
public enum ForeignKeyAction
{
    Restrict = 0,
    Cascade = 1,
    SetNull = 2
}

