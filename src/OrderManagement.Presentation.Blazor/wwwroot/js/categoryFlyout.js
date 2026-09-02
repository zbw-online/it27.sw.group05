const SAFETY_MARGIN = 8;
const PANEL_COLUMN_WIDTH = 220;
const MIN_PANEL_HEIGHT = 120;

function getViewportSize() {
    if (window.visualViewport) {
        return { width: window.visualViewport.width, height: window.visualViewport.height };
    }

    return { width: window.innerWidth, height: window.innerHeight };
}

function computePlacement(triggerElement, panelsElement, levelCount) {
    const viewport = getViewportSize();
    const triggerRect = triggerElement.getBoundingClientRect();

    const naturalWidth = panelsElement.scrollWidth || panelsElement.getBoundingClientRect().width;
    const naturalHeight = panelsElement.scrollHeight || panelsElement.getBoundingClientRect().height;

    const spaceBelow = viewport.height - triggerRect.bottom - SAFETY_MARGIN;
    const spaceAbove = triggerRect.top - SAFETY_MARGIN;
    const spaceRight = viewport.width - triggerRect.left - SAFETY_MARGIN;

    const placement = (spaceBelow >= naturalHeight || spaceBelow >= spaceAbove) ? "below" : "above";
    const availableHeight = Math.max(MIN_PANEL_HEIGHT, placement === "below" ? spaceBelow : spaceAbove);

    const panelCount = levelCount + 1;
    const requiredCascadeWidth = PANEL_COLUMN_WIDTH * panelCount;
    const compact = spaceRight < requiredCascadeWidth;

    return {
        placement,
        compact,
        maxHeight: Math.floor(availableHeight),
        viewport
    };
}

function clampToViewport(panelsElement, viewport) {
    const rect = panelsElement.getBoundingClientRect();
    const style = panelsElement.style;

    let deltaX = 0;
    if (rect.left < SAFETY_MARGIN) {
        deltaX = SAFETY_MARGIN - rect.left;
    } else if (rect.right > viewport.width - SAFETY_MARGIN) {
        deltaX = (viewport.width - SAFETY_MARGIN) - rect.right;
    }

    let deltaY = 0;
    if (rect.top < SAFETY_MARGIN) {
        deltaY = SAFETY_MARGIN - rect.top;
    } else if (rect.bottom > viewport.height - SAFETY_MARGIN) {
        deltaY = (viewport.height - SAFETY_MARGIN) - rect.bottom;
    }

    if (deltaX !== 0) {
        const currentLeft = parseFloat(style.left) || rect.left;
        style.left = `${currentLeft + deltaX}px`;
        style.right = "auto";
    }

    if (deltaY !== 0) {
        const currentTop = parseFloat(style.top) || rect.top;
        style.top = `${currentTop + deltaY}px`;
        style.bottom = "auto";
    }
}

export function applyPlacement(triggerElement, panelsElement, levelCount) {
    if (!triggerElement || !panelsElement) {
        return false;
    }

    panelsElement.dataset.levelCount = String(levelCount);

    const result = computePlacement(triggerElement, panelsElement, levelCount);
    const triggerRect = triggerElement.getBoundingClientRect();
    const style = panelsElement.style;

    style.position = "fixed";
    style.maxHeight = `${result.maxHeight}px`;

    if (result.placement === "below") {
        style.top = `${Math.round(triggerRect.bottom + SAFETY_MARGIN)}px`;
        style.bottom = "auto";
    } else {
        style.bottom = `${Math.round(result.viewport.height - triggerRect.top + SAFETY_MARGIN)}px`;
        style.top = "auto";
    }

    style.left = `${Math.round(triggerRect.left)}px`;
    style.right = "auto";

    clampToViewport(panelsElement, result.viewport);

    return result.compact;
}

let repositionHandler = null;
let repositionTarget = null;

export function registerReposition(triggerElement, panelsElement, dotNetRef) {
    unregisterReposition();

    repositionTarget = panelsElement;

    repositionHandler = () => {
        const previousCompact = panelsElement.dataset.compact === "true";
        const levelCount = parseInt(panelsElement.dataset.levelCount || "0", 10);
        const compact = applyPlacement(triggerElement, panelsElement, levelCount);
        panelsElement.dataset.compact = String(compact);

        if (compact !== previousCompact) {
            dotNetRef.invokeMethodAsync("OnCompactChangedAsync", compact);
        }
    };

    window.addEventListener("resize", repositionHandler);
    window.addEventListener("scroll", repositionHandler, true);
    if (window.visualViewport) {
        window.visualViewport.addEventListener("resize", repositionHandler);
        window.visualViewport.addEventListener("scroll", repositionHandler);
    }
}

export function unregisterReposition() {
    if (repositionHandler) {
        window.removeEventListener("resize", repositionHandler);
        window.removeEventListener("scroll", repositionHandler, true);
        if (window.visualViewport) {
            window.visualViewport.removeEventListener("resize", repositionHandler);
            window.visualViewport.removeEventListener("scroll", repositionHandler);
        }
        repositionHandler = null;
        repositionTarget = null;
    }
}

export function focusItem(panelsElement, level, index) {
    if (!panelsElement) {
        return;
    }

    const selector = `[data-level="${level}"][data-index="${index}"]`;
    const element = panelsElement.querySelector(selector);
    if (element) {
        element.focus();
    }
}

let outsideClickHandler = null;

export function registerOutsideClick(containerElement, dotNetRef) {
    unregisterOutsideClick();

    outsideClickHandler = (event) => {
        if (containerElement && !containerElement.contains(event.target)) {
            dotNetRef.invokeMethodAsync("OnOutsideClickAsync");
        }
    };

    document.addEventListener("mousedown", outsideClickHandler, true);
}

export function unregisterOutsideClick() {
    if (outsideClickHandler) {
        document.removeEventListener("mousedown", outsideClickHandler, true);
        outsideClickHandler = null;
    }
}
