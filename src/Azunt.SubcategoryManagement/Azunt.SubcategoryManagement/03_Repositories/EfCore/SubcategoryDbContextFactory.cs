using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.SubcategoryManagement;

public class SubcategoryDbContextFactory
{
    private readonly IConfiguration? _configuration;

    public SubcategoryDbContextFactory() { }

    public SubcategoryDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SubcategoryDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SubcategoryDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SubcategoryDbContext(options);
    }

    public SubcategoryDbContext CreateDbContext(DbContextOptions<SubcategoryDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new SubcategoryDbContext(options);
    }

    public SubcategoryDbContext CreateDbContext()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not provided.");
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection is not configured properly.");
        }

        return CreateDbContext(defaultConnection);
    }
}