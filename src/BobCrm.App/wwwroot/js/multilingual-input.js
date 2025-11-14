const handlerMap = new Map();

export function multilingualInput_getPosition(element, expectedHeight) {
    if (!element) {
        return { top: 0, left: 0, minWidth: 320 };
    }

    const rect = element.getBoundingClientRect();
    const scrollX = window.scrollX || window.pageXOffset;
    const scrollY = window.scrollY || window.pageYOffset;
    const viewportWidth = document.documentElement.clientWidth;
    const viewportHeight = document.documentElement.clientHeight;
    const margin = 12;
    const desiredWidth = Math.max(rect.width, 320);
    const desiredHeight = expectedHeight || 280;

    let left = rect.left + scrollX;
    if (left + desiredWidth > viewportWidth + scrollX - margin) {
        left = Math.max(margin + scrollX, viewportWidth + scrollX - desiredWidth - margin);
    }

    const spaceBelow = viewportHeight - rect.bottom - margin;
    const spaceAbove = rect.top - margin;

    let top;
    if (spaceBelow < desiredHeight && spaceAbove > spaceBelow) {
        top = Math.max(margin + scrollY, rect.top + scrollY - desiredHeight - margin);
    } else {
        top = rect.bottom + scrollY + margin;
    }

    return {
        top,
        left,
        minWidth: desiredWidth
    };
}

export function multilingualInput_registerHandlers(dotNetRef) {
    const handler = () => dotNetRef.invokeMethodAsync("CloseFromJs");
    window.addEventListener("scroll", handler, true);
    window.addEventListener("resize", handler);

    const id = Date.now() + Math.random();
    handlerMap.set(id, handler);
    return id;
}

export function multilingualInput_removeHandlers(id) {
    const handler = handlerMap.get(id);
    if (!handler) {
        return;
    }
    window.removeEventListener("scroll", handler, true);
    window.removeEventListener("resize", handler);
    handlerMap.delete(id);
}
