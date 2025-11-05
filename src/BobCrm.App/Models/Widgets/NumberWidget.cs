namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 数字输入控件
/// </summary>
public class NumberWidget : TextWidget
{
    public NumberWidget()
    {
        Type = "number";
        Label = "LBL_NUMBER";
    }

    /// <summary>默认值</summary>
    public double? DefaultValue { get; set; }

    /// <summary>最小值</summary>
    public double? MinValue { get; set; }

    /// <summary>最大值</summary>
    public double? MaxValue { get; set; }

    /// <summary>步长</summary>
    public double Step { get; set; } = 1;

    /// <summary>是否允许小数</summary>
    public bool AllowDecimal { get; set; } = true;

    /// <summary>是否显示千分位</summary>
    public bool ShowThousandsSeparator { get; set; } = false;
}
