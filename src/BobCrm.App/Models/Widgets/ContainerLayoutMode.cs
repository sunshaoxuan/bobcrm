namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 容器内布局模式
/// </summary>
public enum ContainerLayoutMode
{
    /// <summary>流式布局（基于 Flex，自动换行）</summary>
    Flow,

    /// <summary>Flex 布局（不自动换行）</summary>
    Flex,

    /// <summary>绝对定位</summary>
    Absolute
}
