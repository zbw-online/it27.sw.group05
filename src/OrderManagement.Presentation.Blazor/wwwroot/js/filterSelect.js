export function setValue(element, value) {
    if (element && element.value !== value) {
        element.value = value ?? "";
    }
}
