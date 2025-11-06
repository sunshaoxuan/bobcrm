/**
 * FormDesigner Resize 功能
 * 处理组件宽度调整的拖拽交互
 */
window.FormDesignerResize = {
    dotNetRef: null,
    widgetId: null,
    startX: 0,
    startWidth: 0,
    widgetElement: null,
    isResizing: false,

    /**
     * 开始 resize
     * @param {object} dotNetReference - .NET对象引用
     * @param {string} widgetId - 组件ID
     * @param {number} clientX - 鼠标起始X坐标
     */
    start: function (dotNetReference, widgetId, clientX) {
        this.dotNetRef = dotNetReference;
        this.widgetId = widgetId;
        this.startX = clientX;
        this.isResizing = true;

        // 查找组件元素（通过ID或data属性）
        this.widgetElement = document.querySelector(`[data-widget-id="${widgetId}"]`);
        
        if (!this.widgetElement) {
            console.warn('[FormDesignerResize] Widget element not found:', widgetId);
            // 即使没找到元素，也继续（可能在容器内）
        } else {
            // 获取当前宽度
            const rect = this.widgetElement.getBoundingClientRect();
            this.startWidth = Math.floor(rect.width);
            console.log('[FormDesignerResize] Start resize:', widgetId, 'width:', this.startWidth);
        }

        // 添加全局事件监听器
        document.addEventListener('mousemove', this.onMouseMove);
        document.addEventListener('mouseup', this.onMouseUp);
        
        // 添加视觉反馈
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';
    },

    /**
     * 鼠标移动处理
     */
    onMouseMove: function (e) {
        if (!window.FormDesignerResize.isResizing) return;

        const deltaX = e.clientX - window.FormDesignerResize.startX;
        const newWidth = Math.max(50, window.FormDesignerResize.startWidth + deltaX);

        // 调用 .NET 方法更新宽度
        if (window.FormDesignerResize.dotNetRef) {
            window.FormDesignerResize.dotNetRef.invokeMethodAsync(
                'OnResizeMove',
                window.FormDesignerResize.widgetId,
                Math.floor(newWidth)
            );
        }

        e.preventDefault();
        e.stopPropagation();
    },

    /**
     * 鼠标释放处理
     */
    onMouseUp: function (e) {
        if (!window.FormDesignerResize.isResizing) return;

        console.log('[FormDesignerResize] End resize');

        // 移除事件监听器
        document.removeEventListener('mousemove', window.FormDesignerResize.onMouseMove);
        document.removeEventListener('mouseup', window.FormDesignerResize.onMouseUp);

        // 恢复样式
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        // 调用 .NET 方法结束 resize
        if (window.FormDesignerResize.dotNetRef) {
            window.FormDesignerResize.dotNetRef.invokeMethodAsync('OnResizeEnd');
        }

        // 清理状态
        window.FormDesignerResize.isResizing = false;
        window.FormDesignerResize.dotNetRef = null;
        window.FormDesignerResize.widgetElement = null;

        e.preventDefault();
        e.stopPropagation();
    }
};

