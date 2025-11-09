/*
* Declares objects set by CookieConsent.js from https://www.freeprivacypolicy.com/
*/

// Holds a global object that provides the real CookieConsent object.
declare const cookieconsent: CookieConsentAccessor;

declare interface CookieConsentAccessor {

    // Provides general interactions with the CookieConsent library.
    cookieConsentObject: CookieConsent;
}

declare interface CookieConsent {
    
    // Opens the GUI to configure the accepted cookie levels.
    openPreferencesCenter(): void;
    
    // Provides the cookie settings the user saved.
    userConsent: UserConsent;
}

declare interface UserConsent {
    
    // true when the user successfully saved the setting.
    userAccepted: boolean;
    
    // The level of cookies the user accepted.
    acceptedLevels: AcceptedLevels;
}

declare interface AcceptedLevels {
    
    // always true (required to accept) and used for things to get the application work properly (identity, etc.)
    "strictly-necessary": boolean;
    
    // used for things which improve user experience (optional) like color theme or culture
    "functionality": boolean;
    
    "targeting": boolean;
    "tracking": boolean;
}