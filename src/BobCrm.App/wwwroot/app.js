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
    // Calculate insertion index based on coordinates within flex container
    try {
      const container = document.querySelector(containerSelector);
      if (!container) return -1;
      
      const widgets = container.querySelectorAll('.layout-widget');
      const containerRect = container.getBoundingClientRect();
      const relativeX = x - containerRect.left;
      const relativeY = y - containerRect.top;
      
      // For flex wrap layout, we need to find which row and position
      let insertIndex = widgets.length; // Default: append at end
      
      for (let i = 0; i < widgets.length; i++) {
        const widget = widgets[i];
        const rect = widget.getBoundingClientRect();
        const widgetRelativeX = rect.left - containerRect.left;
        const widgetRelativeY = rect.top - containerRect.top;
        const widgetWidth = rect.width;
        const widgetHeight = rect.height;
        
        // Check if point is within this widget
        if (relativeX >= widgetRelativeX && relativeX <= widgetRelativeX + widgetWidth &&
            relativeY >= widgetRelativeY && relativeY <= widgetRelativeY + widgetHeight) {
          // Determine if we should insert before or after based on which half of the widget
          if (relativeX < widgetRelativeX + widgetWidth / 2) {
            insertIndex = i;
          } else {
            insertIndex = i + 1;
          }
          break;
        }
        // If point is before this widget (vertically), insert before it
        else if (relativeY < widgetRelativeY) {
          insertIndex = i;
          break;
        }
      }
      
      return insertIndex;
    } catch (e) {
      return -1;
    }
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
