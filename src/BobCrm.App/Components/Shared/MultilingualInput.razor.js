/**
 * MultilingualInput JavaScript module
 * Handles dynamic width matching for the dropdown overlay
 */

export function setOverlayWidth(triggerElement) {
    if (!triggerElement) {
        console.warn('[MultilingualInput] Trigger element not found');
        return;
    }

    try {
        // Get the trigger width
        const triggerWidth = triggerElement.offsetWidth;

        // Find the dropdown overlay (it's rendered in a portal, so we need to search the document)
        // Ant Design adds the overlay className to the dropdown container
        const overlayContainers = document.querySelectorAll('.multilingual-dropdown-overlay');

        if (overlayContainers.length === 0) {
            // If not found immediately, wait a bit for the DOM to update
            setTimeout(() => setOverlayWidth(triggerElement), 50);
            return;
        }

        // Apply the width to all matching overlays (there should only be one visible)
        overlayContainers.forEach(container => {
            // Check if this overlay is visible
            const dropdown = container.closest('.ant-dropdown');
            if (dropdown && !dropdown.classList.contains('ant-dropdown-hidden')) {
                const overlayContent = container.querySelector('.multilingual-overlay');
                if (overlayContent) {
                    // Set the overlay width to match the trigger
                    // Use min-width to ensure it's at least as wide as the trigger
                    overlayContent.style.minWidth = `${triggerWidth}px`;
                    overlayContent.style.width = `${triggerWidth}px`;
                }
            }
        });
    } catch (error) {
        console.error('[MultilingualInput] Error setting overlay width:', error);
    }
}
