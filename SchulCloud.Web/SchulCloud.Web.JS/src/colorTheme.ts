export namespace colorTheme {
    export const enum theme {
        auto = 'auto',
        light = 'light',
        dark = 'dark'
    }

    export function retrieveFromLocalStorage(): void {
        const themeId: string | null = localStorage.getItem('.AspNetCore.Theme')
        switch (themeId) {
            case null:
            case '0':
                colorTheme.set(colorTheme.theme.auto);
                break;
            case '1':
                colorTheme.set(colorTheme.theme.light);
                break;
            case '2':
                colorTheme.set(colorTheme.theme.dark);
                break;
            default:
                console.error('Unable to determine the saved color theme. Theme id: ' + themeId)
        }
    }

    export function set(newTheme: theme): void {
        if (newTheme === theme.auto) {
            if (autoThemeAvailable() && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                newTheme = theme.dark;
            }
            else {
                newTheme = theme.light;
            }
        }

        document.getElementsByTagName('html')[0].setAttribute('data-bs-theme', newTheme);
    }

    export function autoThemeAvailable(): boolean {
        return Object.hasOwn(window, 'matchMedia');
    }
}