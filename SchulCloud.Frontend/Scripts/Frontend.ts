export * from './ElementHelpers';
export * from './Clipboard';
export * from './WebAuthn';
export * from './File';

document.addEventListener('DOMContentLoaded', () => {
    // Required to provide a smooth page loading without a flash bang
    let consent: CookieConsent = cookieconsent.cookieConsentObject;
    if (consent.userConsent.acceptedLevels.functionality) {
        const darkModePreferred: boolean = window.matchMedia("(prefers-color-scheme: dark)").matches;
        document.cookie = darkModePreferred ? ".AspNetCore.AutoDarkTheme=true; Path=/; Max-Age=31557600;" : "";     // Used as flag (one year)
    }
});