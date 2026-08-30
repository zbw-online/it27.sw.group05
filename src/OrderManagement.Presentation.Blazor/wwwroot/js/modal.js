export function show(dialogElement, dotNetRef) {
    if (!dialogElement || dialogElement.open) {
        return;
    }

    dialogElement.showModal();

    dialogElement.addEventListener("close", () => {
        dotNetRef.invokeMethodAsync("OnDialogClosed");
    }, { once: true });
}

export function closeDialog(dialogElement) {
    if (dialogElement && dialogElement.open) {
        dialogElement.close();
    }
}
