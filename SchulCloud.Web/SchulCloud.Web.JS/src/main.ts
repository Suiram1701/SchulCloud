import { colorTheme } from './colorTheme';
import { file } from './file';
import { blazorBootstrapExtensions } from './blazorBootstrap-extensions';

colorTheme.retrieveFromLocalStorage();

export { colorTheme, file, blazorBootstrapExtensions }