// Initialize color theme.
let theme = localStorage.getItem('.AspNetCore.Theme');
if (theme === null || theme == 0) {
    setTheme('auto');
}
else if (theme == 1) {
    setTheme('light');
}
else if (theme == 2) {
    setTheme('dark')
}
else {
    console.error('Unable to determine the saved color theme. Theme id: ' + theme)
}

function setTheme(theme) {
    if (theme === 'auto') {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            theme = 'dark';
        }
        else {
            theme = 'light';
        }
    }

    document.getElementsByTagName('html')[0].setAttribute('data-bs-theme', theme);
}

function autoColorThemeAvailable() {
    if (window.matchMedia) {
        return true;
    }
    else {
        return false;
    }
}