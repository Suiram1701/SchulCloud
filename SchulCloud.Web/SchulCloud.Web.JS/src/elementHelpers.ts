export namespace elementHelpers {
    export function formSubmit(element: HTMLElement): void {
        if (!(element instanceof HTMLFormElement)) {
            console.error('A <form> element was expected but "' + typeof element + '" was specified.');
            return;
        }

        element.submit();
    }
}