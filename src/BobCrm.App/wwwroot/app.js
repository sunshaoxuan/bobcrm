window.bobcrm = {
  // Ensure primary color actually applied to computed styles
  _ensurePrimaryApplied: function(expected) {
    try {
      const want = (expected || this.getPrimary() || '').trim();
      if (!want) return;
      const computed = getComputedStyle(document.documentElement).getPropertyValue('--primary').trim();
      if (computed.toLowerCase() !== want.toLowerCase()) {
        const styleTag = document.getElementById('dynamic-theme');
        if (styleTag) styleTag.textContent = `:root { --primary: ${want} !important; }`;
        document.documentElement.style.setProperty('--primary', want);
        console.log('[guardian] reapplied --primary, want:', want, 'computed:', computed);
      }
    } catch (e) { }
  },
  getInitColor: function() {
    try {
      if (window.APP_DEFAULTS && window.APP_DEFAULTS.initColor) return window.APP_DEFAULTS.initColor;
      // Fallback to CSS variable
      const css = getComputedStyle(document.documentElement).getPropertyValue('--primary').trim();
      return css;
    } catch (e) { return ''; }
  },
  preferencesCallback: null,
  skipNextSave: false,
  registerPreferencesCallback: function(dotnetRef) {
    this.preferencesCallback = dotnetRef;
  },
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
  , fetchJson: async function (url) {
    try {
      const resp = await fetch(url, { method: 'GET', mode: 'cors', credentials: 'omit' });
      if (!resp.ok) return null;
      return await resp.json();
    } catch (e) { return null; }
  }
  , setTheme: function (name, skipSave) {
    try {
      name = (name || 'light');
      const root = document.documentElement;
      root.classList.remove('theme-light', 'theme-dark');
      root.classList.add(name === 'dark' ? 'theme-dark' : 'theme-light');
      localStorage.setItem('theme', name);

      // Save to server (unless explicitly skipped)
      if (!skipSave && this.preferencesCallback) {
        this.preferencesCallback.invokeMethodAsync('SaveThemeAsync', name);
      }
    } catch (e) { }
  }
  , getTheme: function () {
    try { return localStorage.getItem('theme') || 'light'; } catch (e) { return 'light'; }
  }
  , setPrimary: function (color) {
    try {
      const normalizedColor = color || this.getInitColor();

      console.log('[setPrimary] START - color:', normalizedColor);

      // 1. Update CSS via style tag (won't be cleared by Blazor)
      const styleTag = document.getElementById('dynamic-theme');
      console.log('[setPrimary] styleTag found:', !!styleTag);
      if (styleTag) {
        styleTag.textContent = `:root { --primary: ${normalizedColor} !important; }`;
        console.log('[setPrimary] CSS updated');
      } else {
        console.error('[setPrimary] styleTag NOT FOUND!');
      }

      // 1.1 Also set inline custom property on :root to ensure precedence over external stylesheets
      try {
        document.documentElement.style.setProperty('--primary', normalizedColor);
        console.log('[setPrimary] Inline --primary set on :root');
      } catch (e) { /* ignore */ }

      // 2. Save to localStorage
      localStorage.setItem('udfColor', normalizedColor);
      console.log('[setPrimary] localStorage saved:', localStorage.getItem('udfColor'));

      // 3. Update button active states
      document.querySelectorAll('.color-btn').forEach(btn => {
        const btnColor = btn.getAttribute('data-color');
        if (btnColor && btnColor.toLowerCase() === normalizedColor.toLowerCase()) {
          btn.style.borderColor = '#333';
          btn.style.boxShadow = '0 0 0 2px rgba(0,0,0,0.1)';
        } else {
          btn.style.borderColor = 'transparent';
          btn.style.boxShadow = 'none';
        }
      });
      console.log('[setPrimary] Button states updated');

      // 4. Save to backend
      console.log('[setPrimary] preferencesCallback exists:', !!this.preferencesCallback);
      if (this.preferencesCallback) {
        console.log('[setPrimary] Calling backend...');
        this.preferencesCallback.invokeMethodAsync('SavePrimaryColorAsync', normalizedColor);
      } else {
        console.warn('[setPrimary] preferencesCallback NOT REGISTERED!');
      }

      console.log('[setPrimary] DONE');
    } catch (e) {
      console.error('[setPrimary] error:', e);
    }
  }
  , getPrimary: function () {
    try {
      // Read from cache; fallback to system initColor
      return localStorage.getItem('udfColor') || this.getInitColor();
    } catch (e) {
      return this.getInitColor();
    }
  }
  , setDragData: function (type, data) {
    // This function is kept for compatibility, but we use data-* attributes instead
    // The actual dataTransfer setup is done in the dragstart event handler
  }
  , initDragDrop: function () {
    // Set up global dragstart handler to read data-* attributes and set dataTransfer
    if (!this._dragDropInitialized) {
      document.addEventListener('dragstart', function(e) {
        try {
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

      const widgets = Array.from(container.querySelectorAll('.layout-widget'));
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
      const widgets = Array.from(container.querySelectorAll('.layout-widget'));
      if (!this._marker) {
        const m = document.createElement('div');
        m.className = 'drop-marker';
        m.style.position = 'absolute';
        m.style.width = '0px';
        m.style.borderLeft = '3px solid var(--primary)';
        m.style.pointerEvents = 'none';
        m.style.zIndex = '10';
        this._marker = m;
      }
      if (getComputedStyle(container).position === 'static') {
        container.style.position = 'relative';
      }

      // Compute row and target index similar to getInsertIndex
      if (widgets.length === 0) {
        // Place at top-left with minimal height
        if (!this._marker.parentElement) container.appendChild(this._marker);
        this._marker.style.left = '0px';
        this._marker.style.top = '0px';
        this._marker.style.height = '24px';
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

      if (!this._marker.parentElement) container.appendChild(this._marker);
      this._marker.style.left = leftPx;
      this._marker.style.top = topPx;
      this._marker.style.height = heightPx;
    } catch (e) { }
  }
  , clearDropMarker: function () {
    try {
      if (this._marker && this._marker.parentElement) this._marker.parentElement.removeChild(this._marker);
      this._marker = null;
    } catch (e) { }
  }
  , initTheme: function () {
    try {
      console.log('[initTheme] called');

      // 1. Apply theme (light/dark)
      const savedTheme = this.getTheme();
      this.setTheme(savedTheme, true);

      // 2. Apply primary color from localStorage
      const udfColor = this.getPrimary();
      console.log('[initTheme] udfColor from localStorage:', udfColor);

      const styleTag = document.getElementById('dynamic-theme');
      if (styleTag) {
        styleTag.textContent = `:root { --primary: ${udfColor} !important; }`;
      }
      // Debug: readback computed value
      try {
        const computed = getComputedStyle(document.documentElement).getPropertyValue('--primary').trim();
        console.log('[initTheme] computed --primary:', computed);
      } catch (e) { }
      // Ensure runtime color wins over defaults in external CSS
      try { document.documentElement.style.setProperty('--primary', udfColor); } catch (e) { }
      // Verify application
      this._ensurePrimaryApplied(udfColor);

      // 3. Update UI elements
      const themeSelector = document.getElementById('theme-selector');
      if (themeSelector) {
        themeSelector.value = savedTheme;
      }

      // Update color button active states
      document.querySelectorAll('.color-btn').forEach(btn => {
        const btnColor = btn.getAttribute('data-color');
        if (btnColor && btnColor.toLowerCase() === udfColor.toLowerCase()) {
          btn.style.borderColor = '#333';
          btn.style.boxShadow = '0 0 0 2px rgba(0,0,0,0.1)';
        } else {
          btn.style.borderColor = 'transparent';
          btn.style.boxShadow = 'none';
        }
      });
    } catch (e) {
      console.error('[initTheme] error:', e);
    }
  }
  , debugDnD: function() {
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
};

// Monitor localStorage changes for 'udfColor' key
window.addEventListener('storage', (e) => {
  if (e.key === 'udfColor') {
    console.log('=== [theme] localStorage.udfColor changed ===');
    console.log('  oldValue:', e.oldValue);
    console.log('  newValue:', e.newValue);
    console.log('  url:', e.url);
  }
});

// Ensure theme reapplies on client-side navigation (history push/replace)
(function() {
  try {
    const callInit = () => { try { window.bobcrm && window.bobcrm.initTheme && window.bobcrm.initTheme(); } catch(e) { } };
    const _push = history.pushState;
    history.pushState = function() { const r = _push.apply(this, arguments); try { window.dispatchEvent(new Event('bobcrm:navigated')); } catch (_) {} return r; };
    const _replace = history.replaceState;
    history.replaceState = function() { const r = _replace.apply(this, arguments); try { window.dispatchEvent(new Event('bobcrm:navigated')); } catch (_) {} return r; };
    window.addEventListener('bobcrm:navigated', callInit);
    // Guard against head/style mutations that could revert variables
    const mo = new MutationObserver(() => {
      try { window.bobcrm && window.bobcrm._ensurePrimaryApplied && window.bobcrm._ensurePrimaryApplied(); } catch(e) {}
    });
    mo.observe(document.documentElement, { attributes: true, childList: true, subtree: true });
    // Also periodically verify for the first few seconds after load
    let ticks = 0; const t = setInterval(() => { try { window.bobcrm && window.bobcrm._ensurePrimaryApplied && window.bobcrm._ensurePrimaryApplied(); } catch(e) {} if (++ticks > 50) clearInterval(t); }, 100);
    // Ensure browsers always show allowed drop over canvas
    document.addEventListener('dragover', function(e){
      try {
        const target = e.target && e.target.closest ? e.target.closest('.abs-canvas, .layout-widgets-container') : null;
        if (target) {
          e.preventDefault();
          if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
          const sel = target.classList.contains('abs-canvas') ? '.abs-canvas' : '.layout-widgets-container';
          try { window.bobcrm && window.bobcrm.updateDropMarker && window.bobcrm.updateDropMarker(sel, e.clientX, e.clientY); } catch(_){ }
        }
      } catch(_) {}
    }, { capture: true, passive: false });
    document.addEventListener('dragend', function(){
      try { window.bobcrm && window.bobcrm.clearDropMarker && window.bobcrm.clearDropMarker(); } catch(_) {}
    }, true);
  } catch(e) { }
})();

window.logout = function() {
  // 清除本地存储
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  
  // 跳转到登录页
  window.location.href = '/login';
};

window.changeLang = function(lang) {
  // 保存语言选择
  bobcrm.setCookie('lang', lang, 365);

  // Save to server
  if (bobcrm.preferencesCallback) {
    bobcrm.preferencesCallback.invokeMethodAsync('SaveLanguageAsync', lang);
  }

  // 刷新页面以加载新的i18n资源（最简单可靠的方式）
  window.location.reload();
};
