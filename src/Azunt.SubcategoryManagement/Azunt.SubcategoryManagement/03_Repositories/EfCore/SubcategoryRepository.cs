using Azunt.Models.Common;
using Azunt.SubcategoryManagement;
using Azunt.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azunt.SubcategoryManagement;

/// <summary>
/// Subcategory 테이블에 대한 Entity Framework Core 기반 리포지토리 구현체입니다.
/// </summary>
public class SubcategoryRepository : ISubcategoryRepository
{
    private readonly SubcategoryDbContextFactory _factory;
    private readonly ILogger<SubcategoryRepository> _logger;
    private readonly string? _connectionString;

    public SubcategoryRepository(
        SubcategoryDbContextFactory factory,
        ILoggerFactory loggerFactory)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<SubcategoryRepository>();
    }

    public SubcategoryRepository(
        SubcategoryDbContextFactory factory,
        ILoggerFactory loggerFactory,
        string connectionString)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<SubcategoryRepository>();
        _connectionString = connectionString;
    }

    private SubcategoryDbContext CreateContext() =>
        string.IsNullOrWhiteSpace(_connectionString)
            ? _factory.CreateDbContext()
            : _factory.CreateDbContext(_connectionString);

    public async Task<Subcategory> AddAsyncDefault(Subcategory model)
    {
        await using var context = CreateContext();
        model.Created = DateTime.UtcNow;
        model.IsDeleted = false;
        context.Subcategories.Add(model);
        await context.SaveChangesAsync();
        return model;
    }

    public async Task<Subcategory> AddAsync(Subcategory model)
    {
        await using var context = CreateContext();
        model.Created = DateTime.UtcNow;
        model.IsDeleted = false;

        // 현재 가장 높은 DisplayOrder 값 조회
        var maxDisplayOrder = await context.Subcategories
            .Where(m => !m.IsDeleted)
            .MaxAsync(m => (int?)m.DisplayOrder) ?? 0;

        model.DisplayOrder = maxDisplayOrder + 1;

        context.Subcategories.Add(model);
        await context.SaveChangesAsync();
        return model;
    }

    public async Task<IEnumerable<Subcategory>> GetAllAsync()
    {
        await using var context = CreateContext();
        return await context.Subcategories
            .Where(m => !m.IsDeleted)
            //.OrderByDescending(m => m.Id)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Subcategory> GetByIdAsync(long id)
    {
        await using var context = CreateContext();
        return await context.Subcategories
            .Where(m => m.Id == id && !m.IsDeleted)
            .SingleOrDefaultAsync()
            ?? new Subcategory();
    }

    public async Task<bool> UpdateAsync(Subcategory model)
    {
        await using var context = CreateContext();
        context.Attach(model);
        context.Entry(model).State = EntityState.Modified;
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var context = CreateContext();
        var entity = await context.Subcategories.FindAsync(id);
        if (entity == null || entity.IsDeleted) return false;

        entity.IsDeleted = true;
        context.Subcategories.Update(entity);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<Azunt.Models.Common.ArticleSet<Subcategory, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex,
        int pageSize,
        string searchField,
        string searchQuery,
        string sortOrder,
        TParentIdentifier parentIdentifier,
        string category = "")
    {
        await using var context = CreateContext();
        var query = context.Subcategories
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(m => m.Name != null && m.Name.Contains(searchQuery));
        }

        query = sortOrder switch
        {
            "Name" => query.OrderBy(m => m.Name),
            "NameDesc" => query.OrderByDescending(m => m.Name),
            "DisplayOrder" => query.OrderBy(m => m.DisplayOrder),
            _ => query.OrderBy(m => m.DisplayOrder)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new Azunt.Models.Common.ArticleSet<Subcategory, int>(items, totalCount);
    }

    public async Task<bool> MoveUpAsync(long id)
    {
        await using var context = CreateContext();
        var current = await context.Subcategories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (current == null) return false;

        var upper = await context.Subcategories
            .Where(x => x.DisplayOrder < current.DisplayOrder && !x.IsDeleted)
            .OrderByDescending(x => x.DisplayOrder)
            .FirstOrDefaultAsync();

        if (upper == null) return false;

        // Swap
        int temp = current.DisplayOrder;
        current.DisplayOrder = upper.DisplayOrder;
        upper.DisplayOrder = temp;

        // 명시적 변경 추적
        context.Subcategories.Update(current);
        context.Subcategories.Update(upper);

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveDownAsync(long id)
    {
        await using var context = CreateContext();
        var current = await context.Subcategories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (current == null) return false;

        var lower = await context.Subcategories
            .Where(x => x.DisplayOrder > current.DisplayOrder && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .FirstOrDefaultAsync();

        if (lower == null) return false;

        // Swap
        int temp = current.DisplayOrder;
        current.DisplayOrder = lower.DisplayOrder;
        lower.DisplayOrder = temp;

        // 명시적 변경 추적
        context.Subcategories.Update(current);
        context.Subcategories.Update(lower);

        await context.SaveChangesAsync();
        return true;
    }
}