﻿using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Authorization;
using SchulCloud.Database.Enums;
using SchulCloud.Database.Models;
using SchulCloud.Identity.Abstractions;
using SchulCloud.Identity.Enums;
using SchulCloud.Identity.Models;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using LoginAttemptResult = SchulCloud.Database.Enums.LoginAttemptResult;

namespace SchulCloud.Database.Stores;

public class SchulCloudUserStore<TUser, TRole, TContext>(TContext context, IdentityErrorDescriber? describer = null)
    : UserStore<TUser, TRole, TContext>(context, describer),
    IUserCredentialStore<TUser>,
    IUserTwoFactorEmailStore<TUser>,
    IUserTwoFactorSecurityKeyStore<TUser>,
    IUserPasskeysStore<TUser>,
    IUserLoginAttemptStore<TUser>,
    IUserPermissionStore<TUser>,
    IUserLanguageStore<TUser>,
    IUserColorThemeStore<TUser>,
    IUserApiKeyStore<TUser>
    where TUser : AppUser
    where TRole : IdentityRole
    where TContext : DbContext
{
    private DbSet<Credential> Credentials => Context.Set<Credential>();

    private DbSet<LoginAttempt> LoginAttempts => Context.Set<LoginAttempt>();

    private DbSet<ApiKey> ApiKeys => Context.Set<ApiKey>();

    private static readonly TypeAdapterConfig _loginAttemptAdaptConfig;
    private static readonly TypeAdapterConfig _apiKeyAdaptConfig;

    static SchulCloudUserStore()
    {
        _loginAttemptAdaptConfig = new();
        _loginAttemptAdaptConfig.ForDestinationType<LoginAttempt>().Ignore(attempt => attempt.Id);
        _loginAttemptAdaptConfig.ForType<IPAddress, byte[]>().MapWith(ip => ip.GetAddressBytes());
        _loginAttemptAdaptConfig.ForType<byte[], IPAddress>().MapWith(ip => new IPAddress(ip));

        _apiKeyAdaptConfig = new();
        _apiKeyAdaptConfig.ForDestinationType<ApiKey>().Ignore(key => key.Id);
    }

    public override Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.TwoFactorEnabledFlags.HasFlag(TwoFactorMethod.Authenticator));
    }

    public override Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (enabled)
        {
            user.TwoFactorEnabledFlags |= TwoFactorMethod.Authenticator;
        }
        else
        {
            user.TwoFactorEnabledFlags &= ~TwoFactorMethod.Authenticator;
        }
        return Task.CompletedTask;
    }

    #region IUserTwoFactorEmailStore
    public Task<bool> GetTwoFactorEmailEnabled(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.TwoFactorEnabledFlags.HasFlag(TwoFactorMethod.Email));
    }

    public Task SetTwoFactorEmailEnabled(TUser user, bool enabled, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (enabled)
        {
            user.TwoFactorEnabledFlags |= TwoFactorMethod.Email;
        }
        else
        {
            user.TwoFactorEnabledFlags &= ~TwoFactorMethod.Email;
        }
        return Task.CompletedTask;
    }
    #endregion

    #region IUserTwoFactorSecurityKeyStore
    public Task<bool> GetTwoFactorSecurityKeyEnabledAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.TwoFactorEnabledFlags.HasFlag(TwoFactorMethod.SecurityKey));
    }

    public Task SetTwoFactorSecurityKeyEnabledAsync(TUser user, bool enabled, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (enabled)
        {
            user.TwoFactorEnabledFlags |= TwoFactorMethod.SecurityKey;
        }
        else
        {
            user.TwoFactorEnabledFlags &= ~TwoFactorMethod.SecurityKey;
        }
        return Task.CompletedTask;
    }

    public async Task<int> GetTwoFactorSecurityKeyCountAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        string userId = await GetUserIdAsync(user, ct).ConfigureAwait(false);
        return await Credentials.CountAsync(cred => cred.UserId.Equals(userId), ct).ConfigureAwait(false);
    }
    #endregion

    #region IUserCredentialStore
    public async Task<UserCredential?> FindCredentialAsync(byte[] id, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(id);
        ct.ThrowIfCancellationRequested();

        Credential? credential = await Credentials.FindAsync([id], ct).AsTask();
        return credential?.Adapt<UserCredential>();
    }

    public async Task<TUser?> FindUserByCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        Credential? dbCredential = await Credentials.FindAsync([credential.Id], ct).AsTask();
        if (dbCredential is not null)
        {
            return await FindUserAsync(dbCredential.UserId, ct);
        }

        return null;
    }

    public async Task<IEnumerable<UserCredential>> FindCredentialsByUserAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        return await Credentials
            .Where(cred => cred.UserId.Equals(userId))
            .ProjectToType<UserCredential>()
            .ToListAsync(ct);
    }

    public async Task AddCredentialAsync(TUser user, UserCredential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        Credential dbDto = credential.Adapt<Credential>();
        dbDto.UserId = await GetUserIdAsync(user, ct);
        await Credentials.AddAsync(dbDto, ct).AsTask();
    }

    public async Task UpdateCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        Credential? dbDto = await Credentials.FindAsync([credential.Id], ct).AsTask();
        if (dbDto is not null)
        {
            credential.BuildAdapter()
                .EntityFromContext(Context)
                .AdaptTo(dbDto);
        }
        else
        {
            throw new InvalidOperationException($"Unable to find a credential dto with id '{credential.Id}'");
        }
    }

    public async Task RemoveCredentialAsync(UserCredential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        await Credentials.Where(cred => cred.Id.Equals(credential.Id)).ExecuteDeleteAsync(ct);
    }

    public async Task<bool> IsCredentialOwnedByUser(TUser user, UserCredential credential, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        return await Credentials.AnyAsync(cred => cred.UserId.Equals(userId) || cred.Id.SequenceEqual(credential.Id), ct);
    }
    #endregion

    #region IUserPasskeysEnabledStore
    public Task<bool> GetPasskeysEnabledAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.PasskeysEnabled);
    }

    public Task SetPasskeysEnabledAsync(TUser user, bool enabled, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        user.PasskeysEnabled = enabled;
        return Task.CompletedTask;
    }

    public async Task<bool> GetIsPasskeyCredentialAsync(UserCredential credential, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        Credential dbCredential = await Credentials.FindAsync([credential.Id], ct).AsTask()
            ?? throw new InvalidOperationException($"Could not find entity with the id {string.Join('-', credential.Id)}.");
        return dbCredential.IsPasskey;
    }

    public async Task SetIsPasskeyCredentialAsync(UserCredential credential, bool enabled, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credential);
        ct.ThrowIfCancellationRequested();

        Credential dbCredential = await Credentials.FindAsync([credential.Id], ct).AsTask()
            ?? throw new InvalidOperationException($"Could not find entity with the id {string.Join('-', credential.Id)}.");
        dbCredential.IsPasskey = enabled;
    }

    public async Task<int> GetPasskeyCountAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        string userId = await GetUserIdAsync(user, ct).ConfigureAwait(false);
        return await Credentials.CountAsync(cred => cred.UserId.Equals(userId) || cred.IsPasskey, ct).ConfigureAwait(false);
    }
    #endregion

    #region IUserLoginAttemptStore
    public async Task<UserLoginAttempt?> FindLoginAttemptAsync(string id, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(id);
        ct.ThrowIfCancellationRequested();

        LoginAttempt? attempt = await LoginAttempts.FindAsync([id], ct).AsTask();
        return attempt?.Adapt<UserLoginAttempt>(_loginAttemptAdaptConfig);
    }

    public async Task<TUser?> FindUserByLoginAttemptAsync(UserLoginAttempt attempt, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(attempt);
        ct.ThrowIfCancellationRequested();

        LoginAttempt? dbAttempt = await LoginAttempts.FindAsync([attempt.Id], ct);
        if (dbAttempt is not null)
        {
            return await FindUserAsync(dbAttempt.UserId, ct);
        }

        return null;
    }

    public async Task<IEnumerable<UserLoginAttempt>> FindLoginAttemptsByUserAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        return await LoginAttempts
            .Where(attempt => attempt.UserId.Equals(userId))
            .ProjectToType<UserLoginAttempt>(_loginAttemptAdaptConfig)
            .ToListAsync(ct);
    }

    public async Task AddLoginAttemptAsync(TUser user, UserLoginAttempt attempt, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(attempt);
        ct.ThrowIfCancellationRequested();

        LoginAttempt dbDto = attempt.Adapt<LoginAttempt>(_loginAttemptAdaptConfig);
        dbDto.UserId = await GetUserIdAsync(user, ct);
        await LoginAttempts.AddAsync(dbDto, ct).AsTask();
    }

    public async Task RemoveLoginAttemptAsync(UserLoginAttempt attempt, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(attempt);
        ct.ThrowIfCancellationRequested();

        await Credentials.Where(dto => dto.Id.Equals(dto.Id)).ExecuteDeleteAsync(ct);
    }

    public async Task RemoveAllLoginAttemptsAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        await LoginAttempts.Where(attempt => attempt.UserId.Equals(userId)).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Identity.Enums.LoginAttemptMethod, DateTime>> GetLatestLoginMethodUseTimeAsync(TUser user, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        IEnumerable<dynamic> attempts = await LoginAttempts
            .AsNoTracking()
            .Select(attempt => new { attempt.UserId, attempt.Method, attempt.Result, attempt.DateTime })
            .Where(attempt => attempt.UserId.Equals(userId))
            .Where(attempt => attempt.Result == LoginAttemptResult.Succeeded || attempt.Result == LoginAttemptResult.TwoFactorRequired)
            .GroupBy(attempt => attempt.Method).Select(group => group.OrderByDescending(attempt => attempt.DateTime).First())     // DistinctBy isn't currently supported by ef core. The expression in this line does the same.
            .ToListAsync(ct);

        return attempts.ToDictionary<dynamic, Identity.Enums.LoginAttemptMethod, DateTime>(
            keySelector: attempt => (Identity.Enums.LoginAttemptMethod)attempt.Method,
            elementSelector: attempt => attempt.DateTime);
    }
    #endregion

    #region IUserPermissionStore
    public async Task SetPermissionLevelAsync(TUser user, Permission permission, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(permission);
        ct.ThrowIfCancellationRequested();

        Claim? existingClaim = await GetPermissionTypeAsync(user, permission.Name, ct);     // Check whether the same permission type is already stored.
        if (permission.Level != PermissionLevel.None)
        {
            Claim claim = new(Authorization.ClaimTypes.Permission, permission.ToString());
            if (existingClaim is null)
            {
                await AddClaimsAsync(user, [claim], ct);
            }
            else
            {
                await ReplaceClaimAsync(user, existingClaim, claim, ct);
            }
        }
        else if (existingClaim is not null)
        {
            await RemoveClaimsAsync(user, [existingClaim], ct);
        }
    }

    public async Task<PermissionLevel> GetPermissionLevelAsync(TUser user, string permissionName, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);
        ct.ThrowIfCancellationRequested();

        Claim? claim = await GetPermissionTypeAsync(user, permissionName, ct);
        return claim is not null
            ? Permission.Parse(claim.Value).Level
            : PermissionLevel.None;
    }

    public async Task<IReadOnlyCollection<Permission>> GetPermissionLevelsAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        IEnumerable<Claim> userClaims = await GetClaimsAsync(user, ct);
        return userClaims
            .Where(claim => claim.Type == Authorization.ClaimTypes.Permission)
            .Select(claim => Permission.Parse(claim.Value))
            .ToArray();
    }

    private async Task<Claim?> GetPermissionTypeAsync(TUser user, string permissionName, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        IEnumerable<Claim> userClaims = await GetClaimsAsync(user, ct);
        return userClaims
            .Where(claim => claim.Type == Authorization.ClaimTypes.Permission)
            .FirstOrDefault(claim => Permission.Parse(claim.Value).Name == permissionName);
    }
    #endregion

    private const string _settingClaimPrefix = "Setting";
    
    #region IUserLanguageStore

    public async Task SetCultureAsync(TUser user, CultureInfo? culture, CancellationToken ct) => await SetCultureBaseAsync(user, culture, "Culture", ct);

    public async Task SetUiCultureAsync(TUser user, CultureInfo? culture, CancellationToken ct) => await SetCultureBaseAsync(user, culture, "UiCulture", ct);

    public async Task<CultureInfo?> GetCultureAsync(TUser user, CancellationToken ct) => await GetCultureBaseAsync(user, "Culture", ct);

    public async Task<CultureInfo?> GetUiCultureAsync(TUser user, CancellationToken ct) => await GetCultureBaseAsync(user, "UiCulture", ct);

    private async Task SetCultureBaseAsync(TUser user, CultureInfo? culture, string type, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        IEnumerable<Claim> userClaims = await GetClaimsAsync(user, ct);
        Claim? existingClaim = userClaims.SingleOrDefault(claim => claim.Type == $"{_settingClaimPrefix}:{type}");
        if (culture is not null)
        {
            Claim newClaim = new($"{_settingClaimPrefix}:{type}", culture.Name);
            if (existingClaim is null)
            {
                await AddClaimsAsync(user, [newClaim], ct);
            }
            else
            {
                await ReplaceClaimAsync(user, existingClaim, newClaim, ct);
            }
        }
        else if (existingClaim is not null)
        {
            await RemoveClaimsAsync(user, [existingClaim], ct);
        }
    }

    private async Task<CultureInfo?> GetCultureBaseAsync(TUser user, string type, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        IEnumerable<Claim> userClaims = await GetClaimsAsync(user, ct);
        string? cultureValue = userClaims.SingleOrDefault(claim => claim.Type == $"{_settingClaimPrefix}:{type}")?.Value;

        return !string.IsNullOrEmpty(cultureValue) ? new CultureInfo(cultureValue) : null;
    }
    #endregion

    #region IUserColorThemeStore
    public async Task SetColorThemeAsync(TUser user, ColorTheme? theme, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();


        IEnumerable<Claim> userClaims = await GetClaimsAsync(user, ct);
        Claim? existingClaim = userClaims.SingleOrDefault(claim => claim.Type == $"{_settingClaimPrefix}:ColorTheme");
        if (theme is not null)
        {
            Claim newClaim = new($"{_settingClaimPrefix}:ColorTheme", theme.ToString()!);
            if (existingClaim is null)
            {
                await AddClaimsAsync(user, [newClaim], ct);
            }
            else
            {
                await ReplaceClaimAsync(user, existingClaim, newClaim, ct);
            }
        }
        else if (existingClaim is not null)
        {
            await RemoveClaimsAsync(user, [existingClaim], ct);
        }
    }

    public async Task<ColorTheme?> GetColorThemeAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        IEnumerable<Claim> userClaims = await GetClaimsAsync(user, ct);
        string? themeValue = userClaims.SingleOrDefault(claim => claim.Type == $"{_settingClaimPrefix}:ColorTheme")?.Value;

        return !string.IsNullOrEmpty(themeValue) ? Enum.Parse<ColorTheme>(themeValue) : null;
    }
    #endregion

    #region IUserApiKeyStore
    public async Task<UserApiKey?> FindApiKeyByIdAsync(string id, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(id);
        ct.ThrowIfCancellationRequested();

        ApiKey? key = await ApiKeys.FindAsync([id], ct).AsTask();
        return key?.Adapt<UserApiKey>(_apiKeyAdaptConfig);
    }

    public async Task<UserApiKey?> FindApiKeyByKeyHashAsync(string keyHash, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(keyHash);
        ct.ThrowIfCancellationRequested();

        return await ApiKeys
            .Where(key => key.KeyHash.Equals(keyHash))
            .ProjectToType<UserApiKey>(_apiKeyAdaptConfig)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<TUser?> FindUserByApiKeyAsync(UserApiKey apiKey, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(apiKey);
        ct.ThrowIfCancellationRequested();

        ApiKey? key = await ApiKeys.FindAsync([apiKey.Id], ct).AsTask();
        if (key is not null)
        {
            return await FindUserAsync(key.UserId, ct);
        }

        return null;
    }

    public async Task<UserApiKey[]> GetApiKeysByUserAsync(TUser user, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ct.ThrowIfCancellationRequested();

        string userId = await GetUserIdAsync(user, ct);
        return await ApiKeys
            .Where(key => key.UserId.Equals(userId))
            .ProjectToType<UserApiKey>(_apiKeyAdaptConfig)
            .ToArrayAsync(ct);
    }

    public async Task AddApiKeyAsync(TUser user, UserApiKey apiKey, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(apiKey);
        ct.ThrowIfCancellationRequested();

        ApiKey dbDto = apiKey.Adapt<ApiKey>(_apiKeyAdaptConfig);
        dbDto.UserId = await GetUserIdAsync(user, ct);

        await ApiKeys.AddAsync(dbDto, ct).AsTask();
    }

    public async Task RemoveApiKeyAsync(UserApiKey apiKey, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(apiKey);
        ct.ThrowIfCancellationRequested();

        await ApiKeys.Where(key => key.Id.Equals(apiKey.Id)).ExecuteDeleteAsync(ct);
    }
    #endregion
}