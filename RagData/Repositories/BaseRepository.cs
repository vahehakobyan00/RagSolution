using Microsoft.EntityFrameworkCore;
using Rag.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Data.Repositories
{
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : class
    {
        #region Private Fields, Constructors
        protected readonly DbContext _context;
        public readonly DbSet<TEntity> _entities;

        protected BaseRepository(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _entities = context.Set<TEntity>();
            //_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
        #endregion

        #region READ/Select methods

        public async Task<TEntity> SelectAsync(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                var result = await _entities.FindAsync(predicate);
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IQueryable<TEntity> SelectAll(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                //var result = _entities.Where(predicate).ToList();
                //var result = _entities.Where(predicate);

                return _entities.Where(predicate);
            }
            catch (Exception)
            {
                return null;
            }
        }


        public IEnumerable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate)
        {
            IEnumerable<TEntity> query = _entities.Where(predicate).AsEnumerable();
            return query;
        }
        public async Task<IList<TEntity>> SelectAllAsync()
        {
            try
            {
                return await _entities.ToListAsync();
            }
            catch (Exception ex)
            {

                return null;
            }

        }
        public IQueryable<TEntity> GetAllAsQueryble()
        {
            return _entities.AsQueryable<TEntity>();
        }
        #endregion

        #region C-UD Methods
        public bool Any(Expression<Func<TEntity, bool>> predicate) =>
            _entities.Any(predicate);

        public TEntity Insert(TEntity entity)
        {
            _entities.Add(entity);
            return null;
        }
        public async Task InsertAsync(TEntity entity)
        {
            await _entities.AddAsync(entity);
        }

        public void Update(TEntity entity)
        {
            //_context.Entry(entity).State = EntityState.Modified;
            _entities.Update(entity);
        }

        public async Task DeleteAsync(params object[] keyValues) =>
            _entities.Remove(await _entities.FindAsync(keyValues));

        public void Delete(TEntity entity) =>
            _entities.Remove(entity);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public void SaveChanges() =>
            _context.SaveChanges();

        #endregion

        #region Disposabel NVI
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_entities != null)
                    _context.Dispose();
            }
        }

        public async Task<TEntity> FindById(long id)
        {
            return await _entities.FindAsync(id);
        }

        #endregion
    }
}
