/**
 * MultilingualInput JavaScript module
 * Handles scroll listening to close popover when page scrolls
 */

let dotNetRef = null;
let scrollHandler = null;

export function setupScrollListener(dotNetReference) {
    console.log('[MultilingualInput] Setting up scroll listener...');

    // Clean up any existing listener first
    removeScrollListener();

    dotNetRef = dotNetReference;

    // Create immediate scroll handler (no debounce for instant close)
    scrollHandler = () => {
        console.log('[MultilingualInput] Scroll detected! Closing popover...');
        if (dotNetRef) {
            try {
                dotNetRef.invokeMethodAsync('CloseOnScroll');
            } catch (error) {
                console.error('[MultilingualInput] Error invoking CloseOnScroll:', error);
            }
        }
    };

    // Listen to scroll events on window and common scrollable containers
    // Use 'capture' phase to ensure we catch scrolling on any element
    window.addEventListener('scroll', scrollHandler, { passive: true, capture: true });
    document.addEventListener('scroll', scrollHandler, { passive: true, capture: true });

    // Also listen on specific containers
    const scrollableSelectors = ['.app-content', '.layout-body', '.ant-modal-body'];
    scrollableSelectors.forEach(selector => {
        const containers = document.querySelectorAll(selector);
        containers.forEach(container => {
            container.addEventListener('scroll', scrollHandler, { passive: true });
        });
    });

    console.log('[MultilingualInput] Scroll listener setup complete');
}

export function removeScrollListener() {
    if (!scrollHandler) {
        return;
    }

    console.log('[MultilingualInput] Removing scroll listener...');

    // Remove listeners from window and document
    window.removeEventListener('scroll', scrollHandler, { capture: true });
    document.removeEventListener('scroll', scrollHandler, { capture: true });

    // Remove from specific containers
    const scrollableSelectors = ['.app-content', '.layout-body', '.ant-modal-body'];
    scrollableSelectors.forEach(selector => {
        const containers = document.querySelectorAll(selector);
        containers.forEach(container => {
            container.removeEventListener('scroll', scrollHandler);
        });
    });

    dotNetRef = null;
    scrollHandler = null;
    console.log('[MultilingualInput] Scroll listener removed');
}
