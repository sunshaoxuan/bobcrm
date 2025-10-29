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
  }
};
