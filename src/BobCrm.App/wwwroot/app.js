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
  , setTheme: function (name) {
    try {
      name = (name || 'light');
      const root = document.documentElement;
      root.classList.remove('theme-light', 'theme-dark');
      root.classList.add(name === 'dark' ? 'theme-dark' : 'theme-light');
      localStorage.setItem('theme', name);
    } catch (e) { }
  }
  , getTheme: function () {
    try { return localStorage.getItem('theme') || 'light'; } catch (e) { return 'light'; }
  }
  , setPrimary: function (color) {
    try { document.documentElement.style.setProperty('--primary', color || '#3f7cff'); localStorage.setItem('primary', color || '#3f7cff'); } catch (e) { }
  }
  , getPrimary: function () {
    try { return localStorage.getItem('primary') || '#3f7cff'; } catch (e) { return '#3f7cff'; }
  }
};

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
  // 刷新页面以加载新的i18n资源（最简单可靠的方式）
  window.location.reload();
};
