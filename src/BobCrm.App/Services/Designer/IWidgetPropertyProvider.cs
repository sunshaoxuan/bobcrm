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
