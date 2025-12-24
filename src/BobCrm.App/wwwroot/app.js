window.bobcrm = {
  copyText: async function (text) {
    try {
      await navigator.clipboard.writeText(text || '');
      return true;
    } catch (e) {
      return false;
    }
  },
  downloadFile: function (filename, content, mime) {
    const blob = new Blob([content || ''], { type: mime || 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'download.txt';
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  },
  openUrl: function (url) {
    window.open(url, '_blank');
  },
  // 获取元素的实际宽度
  getElementWidth: function(element) {
    if (!element) return 0;
    const rect = element.getBoundingClientRect();
    return rect.width || element.clientWidth || 0;
  },
  // 通过选择器获取元素宽度
  getElementWidthBySelector: function(selector) {
    const element = document.querySelector(selector);
    return this.getElementWidth(element);
  },
  getRect: function (el) {
    if (!el) return { left: 0, top: 0, width: 0, height: 0 };
    const r = el.getBoundingClientRect();
    return { left: r.left, top: r.top, width: r.width, height: r.height };
  },
  setLang: function (lang) {
    try { document.documentElement.lang = (lang || 'ja'); } catch (e) { }
  },
  getOrigin: function () {
    try { return window.location.origin; } catch (e) { return ''; }
  },
  getCookie: function (name) {
    const m = document.cookie.match(new RegExp('(?:^|; )' + encodeURIComponent(name) + '=([^;]*)'));
    return m ? decodeURIComponent(m[1]) : null;
  },
  setCookie: function (name, value, days) {
    try {
      let expires = '';
      if (typeof days === 'number') {
        const d = new Date(); d.setTime(d.getTime() + days * 864e5);
        expires = '; expires=' + d.toUTCString();
      }
      document.cookie = encodeURIComponent(name) + '=' + encodeURIComponent(value || '') + expires + '; path=/';
    } catch (e) { }
  }
  , registerDesignerShortcuts: function (dotnetRef) {
    try {
      if (this._designerShortcutHandler) {
        window.removeEventListener('keydown', this._designerShortcutHandler, true);
      }

      this._designerDotNetRef = dotnetRef;
      this._designerShortcutHandler = (e) => {
        try {
          if (!e || !e.ctrlKey) return;

          const target = e.target;
          const tag = target && target.tagName ? String(target.tagName).toLowerCase() : '';
          const isEditable = target && (target.isContentEditable || tag === 'input' || tag === 'textarea' || tag === 'select');
          if (isEditable) return;

          const key = (e.key || '').toLowerCase();
          if (key === 'z') {
            e.preventDefault();
            e.stopPropagation();
            if (this._designerDotNetRef && this._designerDotNetRef.invokeMethodAsync) {
              this._designerDotNetRef.invokeMethodAsync('OnDesignerShortcut', 'undo');
            }
          } else if (key === 'y') {
            e.preventDefault();
            e.stopPropagation();
            if (this._designerDotNetRef && this._designerDotNetRef.invokeMethodAsync) {
              this._designerDotNetRef.invokeMethodAsync('OnDesignerShortcut', 'redo');
            }
          }
        } catch (_) { }
      };

      window.addEventListener('keydown', this._designerShortcutHandler, true);
    } catch (_) { }
  }
  , unregisterDesignerShortcuts: function () {
    try {
      if (this._designerShortcutHandler) {
        window.removeEventListener('keydown', this._designerShortcutHandler, true);
      }
    } catch (_) { }
    this._designerShortcutHandler = null;
    this._designerDotNetRef = null;
  }
  , registerCustomerEvents: function(dotnetRef){
    try { this._customerRef = dotnetRef; } catch (e) { }
  }
  , customerUpdated: function(id, code, name){
    try {
      if (this._customerRef && this._customerRef.invokeMethodAsync) {
        this._customerRef.invokeMethodAsync('OnCustomerUpdated', id | 0, code || '', name || '');
      }
    } catch (e) { }
  }
  , fetchJson: async function (url) {
    try {
      const resp = await fetch(url, { method: 'GET', mode: 'cors', credentials: 'omit' });
      if (!resp.ok) return null;
      return await resp.json();
    } catch (e) { return null; }
  }
  , setDragData: function (type, data) {
    // 保留兼容接口，实际 dragstart 中会读取 data-* 属性
  }
  , initDragDrop: function () {
    // Set up global dragstart handler to read data-* attributes and set dataTransfer
    if (!this._dragDropInitialized) {
      document.addEventListener('dragstart', function(e) {
        try {
          // 添加dragging class到body，用于CSS控制子控件的pointer-events
          document.body.classList.add('is-dragging');

          // Try to get data from the draggable element itself
          let dragElement = e.target;
          let dragType = dragElement.getAttribute('data-drag-type');
          let dragData = dragElement.getAttribute('data-drag-data');

          // If not found on target, check parent elements (for nested content)
          if (!dragData) {
            let parent = dragElement.parentElement;
            while (parent && !dragData) {
              dragType = parent.getAttribute('data-drag-type');
              dragData = parent.getAttribute('data-drag-data');
              if (dragData) {
                dragElement = parent;
                break;
              }
              parent = parent.parentElement;
            }
          }

          if (dragData) {
            e.dataTransfer.setData('text/plain', dragData);
            e.dataTransfer.effectAllowed = 'move';
            // Also store type for later use
            e.dataTransfer.setData('application/x-drag-type', dragType || '');
          }
        } catch (err) {
          // Ignore errors
        }
      }, true); // Use capture phase
      this._dragDropInitialized = true;
    }
  }
  , getElementAtPoint: function (x, y) {
    // Get element at specific coordinates
    try {
      const element = document.elementFromPoint(x, y);
      if (!element) return null;
      
      // Find the closest draggable widget or container
      let current = element;
      while (current) {
        const dragType = current.getAttribute('data-drag-type');
        const dragData = current.getAttribute('data-drag-data');
        if (dragType === 'widget' && dragData) {
          return { id: dragData, element: current };
        }
        // Check if it's the flex container
        if (current.classList && (current.classList.contains('layout-widget') || current.style.display === 'flex')) {
          const rect = current.getBoundingClientRect();
          return { element: current, x: x - rect.left, y: y - rect.top };
        }
        current = current.parentElement;
      }
      return null;
    } catch (e) {
      return null;
    }
  }
  , getInsertIndex: function (containerSelector, x, y) {
    // Lenient rule: container内任意位置均可放置；就近行 + 左/右侧
    try {
      const container = document.querySelector(containerSelector);
      if (!container) return -1;

      // 支持容器内的子控件（.container-child-widget 或 .container-child-wrapper）和主画布的控件（.layout-widget）
      const isFrameDropZone = container.classList.contains('frame-drop-zone');
      const widgetSelector = isFrameDropZone ? '.container-child-widget, .container-child-wrapper' : '.layout-widget';
      const widgets = Array.from(container.querySelectorAll(widgetSelector));
      if (widgets.length === 0) return 0;

      // Group widgets by row with vertical tolerance
      const rowTol = 12; // px
      const rows = [];
      for (let i = 0; i < widgets.length; i++) {
        const w = widgets[i];
        const r = w.getBoundingClientRect();
        if (!rows.length || Math.abs(r.top - rows[rows.length - 1].top) > rowTol) {
          rows.push({ top: r.top, bottom: r.bottom, items: [{ el: w, rect: r, idx: i }] });
        } else {
          const row = rows[rows.length - 1];
          row.bottom = Math.max(row.bottom, r.bottom);
          row.items.push({ el: w, rect: r, idx: i });
        }
      }

      // Choose target row by y
      let row = null;
      for (const rw of rows) {
        if (y >= rw.top - rowTol && y <= rw.bottom + rowTol) { row = rw; break; }
      }
      if (!row) {
        // Pick nearest row
        row = rows.reduce((best, rw) => (best && Math.abs(y - (best.top + best.bottom) / 2) < Math.abs(y - (rw.top + rw.bottom) / 2)) ? best : rw, null);
      }

      // Within row, find insertion by x against item centers
      const items = row.items.slice().sort((a, b) => a.rect.left - b.rect.left);
      // far left of first
      if (x <= items[0].rect.left + items[0].rect.width / 2) return items[0].idx;
      // far right of last
      if (x >= items[items.length - 1].rect.left + items[items.length - 1].rect.width / 2)
        return items[items.length - 1].idx + 1;

      for (let k = 0; k < items.length; k++) {
        const it = items[k];
        const center = it.rect.left + it.rect.width / 2;
        if (x < center) return it.idx;
        // between this and next => after this
        const next = items[k + 1];
        if (next && x >= center && x < (next.rect.left + next.rect.width / 2)) return it.idx + 1;
      }
      return widgets.length; // default append
    } catch (e) { return -1; }
  }
  , getInsertIndexStrict: function (containerSelector, x, y) {
    // Strict: decide before/after relative to the item under pointer
    try {
      const container = document.querySelector(containerSelector);
      if (!container) return -1;
      const isFrameDropZone = container.classList.contains('frame-drop-zone');
      const widgetSelector = isFrameDropZone ? '.container-child-widget, .container-child-wrapper' : '.layout-widget';
      const widgets = Array.from(container.querySelectorAll(widgetSelector));
      if (widgets.length === 0) return 0;

      const targetEl = document.elementFromPoint(x, y);
      const candidate = targetEl ? targetEl.closest(widgetSelector) : null;
      if (candidate && container.contains(candidate)) {
        const idx = widgets.indexOf(candidate);
        if (idx < 0) return widgets.length;
        const r = candidate.getBoundingClientRect();
        return x > r.left + r.width / 2 ? idx + 1 : idx;
      }
      // Fallbacks: above first -> 0; otherwise append
      const firstRect = widgets[0].getBoundingClientRect();
      if (y < firstRect.top) return 0;
      return widgets.length;
    } catch (e) { return -1; }
  }
  , getDropPosition: function (containerSelector, clientX, clientY, grid) {
    try {
      const container = document.querySelector(containerSelector);
      if (!container) return { x: 0, y: 0 };
      const rect = container.getBoundingClientRect();
      let x = clientX - rect.left; let y = clientY - rect.top;
      const g = Math.max(1, grid || 1);
      x = Math.max(0, Math.round(x / g) * g);
      y = Math.max(0, Math.round(y / g) * g);
      return { x, y };
    } catch (e) { return { x: 0, y: 0 }; }
  }
  , updateDropMarker: function (containerSelector, x, y) {
    // Show an absolutely positioned marker overlay; avoids flex stretching issues
    try {
      const container = document.querySelector(containerSelector);
      if (!container) return;

      // 支持容器内的子控件（.container-child-widget 或 .container-child-wrapper）和主画布的控件（.layout-widget）
      const isFrameDropZone = container.classList.contains('frame-drop-zone');
      const widgetSelector = isFrameDropZone ? '.container-child-widget, .container-child-wrapper' : '.layout-widget';
      const widgets = Array.from(container.querySelectorAll(widgetSelector));

      // 为每个container使用独立的marker，通过data属性关联
      const containerId = containerSelector;
      let marker = container.querySelector('.drop-marker[data-container="' + containerId.replace(/[^\w-]/g, '_') + '"]');

      if (!marker) {
        marker = document.createElement('div');
        marker.className = 'drop-marker';
        marker.setAttribute('data-container', containerId.replace(/[^\w-]/g, '_'));
        marker.style.position = 'absolute';
        marker.style.width = '0px';
        marker.style.borderLeft = '3px solid var(--primary)';
        marker.style.pointerEvents = 'none';
        marker.style.zIndex = '999';
        container.appendChild(marker);
      }

      if (getComputedStyle(container).position === 'static') {
        container.style.position = 'relative';
      }

      // Compute row and target index similar to getInsertIndex
      if (widgets.length === 0) {
        // Place at top-left with minimal height
        marker.style.left = '0px';
        marker.style.top = '0px';
        marker.style.height = '24px';
        marker.style.display = 'block';
        return;
      }

      const rowTol = 12;
      const rows = [];
      for (let i = 0; i < widgets.length; i++) {
        const w = widgets[i];
        const r = w.getBoundingClientRect();
        if (!rows.length || Math.abs(r.top - rows[rows.length - 1].top) > rowTol) {
          rows.push({ top: r.top, bottom: r.bottom, items: [{ el: w, rect: r, idx: i }] });
        } else {
          const row = rows[rows.length - 1];
          row.bottom = Math.max(row.bottom, r.bottom);
          row.items.push({ el: w, rect: r, idx: i });
        }
      }
      let row = null;
      for (const rw of rows) { if (y >= rw.top - rowTol && y <= rw.bottom + rowTol) { row = rw; break; } }
      if (!row) { row = rows.reduce((best, rw) => (best && Math.abs(y - (best.top + best.bottom) / 2) < Math.abs(y - (rw.top + rw.bottom) / 2)) ? best : rw, null); }

      const items = row.items.slice().sort((a, b) => a.rect.left - b.rect.left);
      let idx;
      if (x <= items[0].rect.left + items[0].rect.width / 2) idx = items[0].idx;
      else if (x >= items[items.length - 1].rect.left + items[items.length - 1].rect.width / 2) idx = items[items.length - 1].idx + 1;
      else {
        for (let k = 0; k < items.length; k++) {
          const it = items[k];
          const center = it.rect.left + it.rect.width / 2;
          const next = items[k + 1];
          if (x < center) { idx = it.idx; break; }
          if (next && x >= center && x < (next.rect.left + next.rect.width / 2)) { idx = it.idx + 1; break; }
        }
      }
      if (idx == null) idx = widgets.length;

      const containerRect = container.getBoundingClientRect();
      let leftPx; let heightPx; let topPx = (row.top - containerRect.top) + 'px';
      const rowHeight = Math.max(24, Math.round(row.bottom - row.top));
      heightPx = rowHeight + 'px';
      if (idx <= 0) leftPx = (items[0].rect.left - containerRect.left - 1) + 'px';
      else if (idx >= widgets.length) leftPx = (items[items.length - 1].rect.right - containerRect.left + 1) + 'px';
      else {
        // before items[idx] visually
        const beforeRect = widgets[idx].getBoundingClientRect();
        leftPx = (beforeRect.left - containerRect.left - 1) + 'px';
      }

      marker.style.left = leftPx;
      marker.style.top = topPx;
      marker.style.height = heightPx;
      marker.style.display = 'block';
    } catch (e) { }
  }
  , clearDropMarker: function () {
    try {
      // 隐藏所有drop markers
      const markers = document.querySelectorAll('.drop-marker');
      markers.forEach(m => m.style.display = 'none');
    } catch (e) { }
  }, debugDnD: function() {
    // Debug drag and drop events
    const events = ['dragenter', 'dragover', 'drop', 'dragleave', 'dragstart', 'dragend'];
    events.forEach(eventType => {
      document.addEventListener(eventType, (e) => {
        if (eventType === 'drop' || eventType === 'dragstart' || eventType === 'dragend') {
          console.log('=== [dnd] ' + eventType + ' ===');
          console.log('  target:', e.target?.className || e.target?.tagName);
          console.log('  dataTransfer.types:', e.dataTransfer?.types);
          console.log('  clientX/Y:', e.clientX, e.clientY);
        }
      }, true);
    });
    console.log('=== [dnd] Debug mode enabled ===');
  }

  // ===== Pointer-based Free Designer =====
  , freeDesigner: {
    _dragState: null,
    _resizeState: null,
    _rafId: null,
    _grid: 8,
    _minWidth: 80,
    _minHeight: 40,

    // Initialize pointer-based drag system
    init: function(canvasSelector, dotnetRef, grid) {
      this._grid = grid || 8;
      this._dotnetRef = dotnetRef;
      this._canvasSelector = canvasSelector;

      const canvas = document.querySelector(canvasSelector);
      if (!canvas) return;

      // Use pointer events for unified mouse/touch handling
      canvas.addEventListener('pointerdown', this._onPointerDown.bind(this), { passive: false });
      document.addEventListener('pointermove', this._onPointerMove.bind(this), { passive: false });
      document.addEventListener('pointerup', this._onPointerUp.bind(this), { passive: false });
      document.addEventListener('pointercancel', this._onPointerUp.bind(this), { passive: false });

      console.log('[freeDesigner] Initialized with grid:', this._grid);
    },

    destroy: function() {
      if (this._rafId) {
        cancelAnimationFrame(this._rafId);
        this._rafId = null;
      }
      this._dragState = null;
      this._resizeState = null;
    },

    _onPointerDown: function(e) {
      // Check if clicking on a widget or resize handle
      const widget = e.target.closest('.abs-widget');
      const handle = e.target.closest('.resize-handle');
      const canvas = document.querySelector(this._canvasSelector);

      if (!canvas) return;

      if (handle && widget) {
        // Start resize
        e.preventDefault();
        e.stopPropagation();

        const handleType = handle.dataset.handle;
        const rect = widget.getBoundingClientRect();
        const canvasRect = canvas.getBoundingClientRect();

        this._resizeState = {
          widgetId: widget.dataset.widgetId,
          widget: widget,
          handle: handleType,
          startX: e.clientX,
          startY: e.clientY,
          startLeft: rect.left - canvasRect.left,
          startTop: rect.top - canvasRect.top,
          startWidth: rect.width,
          startHeight: rect.height
        };

        widget.style.pointerEvents = 'none'; // Prevent interference during resize
        console.log('[freeDesigner] Start resize:', handleType, this._resizeState.widgetId);
      } else if (widget) {
        // Start drag
        e.preventDefault();
        e.stopPropagation();

        const rect = widget.getBoundingClientRect();
        const canvasRect = canvas.getBoundingClientRect();

        this._dragState = {
          widgetId: widget.dataset.widgetId,
          widget: widget,
          startX: e.clientX,
          startY: e.clientY,
          offsetX: e.clientX - rect.left,
          offsetY: e.clientY - rect.top,
          startLeft: rect.left - canvasRect.left,
          startTop: rect.top - canvasRect.top,
          canvasRect: canvasRect
        };

        // Use transform for smooth dragging (no layout thrashing)
        widget.style.transition = 'none';
        widget.classList.add('dragging');
        console.log('[freeDesigner] Start drag:', this._dragState.widgetId);
      }
    },

    _onPointerMove: function(e) {
      if (this._dragState) {
        e.preventDefault();
        this._handleDragMove(e);
      } else if (this._resizeState) {
        e.preventDefault();
        this._handleResizeMove(e);
      }
    },

    _handleDragMove: function(e) {
      const state = this._dragState;
      if (!state || !state.widget) return;

      // Cancel previous animation frame
      if (this._rafId) cancelAnimationFrame(this._rafId);

      // Use RAF for smooth updates
      this._rafId = requestAnimationFrame(() => {
        const dx = e.clientX - state.startX;
        const dy = e.clientY - state.startY;

        // Apply transform (no reflow)
        state.widget.style.transform = `translate(${dx}px, ${dy}px)`;

        // Show preview grid snap position
        const snappedX = this._snapToGrid(state.startLeft + dx);
        const snappedY = this._snapToGrid(state.startTop + dy);

        // Optional: show alignment guides here
        this._showSnapGuides(snappedX, snappedY);
      });
    },

    _handleResizeMove: function(e) {
      const state = this._resizeState;
      if (!state || !state.widget) return;

      if (this._rafId) cancelAnimationFrame(this._rafId);

      this._rafId = requestAnimationFrame(() => {
        const dx = e.clientX - state.startX;
        const dy = e.clientY - state.startY;

        let newX = state.startLeft;
        let newY = state.startTop;
        let newW = state.startWidth;
        let newH = state.startHeight;

        // Calculate new dimensions based on handle
        switch (state.handle) {
          case 'nw': // Top-left
            newX = state.startLeft + dx;
            newY = state.startTop + dy;
            newW = state.startWidth - dx;
            newH = state.startHeight - dy;
            break;
          case 'n': // Top
            newY = state.startTop + dy;
            newH = state.startHeight - dy;
            break;
          case 'ne': // Top-right
            newY = state.startTop + dy;
            newW = state.startWidth + dx;
            newH = state.startHeight - dy;
            break;
          case 'e': // Right
            newW = state.startWidth + dx;
            break;
          case 'se': // Bottom-right
            newW = state.startWidth + dx;
            newH = state.startHeight + dy;
            break;
          case 's': // Bottom
            newH = state.startHeight + dy;
            break;
          case 'sw': // Bottom-left
            newX = state.startLeft + dx;
            newW = state.startWidth - dx;
            newH = state.startHeight + dy;
            break;
          case 'w': // Left
            newX = state.startLeft + dx;
            newW = state.startWidth - dx;
            break;
        }

        // Enforce minimum size
        if (newW < this._minWidth) {
          if (state.handle.includes('w')) newX = state.startLeft + state.startWidth - this._minWidth;
          newW = this._minWidth;
        }
        if (newH < this._minHeight) {
          if (state.handle.includes('n')) newY = state.startTop + state.startHeight - this._minHeight;
          newH = this._minHeight;
        }

        // Snap to grid
        newX = this._snapToGrid(newX);
        newY = this._snapToGrid(newY);
        newW = this._snapToGrid(newW);
        newH = this._snapToGrid(newH);

        // Apply directly (for resize we don't use transform)
        state.widget.style.left = newX + 'px';
        state.widget.style.top = newY + 'px';
        state.widget.style.width = newW + 'px';
        state.widget.style.height = newH + 'px';
      });
    },

    _onPointerUp: function(e) {
      if (this._dragState) {
        this._finalizeDrag(e);
      } else if (this._resizeState) {
        this._finalizeResize(e);
      }
    },

    _finalizeDrag: function(e) {
      const state = this._dragState;
      if (!state || !state.widget) return;

      const dx = e.clientX - state.startX;
      const dy = e.clientY - state.startY;

      // Calculate final snapped position
      const finalX = this._snapToGrid(state.startLeft + dx);
      const finalY = this._snapToGrid(state.startTop + dy);

      // Remove transform and apply final position with transition
      state.widget.style.transition = 'left 0.2s ease, top 0.2s ease';
      state.widget.style.transform = '';
      state.widget.style.left = finalX + 'px';
      state.widget.style.top = finalY + 'px';
      state.widget.classList.remove('dragging');

      // Notify Blazor
      if (this._dotnetRef) {
        this._dotnetRef.invokeMethodAsync('OnWidgetMoved', state.widgetId, finalX, finalY);
      }

      this._hideSnapGuides();
      this._dragState = null;

      console.log('[freeDesigner] Drag completed:', state.widgetId, finalX, finalY);
    },

    _finalizeResize: function(e) {
      const state = this._resizeState;
      if (!state || !state.widget) return;

      // Get final dimensions from style
      const finalX = parseInt(state.widget.style.left) || state.startLeft;
      const finalY = parseInt(state.widget.style.top) || state.startTop;
      const finalW = parseInt(state.widget.style.width) || state.startWidth;
      const finalH = parseInt(state.widget.style.height) || state.startHeight;

      state.widget.style.pointerEvents = '';

      // Notify Blazor
      if (this._dotnetRef) {
        this._dotnetRef.invokeMethodAsync('OnWidgetResized', state.widgetId, finalX, finalY, finalW, finalH);
      }

      this._resizeState = null;

      console.log('[freeDesigner] Resize completed:', state.widgetId, finalX, finalY, finalW, finalH);
    },

    _snapToGrid: function(value) {
      return Math.round(value / this._grid) * this._grid;
    },

    _showSnapGuides: function(x, y) {
      // TODO: Implement alignment guides (optional enhancement)
    },

    _hideSnapGuides: function() {
      // TODO: Remove alignment guides
    }
  }
  /**
   * @deprecated 已弃用：请使用 dragManager.startResize() 替代
   * 该方法保留用于向后兼容，将在下一版本移除
   */
  , startWidgetResize: function (dotNetRef, widgetId, startX, initialWidth, widthUnit) {
    // 开始调整控件宽度
    console.warn('[Deprecated] startWidgetResize is deprecated, use dragManager.startResize() instead');
    const resizeState = {
      dotNetRef,
      widgetId,
      startX,
      initialWidth,
      widthUnit,
      containerWidth: null,
      lastWidth: initialWidth
    };

    // 动态查找控件所在的容器（可能是主画布或frame容器）
    const widgetEl = document.querySelector(`[data-widget-id="${widgetId}"]`);
    let container = null;
    if (widgetEl) {
      // 查找最近的容器：frame-drop-zone 或 layout-widgets-container
      container = widgetEl.closest('.frame-drop-zone, .layout-widgets-container');
    }

    // 如果找不到，回退到主画布
    if (!container) {
      container = document.querySelector('.layout-widgets-container');
    }

    // 获取容器宽度（用于计算百分比）
    if (container && widthUnit === '%') {
      resizeState.containerWidth = container.getBoundingClientRect().width;
    }

    const onMouseMove = (e) => {
      // 只响应横向移动，完全忽略纵向位置
      const deltaX = e.clientX - resizeState.startX;

      let newWidth;
      if (resizeState.widthUnit === '%' && resizeState.containerWidth) {
        // 百分比模式：计算像素变化对应的百分比
        const deltaPercent = (deltaX / resizeState.containerWidth) * 100;
        newWidth = Math.max(8, Math.min(100, Math.round(resizeState.initialWidth + deltaPercent)));
      } else {
        // 像素模式
        newWidth = Math.max(100, Math.round(resizeState.initialWidth + deltaX));
      }

      // 只有宽度真正变化时才更新
      if (newWidth === resizeState.lastWidth) {
        return;
      }

      resizeState.lastWidth = newWidth;

      // 立即调用C#更新状态（这样才能真正改变属性）
      if (resizeState.dotNetRef && resizeState.dotNetRef.invokeMethodAsync) {
        resizeState.dotNetRef.invokeMethodAsync('OnWidgetResized', widgetId, newWidth, resizeState.widthUnit);
      }
    };

    const onMouseUp = () => {
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';

      // 确保最终宽度被保存到C#
      if (resizeState.dotNetRef && resizeState.dotNetRef.invokeMethodAsync) {
        resizeState.dotNetRef.invokeMethodAsync('OnWidgetResized', widgetId, resizeState.lastWidth, resizeState.widthUnit);
      }
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
    document.body.style.cursor = 'ew-resize';
    document.body.style.userSelect = 'none';
  }
};

(function() {
  try {
    document.addEventListener('dragover', function(e){
      try {
        let target = e.target && e.target.closest ? e.target.closest('.frame-drop-zone') : null;
        let sel = null;
        if (target) {
          e.preventDefault();
          if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
          const containerId = target.getAttribute('data-container-id');
          sel = containerId ? `.frame-drop-zone[data-container-id="${containerId}"]` : '.frame-drop-zone';
        } else {
          target = e.target && e.target.closest ? e.target.closest('.abs-canvas, .layout-widgets-container') : null;
          if (target) {
            e.preventDefault();
            if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
            sel = target.classList.contains('abs-canvas') ? '.abs-canvas' : '.layout-widgets-container';
          }
        }
        if (sel) {
          try { window.bobcrm && window.bobcrm.updateDropMarker && window.bobcrm.updateDropMarker(sel, e.clientX, e.clientY); } catch(_) { }
        }
      } catch(_) { }
    }, { capture: true, passive: false });
    document.addEventListener('dragend', function(){
      try {
        document.body.classList.remove('is-dragging');
        window.bobcrm && window.bobcrm.clearDropMarker && window.bobcrm.clearDropMarker();
      } catch(_) { }
    }, true);
  } catch(e) { }
})();

window.bobcrmTheme = window.bobcrmTheme || {
  apply: function(themeName) {
    try {
      const root = document.documentElement;
      const allowed = ['theme-calm-light', 'theme-calm-dark'];
      const normalized = allowed.includes(themeName) ? themeName : 'theme-calm-light';
      allowed.forEach(cls => root.classList.remove(cls));
      root.classList.add(normalized);
    } catch (e) {
      console.error('[bobcrmTheme] apply error', e);
    }
  }
};

window.addEventListener('DOMContentLoaded', function () {
  try {
    window.bobcrmTheme.apply('theme-calm-light');
  } catch (e) {
    console.error('[bobcrmTheme] init error', e);
  }
});

window.logout = function() {
  // 清除本地存储
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  
  // 跳转到登录页
  window.location.href = '/login';
};
