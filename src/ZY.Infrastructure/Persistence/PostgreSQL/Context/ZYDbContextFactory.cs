
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ZY.Infrastructure.Persistence.PostgreSQL.Context;

namespace ZY.Infrastructure.Persistence.PostgreSQL;

public sealed class ZYDbContextFactory
    : IDesignTimeDbContextFactory<ZYDbContext>
{
    public ZYDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ZYDbContext>()
            .UseNpgsql(PostgreSqlConfiguration.ConnectionString)
            .Options;

        return new ZYDbContext(options);
    }
}
