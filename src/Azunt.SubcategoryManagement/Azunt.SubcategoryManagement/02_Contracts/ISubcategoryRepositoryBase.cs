using Azunt.Repositories;

namespace Azunt.SubcategoryManagement;

/// <summary>
/// 기본 CRUD 작업을 위한 Subcategory 전용 저장소 인터페이스
/// </summary>
public interface ISubcategoryRepositoryBase : IRepositoryBase<Subcategory, long>
{
}