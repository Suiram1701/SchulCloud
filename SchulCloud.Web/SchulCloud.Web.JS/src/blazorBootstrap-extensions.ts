export namespace blazorBootstrapExtensions {
    export namespace checkBox {
        export function setIndeterminate(elementId: string, state: boolean): void {
            let element = document.getElementById(elementId);
            if (element instanceof HTMLInputElement) {
                element.indeterminate = state;
            }
        }
    }
}