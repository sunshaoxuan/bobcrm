# 容器设计态渲染器

## 目录结构

```
ContainerRenderers/
├── README.md                          # 本文档
├── GridDesignRenderer.razor           # Grid 容器渲染器
├── PanelDesignRenderer.razor          # Panel 容器渲染器
├── SectionDesignRenderer.razor        # Section 容器渲染器
├── FrameDesignRenderer.razor          # Frame 容器渲染器
├── TabContainerDesignRenderer.razor   # TabContainer 容器渲染器
└── GenericContainerDesignRenderer.razor # 通用容器渲染器
```

## 设计原则

### 1. **职责分离 (Separation of Concerns)**
每个容器类型的设计态渲染逻辑独立在自己的 Razor 组件中，不再集中在 `FormDesigner.razor` 中。

### 2. **开闭原则 (Open/Closed Principle)**
- **开放扩展**：添加新容器类型只需创建新的渲染器组件
- **关闭修改**：无需修改 `FormDesigner.razor` 或其他已有渲染器

### 3. **单一职责原则 (Single Responsibility Principle)**
- `FormDesigner.razor`：负责设计器的整体逻辑（工具栏、画布、属性面板、拖放协调）
- 渲染器组件：只负责特定容器类型的视觉呈现

### 4. **组件化 (Componentization)**
- 每个渲染器都是独立的 Razor 组件
- 可复用、可测试、易维护
- 遵循 Blazor 的组件化最佳实践

## 使用方式

### 在 FormDesigner 中调用

```razor
@using BobCrm.App.Components.Designer.ContainerRenderers

private RenderFragment RenderContainerDesign(ContainerWidget container) => __builder =>
{
    switch (container)
    {
        case GridWidget grid:
            <GridDesignRenderer Grid="@grid" 
                              RenderChild="@RenderDesignWidget"
                              OnDrop="@(e => OnContainerDrop(e, grid))"
                              OnDragOver="@OnDragOver" />
            break;
        
        // ... 其他容器类型 ...
    }
};
```

## 渲染器接口

所有渲染器组件都遵循相同的参数模式：

### 参数说明

| 参数名 | 类型 | 说明 |
|--------|------|------|
| `Grid/Panel/Section/...` | 对应的 Widget 类型 | 要渲染的容器实例 |
| `RenderChild` | `RenderFragment<DraggableWidget>` | 用于递归渲染子控件的委托 |
| `OnDrop` | `EventCallback<DragEventArgs>` | 拖放事件处理 |
| `OnDragOver` | `EventCallback<DragEventArgs>` | 拖拽悬停事件处理 |

## 添加新容器类型

### 步骤 1：创建渲染器组件

在 `ContainerRenderers/` 目录下创建新文件，例如 `MyContainerDesignRenderer.razor`：

```razor
@using BobCrm.App.Models.Widgets

@* MyContainer 容器的设计态渲染器 *@
<div style="...">
    @* 容器特有的 UI 结构 *@
    @if (MyContainer.Children?.Any() == true)
    {
        @foreach (var child in MyContainer.Children)
        {
            @RenderChild(child)
        }
    }
    else
    {
        <div>空状态提示</div>
    }
</div>

@code {
    [Parameter, EditorRequired]
    public MyContainerWidget MyContainer { get; set; } = null!;

    [Parameter, EditorRequired]
    public RenderFragment<DraggableWidget> RenderChild { get; set; } = null!;

    [Parameter, EditorRequired]
    public EventCallback<Microsoft.AspNetCore.Components.Web.DragEventArgs> OnDrop { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<Microsoft.AspNetCore.Components.Web.DragEventArgs> OnDragOver { get; set; }
}
```

### 步骤 2：在 FormDesigner 中注册

在 `FormDesigner.razor` 的 `RenderContainerDesign` 方法中添加新的 case：

```csharp
case MyContainerWidget myContainer:
    <MyContainerDesignRenderer MyContainer="@myContainer"
                              RenderChild="@RenderDesignWidget"
                              OnDrop="@(e => OnContainerDrop(e, myContainer))"
                              OnDragOver="@OnDragOver" />
    break;
```

## 与运行态对比

| 方面 | 设计态 | 运行态 |
|------|--------|--------|
| 位置 | `Components/Designer/ContainerRenderers/` | `Services/Widgets/Rendering/RuntimeWidgetRenderer.cs` |
| 架构 | 每个容器一个 Razor 组件 | 集中在一个服务类中 |
| 职责 | 可视化编辑外观 | 用户界面呈现 |
| 扩展性 | 添加新文件 | 添加新方法 |

## 相关文件

- 属性面板：`src/BobCrm.App/Components/Designer/PropertyPanels/`
- 控件模型：`src/BobCrm.App/Models/Widgets/`
- 运行态渲染：`src/BobCrm.App/Services/Widgets/Rendering/RuntimeWidgetRenderer.cs`
- 设计器主文件：`src/BobCrm.App/Components/Pages/FormDesigner.razor`

## 重构历史

**v0.5.5** (2025-01-XX)
- 从 `FormDesigner.razor` 抽离设计态渲染逻辑
- 创建独立的容器渲染器组件
- 将 160+ 行的 `if-else` 简化为 50 行的 `switch` + 组件调用
- 实现职责分离和开闭原则

