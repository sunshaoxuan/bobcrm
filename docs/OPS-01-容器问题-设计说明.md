# 容器拖放功能设计文档

## 目录
- [架构概述](#架构概述)
- [事件流设计](#事件流设计)
- [关键组件](#关键组件)
- [状态管理](#状态管理)
- [DOM结构](#dom结构)
- [CSS策略](#css策略)
- [JavaScript集成](#javascript集成)

---

## 架构概述

### 设计目标

容器拖放功能需要支持：
1. **从工具箱拖入组件** 到容器
2. **容器内重新排序** 组件
3. **跨容器移动** 组件
4. **容器嵌套** 支持
5. **实时视觉反馈**（drop marker）

### 核心挑战

1. **事件传播复杂性**
   - 嵌套DOM结构导致事件多次触发
   - 需要精确控制事件冒泡和停止传播

2. **指针事件控制**
   - 拖拽时需要禁用某些元素的交互
   - 同时保持drop zone的可接收性

3. **异步竞态条件**
   - Blazor的async/await与浏览器事件序列的冲突
   - 需要防止状态被意外清空

4. **视觉一致性**
   - 设计态和运行态的组件渲染差异
   - 需要准确计算插入位置

---

## 事件流设计

### HTML5 拖放事件序列

```
1. dragstart  → 开始拖动（用户按住鼠标拖动元素）
2. drag       → 持续拖动（鼠标移动时重复触发）
3. dragenter  → 进入drop target
4. dragover   → 在drop target上移动（必须preventDefault才能drop）
5. drop       → 释放鼠标，放下元素
6. dragend    → 拖动结束（无论成功或取消）
```

### 事件处理策略

#### 场景1: 空容器接收第一个组件

```
用户拖动 Input 组件到空的 Section 容器

事件流:
┌─────────────────────────────────────────────────┐
│ 1. dragstart on component-item (工具箱)          │
│    ├─ OnDragStart()                             │
│    ├─ draggedDefinition = Input                 │
│    └─ body.is-dragging 类添加 (via JS)          │
├─────────────────────────────────────────────────┤
│ 2. dragover on .frame-drop-zone (Section内)     │
│    ├─ OnDragOver()                              │
│    └─ preventDefault() → 允许drop                │
├─────────────────────────────────────────────────┤
│ 3. drop on .frame-drop-zone                     │
│    ├─ OnContainerDrop(e, sectionContainer)      │
│    ├─ 保存快照: definition = draggedDefinition  │
│    ├─ 计算insertIndex (via JS)                  │
│    ├─ Create new Input widget                   │
│    ├─ sectionContainer.Children.Add(widget)     │
│    ├─ dropSucceeded = true                      │
│    └─ StateHasChanged()                         │
├─────────────────────────────────────────────────┤
│ 4. dragend on component-item                    │
│    ├─ OnDragEnd()                               │
│    ├─ 检查 isProcessingDrop (false, 已完成)     │
│    ├─ 清空 draggedDefinition                    │
│    └─ body.is-dragging 类移除 (via JS)          │
└─────────────────────────────────────────────────┘

结果: Input 组件成功添加到 Section ✓
```

#### 场景2: 已有子元素的容器接收新组件

```
用户拖动 Checkbox 组件到已有 Input 的 Section 容器

DOM结构:
.frame-drop-zone (Section内)
  └── .container-child-wrapper (Input的外壳)
        └── Input组件的内容

事件流:
┌─────────────────────────────────────────────────┐
│ 1. dragstart on component-item (工具箱)          │
│    ├─ OnDragStart()                             │
│    ├─ draggedDefinition = Checkbox              │
│    └─ isProcessingDrop = false                  │
├─────────────────────────────────────────────────┤
│ 2. dragover on .container-child-wrapper (Input) │
│    ├─ OnDragOver()                              │
│    └─ preventDefault() → 允许drop                │
├─────────────────────────────────────────────────┤
│ 3. drop on .container-child-wrapper             │
│    ├─ @ondrop="OnContainerDrop(e, section)"     │
│    ├─ ⚠️ 第一行: isProcessingDrop = true        │
│    ├─ 保存快照: definition = Checkbox           │
│    ├─ await getInsertIndex()                    │
│    │   └─ (期间浏览器触发dragend，但被guard阻止)│
│    ├─ Create new Checkbox widget                │
│    ├─ Insert at index 1                         │
│    ├─ @ondrop:stopPropagation                   │
│    │   └─ 阻止冒泡到.frame-drop-zone ✓          │
│    └─ finally: isProcessingDrop = false         │
├─────────────────────────────────────────────────┤
│ 4. dragend on component-item                    │
│    ├─ OnDragEnd()                               │
│    ├─ if (isProcessingDrop) return; ✓           │
│    └─ (已经在finally中重置为false)              │
└─────────────────────────────────────────────────┘

关键点:
- ✓ 事件在wrapper上触发，不是frame-drop-zone
- ✓ stopPropagation阻止重复处理
- ✓ Guard标志防止dragend清空状态
```

#### 场景3: 容器内组件重新排序

```
用户在 Section 内拖动 Input（索引0）到 Checkbox（索引1）之后

事件流:
┌─────────────────────────────────────────────────┐
│ 1. dragstart on Input's wrapper                │
│    ├─ OnWidgetDragStart(e, inputWidget)         │
│    ├─ draggedWidget = inputWidget               │
│    ├─ draggedParentContainer = sectionContainer │
│    └─ @ondragstart:stopPropagation ✓            │
│        └─ 防止冒泡到容器的dragstart             │
├─────────────────────────────────────────────────┤
│ 2. dragover on Checkbox's wrapper              │
│    ├─ OnDragOver()                              │
│    └─ preventDefault()                          │
├─────────────────────────────────────────────────┤
│ 3. drop on Checkbox's wrapper                  │
│    ├─ OnContainerDrop(e, sectionContainer)      │
│    ├─ isProcessingDrop = true                   │
│    ├─ movingWidget = inputWidget                │
│    ├─ originalParent = sectionContainer         │
│    ├─ insertIndex = 2 (Checkbox之后)            │
│    ├─ isSameContainer = true ✓                  │
│    ├─ oldIndex = 0                              │
│    ├─ Remove from oldIndex                      │
│    ├─ insertIndex-- → 1 (因为移除导致索引变化)  │
│    ├─ Insert at index 1                         │
│    └─ stopPropagation ✓                         │
└─────────────────────────────────────────────────┘

结果: Input 移到 Checkbox 之后 ✓
```

---

## 关键组件

### 1. FormDesigner.razor

**职责**: 主设计器组件，管理整体拖放逻辑

**关键方法**:

```csharp
// 工具箱组件开始拖动
private async Task OnDragStart(DragEventArgs e, WidgetDefinition definition)
{
    dropSucceeded = false;
    draggedDefinition = definition;
    isDragging = true;  // 不调用StateHasChanged，避免DOM替换
}

// 已有组件开始拖动
private async Task OnWidgetDragStart(DragEventArgs e, DraggableWidget widget)
{
    dropSucceeded = false;
    draggedWidget = widget;
    draggedParentContainer = FindParentContainer(layoutWidgets, widget);
    isDragging = true;
}

// 容器接收drop
private async Task OnContainerDrop(DragEventArgs e, ContainerWidget targetContainer)
{
    // ⚠️ 关键：第一行，任何await之前
    isProcessingDrop = true;
    var definition = draggedDefinition;
    var movingWidget = draggedWidget;
    var originalParent = draggedParentContainer;

    try {
        // 计算插入位置
        var insertIndex = await JS.InvokeAsync<int>(
            "bobcrm.getInsertIndex",
            $".frame-drop-zone[data-container-id=\"{targetContainer.Id}\"]",
            e.ClientX, e.ClientY);

        // 插入逻辑...
    } finally {
        isProcessingDrop = false;
    }
}

// dragend事件
private async Task OnDragEnd(DragEventArgs e)
{
    // Guard: 如果drop正在处理，不清空状态
    if (isProcessingDrop) return;

    if (!dropSucceeded) {
        draggedDefinition = null;
        draggedWidget = null;
    }
    dropSucceeded = false;
    isDragging = false;
    await InvokeAsync(StateHasChanged);
}

// dragover事件（允许drop）
private void OnDragOver(DragEventArgs e)
{
    // 空实现，关键是@ondragover:preventDefault
}
```

**状态字段**:

```csharp
// 拖放状态
private WidgetRegistry.WidgetDefinition? draggedDefinition;  // 从工具箱拖动的组件定义
private DraggableWidget? draggedWidget;                      // 正在拖动的已有组件
private DraggableWidget? draggedParentContainer;             // 拖动组件的父容器
private bool dropSucceeded = false;                          // drop是否成功
private bool isDragging = false;                             // 全局拖拽状态
private bool isProcessingDrop = false;                       // Guard: 正在处理drop
```

### 2. 容器渲染器（FrameDesignRenderer等）

**职责**: 渲染容器的设计态外观，提供drop zone

**示例**: `FrameDesignRenderer.razor`

```razor
<div class="frame-drop-zone"
     data-container-id="@Frame.Id"
     style="pointer-events:auto; ..."
     @ondrop="@OnDrop"
     @ondrop:preventDefault
     @ondrop:stopPropagation
     @ondragover="@OnDragOver"
     @ondragover:preventDefault>

    @if (Frame.Children?.Any() == true)
    {
        @foreach (var child in Frame.Children)
        {
            @RenderChild(child)
        }
    }
    else
    {
        <div style="pointer-events:none">拖入组件到框架</div>
    }
</div>
```

**关键点**:
- `data-container-id` 用于JavaScript定位
- `pointer-events:auto` 确保可接收事件
- `@ondrop:stopPropagation` 防止重复处理（在FormDesigner中已处理）

### 3. RenderDesignWidget方法

**职责**: 渲染组件的设计态外观（包含交互层）

**容器Wrapper**:

```razor
<div class="container-child-wrapper @(isSelected ? "selected" : "")"
     data-widget-id="@container.Id"
     @onclick="@(() => SelectWidget(container))" @onclick:stopPropagation
     @ondragover="OnDragOver" @ondragover:preventDefault
     @ondrop="@(e => OnContainerDrop(e, container))" @ondrop:preventDefault @ondrop:stopPropagation
     style="position:relative; margin:4px; outline:...">

    @if (isSelected)
    {
        <!-- 拖拽手柄和删除按钮 -->
        <div draggable="true"
             @ondragstart="@((e) => OnWidgetDragStart(e, container))"
             @ondragstart:stopPropagation
             style="pointer-events:auto; cursor:move;">
            ⋮⋮ @container.Label
        </div>
    }

    @RenderContainerDesign(container)
</div>
```

**普通组件Wrapper**:

```razor
<div class="container-child-widget @(isSelected ? "selected" : "")"
     draggable="true"
     data-widget-id="@widget.Id"
     @ondragstart="@((e) => OnWidgetDragStart(e, widget))"
     @ondragstart:stopPropagation
     @ondragend="OnDragEnd"
     @onclick="@(() => SelectWidget(widget))"
     style="...">

    <!-- 组件内容 -->
</div>
```

---

## 状态管理

### 拖放状态生命周期

```
1. 初始状态
   ├─ draggedDefinition = null
   ├─ draggedWidget = null
   ├─ draggedParentContainer = null
   ├─ dropSucceeded = false
   ├─ isDragging = false
   └─ isProcessingDrop = false

2. Dragstart (从工具箱)
   ├─ draggedDefinition = Input定义
   ├─ isDragging = true
   └─ JS: body.is-dragging 类添加

3. Drop开始处理
   ├─ isProcessingDrop = true ⚠️ Guard开启
   ├─ 快照: definition = draggedDefinition
   └─ await getInsertIndex()
       └─ (dragend触发，但被guard阻止)

4. Drop成功
   ├─ 组件插入
   ├─ dropSucceeded = true
   ├─ draggedDefinition = null
   └─ isProcessingDrop = false ⚠️ Guard关闭

5. Dragend
   ├─ if (isProcessingDrop) return; ✓
   ├─ if (!dropSucceeded) 清空状态
   ├─ isDragging = false
   └─ JS: body.is-dragging 类移除
```

### Guard机制

**问题**: `await` 期间浏览器触发 `dragend`，清空了状态

**解决**: `isProcessingDrop` guard标志

```csharp
// Drop处理
isProcessingDrop = true;  // 1. 开启guard
try {
    // 保存快照
    var definition = draggedDefinition;

    // 安全await
    await SomeAsyncCall();

    // 使用快照（不是字段）
    if (definition != null) { ... }
} finally {
    isProcessingDrop = false;  // 2. 关闭guard
}

// Dragend处理
if (isProcessingDrop) return;  // 3. 检查guard
// 只有guard关闭时才清空状态
```

---

## DOM结构

### 顶层结构

```html
<div class="designer-canvas">
    <div class="layout-widgets-container">
        <!-- 主画布的组件 -->
        <div class="layout-widget">...</div>
        <div class="layout-widget">...</div>

        <!-- 容器（例如Section） -->
        <div class="layout-widget">
            <div class="container-child-wrapper"
                 data-widget-id="section-1">

                <!-- SectionDesignRenderer -->
                <div class="frame-drop-zone"
                     data-container-id="section-1">

                    <!-- 容器内的子组件 -->
                    <div class="container-child-widget"
                         data-widget-id="input-1">
                        Input组件内容
                    </div>

                    <div class="container-child-wrapper"
                         data-widget-id="frame-1">

                        <!-- 嵌套容器 -->
                        <div class="frame-drop-zone"
                             data-container-id="frame-1">
                            <!-- 更深层的子组件 -->
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
```

### 元素职责

| 元素 | 职责 | 关键属性 |
|------|------|----------|
| `.layout-widget` | 主画布组件外壳 | `draggable="true"` |
| `.container-child-wrapper` | 容器组件外壳 | `@ondrop`, `@ondragover`, `data-widget-id` |
| `.container-child-widget` | 普通组件外壳 | `draggable="true"`, `data-widget-id` |
| `.frame-drop-zone` | 容器的drop接收区 | `@ondrop`, `@ondragover`, `data-container-id` |

---

## CSS策略

### pointer-events控制

**目标**: 拖拽时禁用子元素交互，但保持drop zone可接收

**策略**: 基于 `body.is-dragging` 类动态控制

```css
/* 非拖拽时：所有元素正常交互 */
.frame-drop-zone { pointer-events: auto; }
.container-child-wrapper { pointer-events: auto; }

/* 拖拽时：frame-drop-zone必须可接收drop */
body.is-dragging .frame-drop-zone,
body.is-dragging .container-child-wrapper > .frame-drop-zone {
    pointer-events: auto !important;
}

/* 拖拽时：子控件wrapper可接收drop */
body.is-dragging .frame-drop-zone > .container-child-widget,
body.is-dragging .frame-drop-zone > .container-child-wrapper {
    pointer-events: auto !important;
}

/* 拖拽时：wrapper内部的内容禁用交互（避免干扰drop） */
body.is-dragging .frame-drop-zone > .container-child-widget > *:not(.frame-drop-zone),
body.is-dragging .frame-drop-zone > .container-child-wrapper > *:not(.frame-drop-zone) {
    pointer-events: none !important;
}
```

**关键点**:
- `:not(.frame-drop-zone)` 确保嵌套的drop zone不被禁用
- 高特异性选择器避免被覆盖
- `!important` 确保优先级

### 视觉反馈

```css
/* Hover效果 */
.frame-drop-zone:hover {
    border-color: #40a9ff;
    background: #f0f9ff;
}

/* 选中效果 */
.container-child-wrapper.selected {
    outline: 2px solid #1890ff;
    outline-offset: 2px;
}

/* Drop marker（由JavaScript动态添加） */
.drop-marker {
    position: absolute;
    background: #1890ff;
    height: 2px;
    pointer-events: none;
    z-index: 1000;
}
```

---

## JavaScript集成

### bobcrm.initDragDrop()

**职责**: 初始化全局拖放钩子

```javascript
initDragDrop: function() {
    if (!this._dragDropInitialized) {
        // dragstart事件：添加is-dragging类
        document.addEventListener('dragstart', function(e) {
            document.body.classList.add('is-dragging');
            // 设置dataTransfer...
        }, true);

        // dragend事件：移除is-dragging类
        document.addEventListener('dragend', function(e) {
            document.body.classList.remove('is-dragging');
            window.bobcrm?.clearDropMarker?.();
        }, true);

        this._dragDropInitialized = true;
    }
}
```

### bobcrm.getInsertIndex()

**职责**: 计算插入索引，基于鼠标位置

**算法**:

```javascript
getInsertIndex: function(containerSelector, x, y) {
    const container = document.querySelector(containerSelector);
    if (!container) return -1;

    // 获取所有子控件
    const isFrameDropZone = container.classList.contains('frame-drop-zone');
    const widgetSelector = isFrameDropZone
        ? '.container-child-widget, .container-child-wrapper'
        : '.layout-widget';
    const widgets = Array.from(container.querySelectorAll(widgetSelector));

    if (widgets.length === 0) return 0;

    // 按行分组（垂直容差12px）
    const rows = [];
    for (let w of widgets) {
        const rect = w.getBoundingClientRect();
        // 分组逻辑...
    }

    // 选择目标行（基于Y坐标）
    let row = rows.find(r => y >= r.top && y <= r.bottom);
    if (!row) {
        row = rows.reduce((best, r) =>
            Math.abs(y - r.center) < Math.abs(y - best.center) ? r : best);
    }

    // 在行内找插入位置（基于X坐标）
    const items = row.items.sort((a, b) => a.rect.left - b.rect.left);
    for (let item of items) {
        const center = item.rect.left + item.rect.width / 2;
        if (x < center) return item.idx;
    }
    return items[items.length - 1].idx + 1;
}
```

### bobcrm.updateDropMarker()

**职责**: 显示drop位置的视觉标记

```javascript
updateDropMarker: function(containerSelector, x, y) {
    const insertIndex = this.getInsertIndex(containerSelector, x, y);
    // 创建或更新marker元素
    // 定位到插入位置...
}
```

---

## 最佳实践

### 1. 事件处理器配置

```razor
<!-- ✓ 正确：完整的drop处理器配置 -->
<div @ondragover="OnDragOver" @ondragover:preventDefault
     @ondrop="OnDrop" @ondrop:preventDefault @ondrop:stopPropagation>

<!-- ✗ 错误：缺少preventDefault -->
<div @ondragover="OnDragOver"
     @ondrop="OnDrop">  <!-- 浏览器不允许drop -->

<!-- ✗ 错误：缺少stopPropagation -->
<div @ondrop="OnDrop" @ondrop:preventDefault>  <!-- 事件重复处理 -->
```

### 2. 状态快照

```csharp
// ✓ 正确：第一行保存快照
private async Task OnDrop(...) {
    var definition = draggedDefinition;  // 立即保存
    await SomeAsyncCall();
    if (definition != null) { ... }  // 使用快照
}

// ✗ 错误：await后保存快照
private async Task OnDrop(...) {
    await SomeAsyncCall();  // dragend可能已清空字段
    var definition = draggedDefinition;  // 可能是null
}
```

### 3. Guard模式

```csharp
// ✓ 正确：Guard + try-finally
private async Task OnDrop(...) {
    isProcessing = true;
    try { ... }
    finally { isProcessing = false; }  // 确保重置
}

// OnDragEnd中检查
if (isProcessing) return;

// ✗ 错误：没有finally
private async Task OnDrop(...) {
    isProcessing = true;
    ...
    isProcessing = false;  // 异常时不执行
}
```

### 4. CSS选择器

```css
/* ✓ 正确：使用:not()排除特定元素 */
body.is-dragging .wrapper > *:not(.drop-zone) {
    pointer-events: none !important;
}

/* ✗ 错误：禁用所有子元素 */
body.is-dragging .wrapper > * {
    pointer-events: none !important;  /* drop-zone也被禁用 */
}
```

---

## 性能考虑

### 1. 避免频繁重新渲染

```csharp
// ✓ 正确：dragstart不触发重新渲染
private async Task OnDragStart(...) {
    isDragging = true;  // 只设置标志
    // 不调用 StateHasChanged()
}

// ✗ 错误：dragstart触发重新渲染
private async Task OnDragStart(...) {
    isDragging = true;
    StateHasChanged();  // ❌ DOM替换导致拖拽终止
}
```

### 2. 事件委托

- 使用 `@ondrop:stopPropagation` 避免事件冒泡
- 在正确的层级处理事件，避免重复处理
- 利用浏览器的事件捕获和冒泡机制

### 3. JavaScript调用优化

```csharp
// ✓ 正确：一次调用获取所有信息
var insertIndex = await JS.InvokeAsync<int>("bobcrm.getInsertIndex", ...);

// ✗ 错误：多次调用
var widgets = await JS.InvokeAsync<List<string>>("getWidgets");
var index = await JS.InvokeAsync<int>("calculateIndex", widgets);
```

---

## 调试技巧

### 1. 事件流追踪

```csharp
// 在关键点添加日志
await JS.InvokeVoidAsync("console.log", $"[OnDragStart] definition={definition.Type}");
await JS.InvokeVoidAsync("console.log", $"[OnDrop] insertIndex={insertIndex}");
await JS.InvokeVoidAsync("console.log", $"[OnDragEnd] dropSucceeded={dropSucceeded}");
```

### 2. 浏览器DevTools

- **Elements面板**: 检查DOM结构和CSS样式
- **Event Listeners**: 查看元素上的事件监听器
- **Console**: 查看JavaScript日志和错误
- **Network**: 检查Blazor SignalR连接

### 3. 状态验证

```csharp
// 验证快照是否成功保存
await JS.InvokeVoidAsync("console.log",
    $"Snapshot: def={definition?.Type ?? "null"}, widget={movingWidget?.Id ?? "null"}");
```

---

## 扩展性

### 添加新的容器类型

1. 创建容器Widget类（继承`ContainerWidget`）
2. 创建设计态渲染器（实现相同的接口）
3. 在`RenderContainerDesign`中添加case

```csharp
case CustomContainerWidget custom:
    <CustomContainerDesignRenderer
        Container="@custom"
        RenderChild="@RenderDesignWidget"
        OnDrop="@(e => OnContainerDrop(e, custom))"
        OnDragOver="@OnDragOver" />
    break;
```

4. 确保渲染器包含：
   - `.frame-drop-zone` 类
   - `data-container-id` 属性
   - `@ondrop` 和 `@ondragover` 处理器

### 添加拖放验证

```csharp
private async Task OnContainerDrop(...) {
    // 验证逻辑
    if (!IsValidDrop(targetContainer, definition ?? movingWidget)) {
        await JS.InvokeVoidAsync("console.warn", "Invalid drop");
        return;
    }

    // 正常处理...
}

private bool IsValidDrop(ContainerWidget container, object item) {
    // 检查容器类型
    // 检查组件兼容性
    // 检查嵌套深度
    return true;
}
```

---

## 未来改进

### 1. 拖放动画

```css
.container-child-widget {
    transition: transform 0.2s ease;
}

.container-child-widget.dragging {
    opacity: 0.5;
    transform: scale(0.95);
}
```

### 2. 撤销/重做

```csharp
private Stack<LayoutSnapshot> undoStack = new();
private Stack<LayoutSnapshot> redoStack = new();

private void SaveSnapshot() {
    undoStack.Push(new LayoutSnapshot(layoutWidgets));
    redoStack.Clear();
}
```

### 3. 批量操作

```csharp
private List<DraggableWidget> selectedWidgets = new();

private async Task OnMultiDrop(...) {
    foreach (var widget in selectedWidgets) {
        // 批量插入
    }
}
```

---

**文档维护**: 此文档应与代码同步更新
**最后更新**: 2025-11-07
**维护者**: Claude & Development Team
