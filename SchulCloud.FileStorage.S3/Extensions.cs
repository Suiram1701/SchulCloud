using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SchulCloud.FileStorage.Abstractions;

namespace SchulCloud.FileStorage.S3;

public static class Extensions
{
    /// <summary>
    /// Adds the <see cref="AwsS3FileStorage{TUser}"/> as implementation for <see cref="IProfileImageStore{TUser}"/>.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IdentityBuilder AddS3ProfileImageStorage(this IdentityBuilder builder)
    {
        TryAddStore(builder);
        Type serviceType = typeof(IProfileImageStore<>).MakeGenericType(builder.UserType);
        Type storeType = typeof(AwsS3FileStorage<>).MakeGenericType(builder.UserType);
        builder.Services.AddScoped(serviceType, provider => provider.GetRequiredService(storeType));

        return builder;
    }

    private static bool TryAddStore(IdentityBuilder builder)
    {
        Type storeType = typeof(AwsS3FileStorage<>).MakeGenericType(builder.UserType);
        if (!builder.Services.Any(service => service.ImplementationType == storeType))
        {
            builder.Services.AddScoped(storeType);
            return true;
        }

        return false;
    }
}
