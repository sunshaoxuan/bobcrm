# 容器拖放功能故障排除指南

## 目录
- [快速诊断流程](#快速诊断流程)
- [常见问题和解决方案](#常见问题和解决方案)
- [调试工具和方法](#调试工具和方法)
- [检查清单](#检查清单)
- [日志分析](#日志分析)

---

## 快速诊断流程

当遇到拖放功能问题时，按以下顺序检查：

```
1. 组件是否可拖动？
   ├─ 是 → 继续
   └─ 否 → 检查 draggable="true"

2. 拖动时有视觉反馈？
   ├─ 是 → 继续
   └─ 否 → 检查 CSS 和 JS 钩子

3. 控制台有 OnDragStart 日志？
   ├─ 是 → 继续
   └─ 否 → 检查事件处理器

4. Drop zone 有高亮效果？
   ├─ 是 → 继续
   └─ 否 → 检查 pointer-events

5. 控制台有 OnContainerDrop 日志？
   ├─ 是 → 继续
   └─ 否 → 检查 @ondragover 和 preventDefault

6. 控制台有 "Added new widget" 日志？
   ├─ 是 → 继续
   └─ 否 → 检查竞态条件和快照

7. 组件出现在 UI 中？
   ├─ 是 → ✓ 成功
   └─ 否 → 检查 StateHasChanged
```

---

## 常见问题和解决方案

### 问题1: 组件无法拖动

**症状**:
- 鼠标悬停时没有可拖动的光标
- 点击拖动没有任何反应

**可能原因**:

1. **缺少 draggable 属性**
   ```razor
   <!-- ❌ 错误 -->
   <div class="component-item">...</div>

   <!-- ✓ 正确 -->
   <div class="component-item" draggable="true">...</div>
   ```

2. **pointer-events 被禁用**
   ```css
   /* 检查元素的 computed style */
   .component-item {
       pointer-events: none; /* ❌ 会阻止拖动 */
   }
   ```

3. **事件处理器缺失**
   ```razor
   <!-- ✓ 需要 @ondragstart -->
   <div draggable="true"
        @ondragstart="OnDragStart"
        @ondragstart:preventDefault="false">
   ```

**解决方案**:
1. 确保元素有 `draggable="true"`
2. 检查CSS中没有 `pointer-events:none`
3. 添加 `@ondragstart` 处理器
4. 设置 `@ondragstart:preventDefault="false"`

---

### 问题2: 拖动时没有视觉反馈

**症状**:
- 组件可拖动，但拖动时没有任何视觉变化
- `body.is-dragging` 类未添加

**可能原因**:

1. **JS 钩子未初始化**
   ```csharp
   // 检查 OnAfterRenderAsync 中是否调用
   await JS.InvokeVoidAsync("bobcrm.initDragDrop");
   ```

2. **JS 函数未定义**
   ```javascript
   // 检查 app.js 中是否有
   window.bobcrm = {
       initDragDrop: function() { ... }
   };
   ```

**解决方案**:
1. 在 `OnAfterRenderAsync` 中调用 `bobcrm.initDragDrop`
2. 确保 app.js 正确加载
3. 检查浏览器控制台是否有 JS 错误

**验证**:
```javascript
// 在浏览器控制台执行
document.body.classList.contains('is-dragging')  // 拖动时应返回 true
```

---

### 问题3: Drop zone 不接受 drop

**症状**:
- 拖动组件到容器上，光标显示"禁止"图标
- Drop 事件从不触发
- 控制台只有 `OnDragStart`，没有 `OnDrop`

**可能原因**:

1. **缺少 @ondragover 处理器**
   ```razor
   <!-- ❌ 错误：浏览器不允许 drop -->
   <div @ondrop="OnDrop">

   <!-- ✓ 正确：必须有 ondragover + preventDefault -->
   <div @ondragover="OnDragOver" @ondragover:preventDefault
        @ondrop="OnDrop" @ondrop:preventDefault>
   ```

2. **pointer-events 阻止事件**
   ```css
   /* ❌ 错误：drop zone 被禁用 */
   .frame-drop-zone {
       pointer-events: none;
   }

   /* ✓ 正确：drop zone 必须可接收事件 */
   body.is-dragging .frame-drop-zone {
       pointer-events: auto !important;
   }
   ```

3. **外层元素拦截事件**
   ```html
   <!-- ❌ 外层元素阻止事件传播 -->
   <div style="pointer-events:none;">
       <div class="frame-drop-zone" style="pointer-events:auto;">
           <!-- auto 被继承的 none 覆盖 -->
       </div>
   </div>
   ```

**解决方案**:
1. 在所有可能接收 drop 的元素上添加 `@ondragover:preventDefault`
2. 检查 CSS，确保 drop zone 的 `pointer-events:auto`
3. 检查 DOM 层级，确保没有外层元素阻止事件

**验证**:
```javascript
// 在浏览器控制台执行
const dropZone = document.querySelector('.frame-drop-zone');
const style = window.getComputedStyle(dropZone);
console.log(style.pointerEvents);  // 应该是 'auto'
```

---

### 问题4: OnContainerDrop 触发但组件未插入

**症状**:
- 控制台显示 `OnContainerDrop start`
- 但没有 `Added new widget` 日志
- 组件未出现在容器中

**可能原因**:

1. **竞态条件：快照为 null**
   ```csharp
   // ❌ 错误：在 await 后保存快照
   private async Task OnContainerDrop(...) {
       await JS.InvokeVoidAsync("console.log", ...);  // dragend 已清空字段
       var definition = draggedDefinition;  // null!
   }
   ```

2. **Guard 标志未设置**
   ```csharp
   // ❌ OnDragEnd 清空了状态
   private async Task OnDragEnd(...) {
       // 没有检查 isProcessingDrop
       draggedDefinition = null;
   }
   ```

**解决方案**:
```csharp
// ✓ 正确：第一行保存快照和设置 guard
private async Task OnContainerDrop(...) {
    isProcessingDrop = true;  // Guard
    var definition = draggedDefinition;  // 立即快照
    var movingWidget = draggedWidget;

    try {
        await JS.InvokeVoidAsync("console.log",
            $"def={definition?.Type ?? "null"}");  // 验证快照

        if (definition != null) {
            // 使用快照，不是字段
        }
    } finally {
        isProcessingDrop = false;
    }
}

// OnDragEnd 检查 guard
private async Task OnDragEnd(...) {
    if (isProcessingDrop) return;  // Guard
    // 清空状态...
}
```

**验证**:
查看日志中的快照值：
```
[FormDesigner] OnContainerDrop start: def=Input, widget=null  ✓ 有值
[FormDesigner] OnContainerDrop start: def=null, widget=null   ❌ null
```

---

### 问题5: 容器只接受第一个组件

**症状**:
- 第一个组件成功插入 ✓
- 第二个、第三个...都失败 ❌
- 容器只能有一个子元素

**可能原因**:

1. **缺少 @ondrop:stopPropagation**
   ```razor
   <!-- ❌ 错误：事件冒泡，重复处理 -->
   <div class="container-child-wrapper"
        @ondrop="@(e => OnContainerDrop(e, container))"
        @ondrop:preventDefault>

   <!-- ✓ 正确：阻止冒泡 -->
   <div class="container-child-wrapper"
        @ondrop="@(e => OnContainerDrop(e, container))"
        @ondrop:preventDefault
        @ondrop:stopPropagation>
   ```

2. **事件重复处理**
   ```
   用户 drop → wrapper 触发 OnContainerDrop
             → 事件冒泡到 frame-drop-zone
             → 再次触发 OnContainerDrop
             → 冲突/错误
   ```

**解决方案**:
在 wrapper 上添加 `@ondrop:stopPropagation`

**验证**:
控制台应该只显示一次 `OnContainerDrop start`，不是多次。

---

### 问题6: 拖动容器内组件时容器被删除

**症状**:
- 拖动容器内的组件
- 整个容器消失

**可能原因**:

**dragstart 事件冒泡到容器**
```razor
<!-- ❌ 子组件的 dragstart 冒泡到容器 -->
<div class="container" @ondragstart="OnContainerDragStart">
    <div class="child" draggable="true">
        <!-- dragstart 冒泡 → 容器被拖动 -->
    </div>
</div>
```

**解决方案**:
```razor
<!-- ✓ 子组件阻止 dragstart 冒泡 -->
<div class="child"
     draggable="true"
     @ondragstart="OnWidgetDragStart"
     @ondragstart:stopPropagation>
```

---

### 问题7: Drop marker 位置错误

**症状**:
- Drop marker 始终显示在第一个组件前面
- 或者根本不显示

**可能原因**:

1. **JavaScript 找不到子元素**
   ```javascript
   // 检查选择器是否正确
   const widgets = container.querySelectorAll('.container-child-widget');
   console.log(widgets.length);  // 应该 > 0
   ```

2. **CSS 类名错误**
   ```razor
   <!-- ❌ 缺少用于定位的类 -->
   <div data-widget-id="...">

   <!-- ✓ 正确 -->
   <div class="container-child-widget" data-widget-id="...">
   ```

**解决方案**:
1. 确保所有子元素有 `.container-child-widget` 或 `.container-child-wrapper` 类
2. 检查 `bobcrm.getInsertIndex` 的选择器
3. 使用浏览器 DevTools 检查元素

---

### 问题8: StateHasChanged 导致拖拽中断

**症状**:
- 开始拖动
- 立即终止，无法完成 drop

**可能原因**:

**在 dragstart 中调用 StateHasChanged**
```csharp
// ❌ 错误：DOM 替换导致拖拽终止
private async Task OnDragStart(...) {
    isDragging = true;
    StateHasChanged();  // ❌ 浏览器终止拖拽
}
```

**解决方案**:
```csharp
// ✓ 正确：不在 dragstart 中重新渲染
private async Task OnDragStart(...) {
    isDragging = true;  // 只设置标志
    // 不调用 StateHasChanged()
    // 依赖 JS 的 body.is-dragging 类控制 CSS
}
```

---

## 调试工具和方法

### 1. 浏览器 DevTools

#### Elements 面板

**检查 DOM 结构**:
```
右键点击元素 → Inspect
查看：
- 元素的类名
- data-* 属性
- Computed 样式中的 pointer-events
```

**检查事件监听器**:
```
Elements → Event Listeners
查看：
- dragstart 监听器
- dragover 监听器
- drop 监听器
```

#### Console 面板

**执行诊断代码**:
```javascript
// 检查 body.is-dragging 状态
document.body.classList.contains('is-dragging')

// 检查元素的 pointer-events
const el = document.querySelector('.frame-drop-zone');
window.getComputedStyle(el).pointerEvents

// 检查事件监听器
getEventListeners(document.querySelector('.frame-drop-zone'))

// 测试 JS 函数
bobcrm.getInsertIndex('.frame-drop-zone', 100, 100)
```

### 2. Blazor Server 日志

**服务器端日志位置**:
```
logs/app_YYYYMMDD_HHmmss.out.log
```

**关键日志**:
```
[FormDesigner] OnDragStart: Input
[FormDesigner] OnContainerDrop start: container=xxx, def=Input, widget=null
[FormDesigner] Added new widget to container at index 0
```

**如果看不到日志**:
```csharp
// 添加更多日志
await JS.InvokeVoidAsync("console.log", $"[Debug] Point A reached");
```

### 3. 添加诊断日志

**在关键点添加日志**:

```csharp
// OnDragStart
await JS.InvokeVoidAsync("console.log",
    $"[OnDragStart] definition={definition.Type}");

// OnContainerDrop 入口
await JS.InvokeVoidAsync("console.log",
    $"[OnContainerDrop] START: container={targetContainer.Id}");

// 快照验证
await JS.InvokeVoidAsync("console.log",
    $"[OnContainerDrop] Snapshot: def={definition?.Type ?? "null"}, " +
    $"widget={movingWidget?.Id ?? "null"}");

// getInsertIndex 结果
await JS.InvokeVoidAsync("console.log",
    $"[OnContainerDrop] insertIndex={insertIndex}");

// 插入成功
await JS.InvokeVoidAsync("console.log",
    $"[OnContainerDrop] SUCCESS: Added widget at index {insertIndex}");

// OnDragEnd
await JS.InvokeVoidAsync("console.log",
    $"[OnDragEnd] isProcessingDrop={isProcessingDrop}, dropSucceeded={dropSucceeded}");
```

### 4. CSS 调试

**高亮 drop zone**:
```css
.frame-drop-zone {
    background: rgba(255, 0, 0, 0.1) !important;  /* 红色半透明 */
    border: 2px solid red !important;
}
```

**检查 pointer-events**:
```javascript
// 拖拽时检查
document.addEventListener('dragstart', () => {
    setTimeout(() => {
        const zones = document.querySelectorAll('.frame-drop-zone');
        zones.forEach(zone => {
            const pe = window.getComputedStyle(zone).pointerEvents;
            console.log(`Zone ${zone.dataset.containerId}: ${pe}`);
        });
    }, 100);
});
```

---

## 检查清单

### 拖动源（被拖动的元素）

- [ ] 有 `draggable="true"` 属性
- [ ] 有 `@ondragstart` 处理器
- [ ] 有 `@ondragstart:preventDefault="false"`
- [ ] 容器内子元素有 `@ondragstart:stopPropagation`
- [ ] `pointer-events` 不是 `none`

### 拖放目标（接收 drop 的元素）

- [ ] 有 `@ondragover` 处理器
- [ ] 有 `@ondragover:preventDefault`
- [ ] 有 `@ondrop` 处理器
- [ ] 有 `@ondrop:preventDefault`
- [ ] Wrapper 有 `@ondrop:stopPropagation`
- [ ] CSS 中 `pointer-events: auto`
- [ ] 有 `data-container-id` 或 `data-widget-id`
- [ ] 外层元素没有阻止事件传播

### 事件处理器

- [ ] `OnDragStart` 不调用 `StateHasChanged()`
- [ ] `OnContainerDrop` 第一行设置 guard 和保存快照
- [ ] `OnContainerDrop` 有 `try-finally` 确保 guard 重置
- [ ] `OnDragEnd` 检查 `isProcessingDrop` guard
- [ ] 使用快照（局部变量），不是字段

### JavaScript

- [ ] `bobcrm.initDragDrop()` 已调用
- [ ] `bobcrm.getInsertIndex()` 正常工作
- [ ] app.js 正确加载，没有 JS 错误
- [ ] 拖拽时 `body.is-dragging` 类添加

### CSS

- [ ] `body.is-dragging` 规则存在
- [ ] `.frame-drop-zone` 有 `pointer-events: auto !important`
- [ ] 使用 `:not(.frame-drop-zone)` 排除嵌套 drop zone
- [ ] CSS 选择器特异性足够高

---

## 日志分析

### 正常的日志序列

**从工具箱拖入组件到容器**:
```
1. [FormDesigner] OnDragStart: Input
2. [FormDesigner] OnContainerDrop start: container=section-1, def=Input, widget=null
3. [FormDesigner] Added new widget to container at index 0
```

**容器内重新排序**:
```
1. [FormDesigner] OnWidgetDragStart: widget-1
2. [FormDesigner] OnContainerDrop start: container=section-1, def=null, widget=widget-1
3. [FormDesigner] Moved widget to container at index 2
```

### 异常日志模式

#### 模式1: 没有 OnContainerDrop 日志

```
✓ [FormDesigner] OnDragStart: Input
❌ (没有 OnContainerDrop)
```

**问题**: Drop 事件未触发

**排查**:
1. 检查 `@ondragover:preventDefault`
2. 检查 `pointer-events`
3. 检查浏览器控制台是否有 JS 错误

#### 模式2: 有 OnContainerDrop start，但没有 Added

```
✓ [FormDesigner] OnDragStart: Input
✓ [FormDesigner] OnContainerDrop start: container=section-1, def=Input, widget=null
❌ (没有 "Added new widget")
```

**问题**: 插入逻辑未执行

**排查**:
1. 检查快照值（def 和 widget）
2. 检查是否有异常日志
3. 检查 `isProcessingDrop` guard

#### 模式3: 快照为 null

```
✓ [FormDesigner] OnDragStart: Input
❌ [FormDesigner] OnContainerDrop start: container=section-1, def=null, widget=null
```

**问题**: 竞态条件，快照保存时机错误

**排查**:
1. 检查快照是否在第一行
2. 检查是否在任何 await 之前
3. 检查 `OnDragEnd` 是否有 guard

#### 模式4: 重复的 OnContainerDrop

```
✓ [FormDesigner] OnDragStart: Input
✓ [FormDesigner] OnContainerDrop start: container=section-1, def=Input, widget=null
✓ [FormDesigner] OnContainerDrop start: container=section-1, def=Input, widget=null  ← 重复
```

**问题**: 事件冒泡，重复处理

**排查**:
1. 检查 wrapper 是否有 `@ondrop:stopPropagation`
2. 检查 frame-drop-zone 是否也有 `@ondrop`

---

## 快速修复脚本

### 验证配置脚本

在浏览器控制台执行：

```javascript
// 验证 JS 函数
console.log('bobcrm.initDragDrop:', typeof bobcrm?.initDragDrop);
console.log('bobcrm.getInsertIndex:', typeof bobcrm?.getInsertIndex);

// 验证 body.is-dragging
console.log('body.is-dragging:', document.body.classList.contains('is-dragging'));

// 验证 drop zone 配置
const zones = document.querySelectorAll('.frame-drop-zone');
console.log('Drop zones found:', zones.length);
zones.forEach((zone, i) => {
    const pe = window.getComputedStyle(zone).pointerEvents;
    console.log(`Zone ${i}: pointer-events=${pe}, id=${zone.dataset.containerId}`);
});

// 验证 wrapper 配置
const wrappers = document.querySelectorAll('.container-child-wrapper');
console.log('Wrappers found:', wrappers.length);
wrappers.forEach((w, i) => {
    const hasOndrop = !!w.ondrop;
    console.log(`Wrapper ${i}: has ondrop=${hasOndrop}, id=${w.dataset.widgetId}`);
});
```

### 重置拖放状态

如果拖放状态卡住：

```javascript
// 清除 is-dragging 类
document.body.classList.remove('is-dragging');

// 清除 drop marker
const markers = document.querySelectorAll('.drop-marker');
markers.forEach(m => m.remove());
```

---

## 联系和支持

如果以上方法都无法解决问题：

1. **收集信息**:
   - 完整的控制台日志
   - 服务器日志（logs/*.out.log）
   - 复现步骤
   - 浏览器和版本

2. **检查文档**:
   - [修复历史](./CONTAINER_DROP_FIX_HISTORY.md)
   - [设计文档](./CONTAINER_DROP_DESIGN.md)

3. **代码审查**:
   - 对比当前代码与工作版本（提交 `12546fd`）
   - 检查是否有新的修改破坏了功能

---

**文档维护**: 遇到新问题时，请更新此指南
**最后更新**: 2025-11-07
**维护者**: Claude & Development Team
