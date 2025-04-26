using MongoDB.Bson;
using System.Linq.Expressions;

namespace GenericRepository.NewFolder
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Add new item to collection
        /// </summary>
        /// <returns>Return bool value and string message for result of method</returns>
        public Task<(bool, string)> AddItemAsync(TEntity item);
        /// <summary>
        /// Add many new items to collection
        ///</summary>
        /// <returns>Return bool value and string message for result of method</returns>
        public Task<(bool, string)> AddManyItemAsync(IEnumerable<TEntity> items);
        ///<summary>
        /// Remove an item from collection permanently
        /// </summary>
        /// <returns>Return bool value and string message for result of method</returns>
        public Task<(bool, string)> RemoveItemAsync(Guid Id);
        /// <summary>
        /// Soft remove an item. Entities must has "isDeleted" field.
        /// </summary>
        /// <returns>Return bool value and string message for result of method</returns>
        public Task<(bool, string)> SoftRemoveItemAsync(Guid Id);
        /// <summary>
        /// Get item by Id field from collection
        /// </summary>
        /// <returns>Return item and bool value and string message for result of method</returns>
        public Task<(TEntity, bool, string)> GetByIdAsync(Guid Id);
        /// <summary>
        /// Update an item in collection by Id field and new item
        /// </summary>
        /// <returns>Return bool value and string message for result of method</returns>
        public Task<(bool, string)> UpdateItemAsync(Guid Id, TEntity newItem);
        /// <summary>
        /// Get item list by filter with paging, sorting
        /// </summary>
        /// <param name="searchParams"> Dictionary of search params. Key is field name and value is field value. Field value is string and can have many value, split by ","</param>
        /// <param name="sortField"> Sort field name. Default is null. Start with "!" for false value and reverse</param>
        /// <param name="pageSize">Size of item list</param>
        /// <param name="skip">Skip number of items</param>
        /// <param name="aggregation"> Aggregation pipeline for MongoDB</param>
        /// <returns>Return item list and bool value and string message for result of method</returns>
        public Task<(IEnumerable<TEntity>, bool, string)> GetPagingAsync(Dictionary<string, string> searchParams, string? sortField = null, int? pageSize = 5, int? skip = 1, BsonDocument[]? aggregation = null);
        /// <summary>
        /// Get item list by filter expression
        /// </summary>
        /// <param name="filter"> Filter expression. Default is null</param>
        /// <param name="aggregation"> Aggregation pipeline for MongoDB</param> 
        /// <returns>Return item list and bool value and string message for result of method</returns>
        public Task<(IEnumerable<TEntity>, bool, string)> GetByFilterAsync(Expression<Func<TEntity, bool>>? filter = null, BsonDocument[]? aggregation = null);
        /// <summary>
        /// Count number of items in collection by condition
        /// </summary>
        /// <param name="searchParams"> Dictionary of search params. Key is field name and value is field value. Field value is string and can have many value, split by ","</param>
        /// <param name="pageSize">Size of item list</param>
        /// <returns>Return number of items and bool value and string message for result of method</returns>
        public Task<long> CountAsync(Dictionary<string, string> searchParams, int pageSize = 5);
    }
}
