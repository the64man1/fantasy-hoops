using Microsoft.EntityFrameworkCore;

namespace FantasyHoops.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
