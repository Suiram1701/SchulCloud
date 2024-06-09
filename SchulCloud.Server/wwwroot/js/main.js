// Called once when the window in initialized.
$(document).ready(function () {

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
});

// Called every time blazor renders the page.
function onAfterRender() {
    $('[data-bs-toggle="tooltip"]').tooltip();
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

    $('html').attr('data-bs-theme', theme);
};

function autoColorThemeAvailable() {
    if (window.matchMedia) {
        return true;
    }
    else {
        return false;
    }
}