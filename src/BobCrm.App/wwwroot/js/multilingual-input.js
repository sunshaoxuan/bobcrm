let scrollHandler = null;
let dotNetRef = null;

export function setupScrollListener(dotNetReference) {
    removeScrollListener();
    dotNetRef = dotNetReference;
    scrollHandler = () => {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('CloseOnScroll');
        }
    };

    window.addEventListener('scroll', scrollHandler, { passive: true, capture: true });
    document.addEventListener('scroll', scrollHandler, { passive: true, capture: true });
}

export function removeScrollListener() {
    if (!scrollHandler) {
        return;
    }

    window.removeEventListener('scroll', scrollHandler, { capture: true });
    document.removeEventListener('scroll', scrollHandler, { capture: true });
    scrollHandler = null;
}

export function isFocusWithinOverlay(overlayId) {
    if (!overlayId) {
        return false;
    }

    const overlay = document.getElementById(overlayId);
    if (!overlay) {
        return false;
    }

    const active = document.activeElement;
    return overlay.contains(active);
}
