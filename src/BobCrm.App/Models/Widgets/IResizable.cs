namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 可调整大小的控件接口
/// </summary>
public interface IResizable
{
    /// <summary>
    /// 开始调整大小（保存初始状态）
    /// </summary>
    void OnResizeStart();

    /// <summary>
    /// 响应调整大小事件
    /// </summary>
    /// <param name="direction">方向: horizontal, vertical, both</param>
    /// <param name="deltaX">相对于上次的X增量（像素）</param>
    /// <param name="deltaY">相对于上次的Y增量（像素）</param>
    /// <param name="totalDeltaX">相对于起始点的总X增量（像素）</param>
    /// <param name="totalDeltaY">相对于起始点的总Y增量（像素）</param>
    /// <param name="containerWidth">容器宽度（用于百分比计算）</param>
    void OnResize(string direction, int deltaX, int deltaY, int totalDeltaX, int totalDeltaY, double containerWidth);

    /// <summary>
    /// 调整大小结束
    /// </summary>
    void OnResizeEnd();
}
