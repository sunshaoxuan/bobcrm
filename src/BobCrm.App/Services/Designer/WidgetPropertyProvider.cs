using BobCrm.App.Models.Designer;
using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Designer;

/// <summary>
/// 控件属性元数据提供者
/// 根据控件类型返回对应的属性清单
/// </summary>
public interface IWidgetPropertyProvider
{
    /// <summary>
    /// 获取指定控件的属性元数据列表
    /// </summary>
    List<WidgetPropertyMetadata> GetProperties(DraggableWidget widget);
}

/// <summary>
/// 简化的属性提供者实现
/// 直接委托给 Widget 自身的 GetPropertyMetadata() 方法
/// </summary>
public class WidgetPropertyProvider : IWidgetPropertyProvider
{
    /// <summary>
    /// 获取指定控件的属性元数据列表
    /// 直接调用 widget 自身的 GetPropertyMetadata() 方法
    /// 符合面向对象的封装原则
    /// </summary>
    public List<WidgetPropertyMetadata> GetProperties(DraggableWidget widget)
    {
        return widget.GetPropertyMetadata();
    }
}

