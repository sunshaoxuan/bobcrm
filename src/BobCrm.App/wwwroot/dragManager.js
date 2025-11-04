/**
 * 通用拖拽管理器 - 负责鼠标事件捕获和坐标计算
 * 采用事件驱动架构，不包含业务逻辑
 */
class DragManager {
  constructor() {
    this.activeSession = null;
    this.dotNetRef = null;
  }

  /**
   * 注册.NET回调引用
   */
  registerCallback(dotNetRef) {
    this.dotNetRef = dotNetRef;
  }

  /**
   * 开始调整大小会话
   * @param {string} widgetId - 控件ID
   * @param {number} startX - 起始X坐标
   * @param {number} startY - 起始Y坐标
   * @param {string} direction - 方向: 'horizontal', 'vertical', 'both'
   */
  startResize(widgetId, startX, startY, direction = 'horizontal') {
    if (this.activeSession) {
      console.warn('[DragManager] Previous session not ended');
      this.endResize();
    }

    this.activeSession = {
      type: 'resize',
      widgetId,
      startX,
      startY,
      lastX: startX,
      lastY: startY,
      direction
    };

    // 绑定全局事件
    document.addEventListener('mousemove', this._onMouseMove);
    document.addEventListener('mouseup', this._onMouseUp);

    // 设置样式
    document.body.style.userSelect = 'none';
    if (direction === 'horizontal') {
      document.body.style.cursor = 'ew-resize';
    } else if (direction === 'vertical') {
      document.body.style.cursor = 'ns-resize';
    } else {
      document.body.style.cursor = 'nwse-resize';
    }

    console.log('[DragManager] Resize session started:', widgetId, direction);
  }

  /**
   * 开始拖动会话（预留接口）
   */
  startDrag(widgetId, startX, startY) {
    if (this.activeSession) {
      console.warn('[DragManager] Previous session not ended');
      this.endDrag();
    }

    this.activeSession = {
      type: 'drag',
      widgetId,
      startX,
      startY,
      lastX: startX,
      lastY: startY
    };

    document.addEventListener('mousemove', this._onMouseMove);
    document.addEventListener('mouseup', this._onMouseUp);
    document.body.style.userSelect = 'none';
    document.body.style.cursor = 'move';

    console.log('[DragManager] Drag session started:', widgetId);
  }

  /**
   * 鼠标移动处理
   */
  _onMouseMove = (e) => {
    if (!this.activeSession) return;

    const session = this.activeSession;
    const deltaX = e.clientX - session.lastX;
    const deltaY = e.clientY - session.lastY;
    const totalDeltaX = e.clientX - session.startX;
    const totalDeltaY = e.clientY - session.startY;

    // 避免无效更新
    if (deltaX === 0 && deltaY === 0) return;

    // 更新最后位置
    session.lastX = e.clientX;
    session.lastY = e.clientY;

    // 触发.NET回调
    if (this.dotNetRef) {
      if (session.type === 'resize') {
        console.log('[DragManager] Calling OnResizeDrag:', session.widgetId, 'deltaX:', deltaX, 'totalDeltaX:', totalDeltaX);
        this.dotNetRef.invokeMethodAsync('OnResizeDrag',
          session.widgetId,
          session.direction,
          deltaX,
          deltaY,
          totalDeltaX,
          totalDeltaY
        ).catch(err => console.error('[DragManager] OnResizeDrag failed:', err));
      } else if (session.type === 'drag') {
        this.dotNetRef.invokeMethodAsync('OnPositionDrag',
          session.widgetId,
          deltaX,
          deltaY,
          totalDeltaX,
          totalDeltaY
        ).catch(err => console.error('[DragManager] OnPositionDrag failed:', err));
      }
    } else {
      console.warn('[DragManager] No dotNetRef registered!');
    }
  }

  /**
   * 鼠标释放处理
   */
  _onMouseUp = (e) => {
    if (!this.activeSession) return;

    const session = this.activeSession;
    const totalDeltaX = e.clientX - session.startX;
    const totalDeltaY = e.clientY - session.startY;

    // 触发结束事件
    if (this.dotNetRef) {
      if (session.type === 'resize') {
        this.dotNetRef.invokeMethodAsync('OnResizeEnd',
          session.widgetId,
          session.direction,
          totalDeltaX,
          totalDeltaY
        );
      } else if (session.type === 'drag') {
        this.dotNetRef.invokeMethodAsync('OnPositionDragEnd',
          session.widgetId,
          totalDeltaX,
          totalDeltaY
        );
      }
    }

    this.endResize();
  }

  /**
   * 结束调整大小会话
   */
  endResize() {
    if (!this.activeSession || this.activeSession.type !== 'resize') return;
    this._cleanupSession();
  }

  /**
   * 结束拖动会话
   */
  endDrag() {
    if (!this.activeSession || this.activeSession.type !== 'drag') return;
    this._cleanupSession();
  }

  /**
   * 清理会话
   */
  _cleanupSession() {
    document.removeEventListener('mousemove', this._onMouseMove);
    document.removeEventListener('mouseup', this._onMouseUp);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';

    console.log('[DragManager] Session ended:', this.activeSession?.widgetId);
    this.activeSession = null;
  }
}

// 全局单例
window.dragManager = new DragManager();

// 兼容旧接口（用于平滑迁移）
window.bobcrm = window.bobcrm || {};
window.bobcrm.dragManager = window.dragManager;
