using Azunt.Models.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Azunt.SubcategoryManagement;

public class SubcategoryRepositoryAdoNet : ISubcategoryRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SubcategoryRepositoryAdoNet> _logger;

    public SubcategoryRepositoryAdoNet(string connectionString, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _logger = loggerFactory.CreateLogger<SubcategoryRepositoryAdoNet>();
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<Subcategory> AddAsync(Subcategory model)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Subcategories (Active, Created, CreatedBy, Name, Category, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@Active, @Created, @CreatedBy, @Name, @Category, 0)";
        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@Created", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", model.Category ?? (object)DBNull.Value);

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        if (result == null)
        {
            throw new InvalidOperationException("Failed to insert Subcategory. No ID was returned.");
        }
        model.Id = (long)result;
        return model;
    }

    public async Task<IEnumerable<Subcategory>> GetAllAsync()
    {
        var result = new List<Subcategory>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, Created, CreatedBy, Name, Category FROM Subcategories WHERE IsDeleted = 0 ORDER BY DisplayOrder";

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Subcategory
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                Created = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4),
                Category = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return result;
    }

    public async Task<Subcategory> GetByIdAsync(long id)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, Created, CreatedBy, Name, Category FROM Subcategories WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Subcategory
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                Created = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4),
                Category = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        return new Subcategory();
    }

    public async Task<bool> UpdateAsync(Subcategory model)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Subcategories SET
                Active = @Active,
                Name = @Name,
                Category = @Category
            WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Category", model.Category ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Id", model.Id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Subcategories SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<ArticleSet<Subcategory, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex,
        int pageSize,
        string searchField,
        string searchQuery,
        string sortOrder,
        TParentIdentifier parentIdentifier,
        string category = "")
    {
        var items = new List<Subcategory>();
        int totalCount = 0;

        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();

        var whereClauses = new List<string> { "IsDeleted = 0" };
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            whereClauses.Add("Name LIKE @SearchQuery");
            cmd.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            whereClauses.Add("Category = @Category");
            cmd.Parameters.AddWithValue("@Category", category);
        }

        string where = string.Join(" AND ", whereClauses);
        string orderBy = sortOrder switch
        {
            "Name" => "ORDER BY Name",
            "NameDesc" => "ORDER BY Name DESC",
            "DisplayOrder" => "ORDER BY DisplayOrder",
            _ => "ORDER BY DisplayOrder"
        };

        cmd.CommandText = $@"
            SELECT COUNT(*) FROM Subcategories WHERE {where};
            SELECT Id, Active, Created, CreatedBy, Name, Category
            FROM Subcategories
            WHERE {where}
            {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        cmd.Parameters.AddWithValue("@Offset", pageIndex * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            totalCount = reader.GetInt32(0);
        }

        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new Subcategory
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                Created = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4),
                Category = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return new ArticleSet<Subcategory, int>(items, totalCount);
    }

    public async Task<bool> MoveUpAsync(long id)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = "SELECT Id, DisplayOrder FROM Subcategories WHERE Id = @Id AND IsDeleted = 0";
        cmd1.Parameters.AddWithValue("@Id", id);

        using var reader1 = await cmd1.ExecuteReaderAsync();
        if (!await reader1.ReadAsync()) return false;

        long currentId = reader1.GetInt64(0);
        int currentOrder = reader1.GetInt32(1);
        await reader1.CloseAsync();

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = @"
        SELECT TOP 1 Id, DisplayOrder 
        FROM Subcategories 
        WHERE DisplayOrder < @CurrentOrder AND IsDeleted = 0 
        ORDER BY DisplayOrder DESC";
        cmd2.Parameters.AddWithValue("@CurrentOrder", currentOrder);

        using var reader2 = await cmd2.ExecuteReaderAsync();
        if (!await reader2.ReadAsync()) return false;

        long upperId = reader2.GetInt64(0);
        int upperOrder = reader2.GetInt32(1);
        await reader2.CloseAsync();

        using var tx = conn.BeginTransaction();
        try
        {
            using var cmdUpdate1 = conn.CreateCommand();
            cmdUpdate1.Transaction = tx;
            cmdUpdate1.CommandText = "UPDATE Subcategories SET DisplayOrder = @NewOrder WHERE Id = @Id";
            cmdUpdate1.Parameters.AddWithValue("@NewOrder", upperOrder);
            cmdUpdate1.Parameters.AddWithValue("@Id", currentId);
            await cmdUpdate1.ExecuteNonQueryAsync();

            using var cmdUpdate2 = conn.CreateCommand();
            cmdUpdate2.Transaction = tx;
            cmdUpdate2.CommandText = "UPDATE Subcategories SET DisplayOrder = @NewOrder WHERE Id = @Id";
            cmdUpdate2.Parameters.AddWithValue("@NewOrder", currentOrder);
            cmdUpdate2.Parameters.AddWithValue("@Id", upperId);
            await cmdUpdate2.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> MoveDownAsync(long id)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = "SELECT Id, DisplayOrder FROM Subcategories WHERE Id = @Id AND IsDeleted = 0";
        cmd1.Parameters.AddWithValue("@Id", id);

        using var reader1 = await cmd1.ExecuteReaderAsync();
        if (!await reader1.ReadAsync()) return false;

        long currentId = reader1.GetInt64(0);
        int currentOrder = reader1.GetInt32(1);
        await reader1.CloseAsync();

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = @"
        SELECT TOP 1 Id, DisplayOrder 
        FROM Subcategories 
        WHERE DisplayOrder > @CurrentOrder AND IsDeleted = 0 
        ORDER BY DisplayOrder ASC";
        cmd2.Parameters.AddWithValue("@CurrentOrder", currentOrder);

        using var reader2 = await cmd2.ExecuteReaderAsync();
        if (!await reader2.ReadAsync()) return false;

        long lowerId = reader2.GetInt64(0);
        int lowerOrder = reader2.GetInt32(1);
        await reader2.CloseAsync();

        using var tx = conn.BeginTransaction();
        try
        {
            using var cmdUpdate1 = conn.CreateCommand();
            cmdUpdate1.Transaction = tx;
            cmdUpdate1.CommandText = "UPDATE Subcategories SET DisplayOrder = @NewOrder WHERE Id = @Id";
            cmdUpdate1.Parameters.AddWithValue("@NewOrder", lowerOrder);
            cmdUpdate1.Parameters.AddWithValue("@Id", currentId);
            await cmdUpdate1.ExecuteNonQueryAsync();

            using var cmdUpdate2 = conn.CreateCommand();
            cmdUpdate2.Transaction = tx;
            cmdUpdate2.CommandText = "UPDATE Subcategories SET DisplayOrder = @NewOrder WHERE Id = @Id";
            cmdUpdate2.Parameters.AddWithValue("@NewOrder", currentOrder);
            cmdUpdate2.Parameters.AddWithValue("@Id", lowerId);
            await cmdUpdate2.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }
}