// These user and role type definitions are used for the whole application.
global using ApplicationRole = SchulCloud.Database.Models.AppRole;
global using ApplicationUser = SchulCloud.Database.Models.AppUser;
global using AppRoleManager = SchulCloud.Identity.Managers.AppRoleManager<SchulCloud.Database.Models.AppRole>;
global using AppUserManager = SchulCloud.Identity.Managers.AppUserManager<SchulCloud.Database.Models.AppUser>;
