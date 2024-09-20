// These user and role type definitions are used for the whole application.
global using AppCredential = SchulCloud.Database.Models.Fido2Credential;
global using AppLogInAttempt = SchulCloud.Database.Models.LogInAttempt;
global using ApplicationRole = SchulCloud.Database.Models.SchulCloudRole;
global using ApplicationUser = SchulCloud.Database.Models.SchulCloudUser;

global using AppUserManager = SchulCloud.Store.Managers.SchulCloudUserManager<
    SchulCloud.Database.Models.SchulCloudUser,
    SchulCloud.Database.Models.Fido2Credential,
    SchulCloud.Database.Models.LogInAttempt>;