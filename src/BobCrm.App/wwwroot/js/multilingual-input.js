/**
 * MultilingualInput JavaScript module
 * Handles scroll listening to close popover when page scrolls
 * and positions the overlay correctly (upward/downward)
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

/**
 * Position the overlay container relative to the trigger element
 * Automatically determines whether to show above or below based on available space
 * @param {string} triggerId - The ID or class of the trigger element
 */
export function positionOverlay(triggerId) {
    console.log('[MultilingualInput] Positioning overlay for trigger:', triggerId);

    // Find trigger and overlay elements
    const trigger = document.querySelector(`.multilingual-trigger`);
    const overlay = document.querySelector('.multilingual-overlay-container');

    if (!trigger || !overlay) {
        console.warn('[MultilingualInput] Could not find trigger or overlay elements');
        return;
    }

    // Get dimensions and positions
    const triggerRect = trigger.getBoundingClientRect();
    const overlayHeight = overlay.offsetHeight || 400; // Default max height
    const viewportHeight = window.innerHeight;

    // Calculate available space above and below
    const spaceBelow = viewportHeight - triggerRect.bottom;
    const spaceAbove = triggerRect.top;

    console.log('[MultilingualInput] Space below:', spaceBelow, 'Space above:', spaceAbove, 'Overlay height:', overlayHeight);

    // Determine if we should show above or below
    const shouldShowAbove = spaceBelow < overlayHeight && spaceAbove > spaceBelow;

    // Apply positioning
    overlay.style.position = 'fixed';
    overlay.style.left = `${triggerRect.left}px`;
    overlay.style.width = `${Math.max(triggerRect.width, 400)}px`;

    if (shouldShowAbove) {
        // Show above trigger
        overlay.style.bottom = `${viewportHeight - triggerRect.top + 8}px`;
        overlay.style.top = 'auto';
        overlay.classList.add('overlay-above');
        overlay.classList.remove('overlay-below');
        console.log('[MultilingualInput] Positioned overlay ABOVE trigger');
    } else {
        // Show below trigger (default)
        overlay.style.top = `${triggerRect.bottom + 8}px`;
        overlay.style.bottom = 'auto';
        overlay.classList.add('overlay-below');
        overlay.classList.remove('overlay-above');
        console.log('[MultilingualInput] Positioned overlay BELOW trigger');
    }

    // Ensure overlay doesn't exceed viewport
    const maxHeight = shouldShowAbove
        ? Math.min(400, spaceAbove - 16)  // 16px for margin
        : Math.min(400, spaceBelow - 16);
    overlay.style.maxHeight = `${maxHeight}px`;
}
