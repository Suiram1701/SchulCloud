import { blazorBootstrapExtensions } from './blazorBootstrap-extensions';
import { colorTheme } from './colorTheme';
import { webAuthn } from './webAuthn';
import { file } from './file';

colorTheme.retrieveFromLocalStorage();

export { blazorBootstrapExtensions, colorTheme, webAuthn, file }