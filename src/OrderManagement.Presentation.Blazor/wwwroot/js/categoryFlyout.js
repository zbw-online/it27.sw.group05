const compactMediaQuery = "(max-width: 900px)";

export function isCompactViewport() {
    return window.matchMedia(compactMediaQuery).matches;
}

export function shouldFlipLeft(containerElement, panelsElement) {
    if (!containerElement || !panelsElement) {
        return false;
    }

    const containerRect = containerElement.getBoundingClientRect();
    const width = panelsElement.scrollWidth || panelsElement.getBoundingClientRect().width;
    return containerRect.left + width > window.innerWidth;
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
