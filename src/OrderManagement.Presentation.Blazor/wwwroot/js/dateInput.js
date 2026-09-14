export function openPicker(element) {
    if (element && typeof element.showPicker === "function") {
        element.showPicker();
    }
}
