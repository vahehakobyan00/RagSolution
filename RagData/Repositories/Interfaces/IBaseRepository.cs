using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Repositories.Interfaces
{
    public interface IBaseRepository<TEntity> : IDisposable
        where TEntity : class
    {
        Task<TEntity> SelectAsync(Expression<Func<TEntity, bool>> predicate);

        IQueryable<TEntity> SelectAll(Expression<Func<TEntity, bool>> predicate);
        Task<IList<TEntity>> SelectAllAsync();
        IQueryable<TEntity> GetAllAsQueryble();

        IEnumerable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate);

        Task<TEntity> FindById(long id);
        #region
        bool Any(Expression<Func<TEntity, bool>> predicate);

        TEntity Insert(TEntity entity);
        Task InsertAsync(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        Task DeleteAsync(params object[] keyValues);
        Task SaveChangesAsync();
        void SaveChanges();
        #endregion
    }
}
