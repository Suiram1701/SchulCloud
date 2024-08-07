using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchulCloud.Database.Models;

namespace SchulCloud.Database;

public class SchulCloudDbContext(DbContextOptions options) : IdentityDbContext<User, Role, string>(options)
{
}
