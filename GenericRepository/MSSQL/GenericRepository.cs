using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace GenericRepository.MSSQL
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        public DbSet<TEntity> _dbSet;
        public GenericRepository(DbContext _context)
        {
            _dbSet = _context.Set<TEntity>();
        }
        public async Task<(bool, string)> AddItemAsync(TEntity item)
        {
            if (item == null)
            {
                return (false, "Item is null");
            }
            try
            {
                await _dbSet.AddAsync(item);
                return (true, "Add new item successfully");
            }
            catch (Exception ex)
            {
                return (false, "Error while adding new item: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(bool, string)> AddManyItemAsync(IEnumerable<TEntity> items)
        {
            if (items == null || !items.Any())
            {
                return (false, "Items list is null or empty");
            }
            try
            {
                await _dbSet.AddRangeAsync(items);
                return (true, "Add new items successfully");
            }
            catch (Exception ex)
            {
                return (false, "Error while adding new items: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<long> CountAsync(Dictionary<string, string> searchParams, int pageSize = 5)
        {
            try
            {
                var query = _dbSet.AsQueryable();
                searchParams.Add("IsDeleted", "false");
                if (searchParams != null)
                {
                    foreach (KeyValuePair<string, string> keyValuePair in searchParams)
                    {

                        query = query.Where(string.Format("{0} = {1}", keyValuePair.Key, keyValuePair.Value));
                    }
                }
                float count = await query.CountAsync();
                return (long)MathF.Ceiling(count / pageSize);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public async Task<(IEnumerable<TEntity>?, bool, string)> GetByFilterAsync(Expression<Func<TEntity, bool>>? filter = null, string? includeProperties = null)
        {
            try
            {
                var query = _dbSet.AsQueryable();
                if (filter != null)
                {
                    query = query.Where(filter);
                }
                if (includeProperties != null)
                {
                    foreach (var includeProperty in includeProperties.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        query = query.Include(includeProperty);
                    }
                }
                var result = await query.ToListAsync();
                if (result == null || result.Count() == 0) return (null, false, "Item list not found or empty");
                return (result, true, "Get item list successfully");
            }
            catch (Exception ex)
            {
                return (null, false, "Error while getting item list: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(TEntity?, bool, string)> GetByIdAsync(Guid Id)
        {
            if (Id == Guid.Empty)
            {
                return (null, false, "Id is null");
            }
            try
            {
                var query = _dbSet.Where(q => q.Id.Equals(Id) && q.IsDeleted == false).First();
                if (query == null) return (null, false, "Item not found");
                return (query, true, "Get item by Id successfully");
            }
            catch (Exception ex)
            {
                return (null, false, "Error while getting item by Id: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(IEnumerable<TEntity>?, bool, string)> GetPagingAsync(Dictionary<string, string> searchParams, string? sortField = null, int? pageSize = 5, int? skip = 1, string? includeProperties = null)
        {
            try
            {
                var query = _dbSet.AsQueryable();

                if (searchParams != null)
                {
                    foreach (KeyValuePair<string, string> keyValuePair in searchParams)
                    {

                        query = query.Where(string.Format("{0} = {1}", keyValuePair.Key, keyValuePair.Value));
                    }
                }

                if (query == null) return (null, false, "No item found");

                if (sortField != null)
                {
                    if (sortField.StartsWith("!")) query = query.OrderBy($"{sortField} descending");
                    else query = query.OrderBy($"{sortField} ascending");
                }
                //int? to int 
                var intPageSize = pageSize == null ? 5 : (int)pageSize;
                var intSkip = skip == null ? 1 : (int)skip;
                query = query.Take(intPageSize)
                                 .Skip((intSkip - 1) * intPageSize);

                if (includeProperties != null)
                {
                    foreach (var includeProp in includeProperties.Split(new char[] { ',' },
                                 StringSplitOptions.RemoveEmptyEntries))
                    {
                        query = query.Include(includeProp);
                    }
                }
                var result = await query.ToListAsync();
                if (result == null || result.Count() == 0) return (null, false, "Item list not found or empty");
                return (result, true, "Retrieve data successfully");
            }
            catch (Exception ex)
            {
                return (null, false, "Error while getting item list: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(bool, string)> RemoveItemAsync(Guid Id)
        {
            if (Id == Guid.Empty)
            {
                return (false, "Id is null");
            }
            try
            {
                var query = await _dbSet.FindAsync(Id);

                if (query == null) return (false, "Item not found");

                _dbSet.Remove(query);

                return (true, "Deleted Item successfully");
            }
            catch (Exception ex)
            {
                return (false, "Error while deleting item: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(bool, string)> SoftRemoveItemAsync(Guid Id)
        {
            if (Id == Guid.Empty)
            {
                return (false, "Id is null");
            }
            try
            {
                var query = await GetByIdAsync(Id);
                var item = query.Item1;
                if (item == null) return (false, "Item not found");

                item.IsDeleted = true;
                _dbSet.Update(item);
                return (true, "Deleted Item successfully");
            }
            catch (Exception ex)
            {
                return (false, "Error while deleting item: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(bool, string)> UpdateItemAsync(Guid Id, TEntity newItem)
        {
            if (Id == Guid.Empty)
            {
                return (false, "Id is null");
            }
            if (newItem == null)
            {
                return (false, "Item is null");
            }
            try
            {
                var query = await GetByIdAsync(Id);
                var item = query.Item1;
                if (item == null) return (false, "Item not found");

                newItem.UpdatedAt = DateTime.Now.ToString("d", new CultureInfo("vi-VN"));

                _dbSet.Update(newItem);

                return (true, "Updated item successfully");
            }
            catch (Exception ex)
            {
                return (false, "Error while updating item: " + ex.Message + " | " + ex.ToString());
            }
        }
    }
}
