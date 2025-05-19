using Azunt.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azunt.SubcategoryManagement;

/// <summary>
/// SubcategoryApp 의존성 주입 확장 메서드
/// </summary>
public static class SubcategoryServicesRegistrationExtensions
{
    /// <summary>
    /// SubcategoryApp 모듈의 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="connectionString">기본 연결 문자열</param>
    /// <param name="mode">레포지토리 모드 (EfCore, Dapper, AdoNet)</param>
    /// <param name="dbContextLifetime">DbContext 수명 주기 (기본: Transient)</param>
    public static void AddDependencyInjectionContainerForSubcategoryApp(
        this IServiceCollection services,
        string connectionString,
        RepositoryMode mode = Azunt.Models.Enums.RepositoryMode.EfCore,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Transient)
    {
        switch (mode)
        {
            case RepositoryMode.EfCore:
                // EF Core 방식 등록
                services.AddDbContext<SubcategoryDbContext>(
                    options => options.UseSqlServer(connectionString),
                    dbContextLifetime);

                services.AddTransient<ISubcategoryRepository, SubcategoryRepository>();
                services.AddTransient<SubcategoryDbContextFactory>();
                break;

            case RepositoryMode.Dapper:
                // Dapper 방식 등록
                services.AddTransient<ISubcategoryRepository>(provider =>
                    new SubcategoryRepositoryDapper(
                        connectionString,
                        provider.GetRequiredService<ILoggerFactory>()));
                break;

            case RepositoryMode.AdoNet:
                // ADO.NET 방식 등록
                services.AddTransient<ISubcategoryRepository>(provider =>
                    new SubcategoryRepositoryAdoNet(
                        connectionString,
                        provider.GetRequiredService<ILoggerFactory>()));
                break;

            default:
                throw new InvalidOperationException(
                    $"Invalid repository mode '{mode}'. Supported modes: EfCore, Dapper, AdoNet.");
        }
    }
}