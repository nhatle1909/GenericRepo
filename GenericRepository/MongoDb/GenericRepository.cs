using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace GenericRepository.NewFolder
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly IMongoCollection<TEntity> _collection;
        public GenericRepository(IMongoDatabase database, string collectionName)
        {
            _collection = database.GetCollection<TEntity>(collectionName);
        }
        // Generic repository methods can be defined here
        public async Task<(bool, string)> AddItemAsync(TEntity item)
        {
            if (item == null) return (false, "Item can not be null");

            try
            {
                await _collection.InsertOneAsync(item);

                return (true, "Add new item success");
            }
            catch (Exception ex)
            {
                return (false, "Error while adding new item: " + ex.Message + "|" + ex.ToString());
            }
        }

        public async Task<(bool, string)> AddManyItemAsync(IEnumerable<TEntity> items)
        {
            if (items.Count() == 0) return (false, "List item can not be null or empty");

            try
            {
                await _collection.InsertManyAsync(items);

                return (true, "Add new items success");
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
                FilterDefinition<TEntity> filterDefinition = FilterDefinitionsBuilder(searchParams);
                //Query
                var count = await _collection.CountDocumentsAsync(filterDefinition);
                return (long)Math.Ceiling((double)count / pageSize);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error while counting items: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(IEnumerable<TEntity>?, bool, string)> GetByFilterAsync(Expression<Func<TEntity, bool>>? filter = null, BsonDocument[]? aggregation = null)
        {
            try
            {
                var result = await _collection.Find(filter).ToListAsync();

                if (result == null || result.Count() == 0)
                {
                    return (null, false, "Item not found");
                }
                if (aggregation != null)
                {
                    foreach (var item in aggregation)
                    {
                        result = await _collection.Aggregate().AppendStage<TEntity>(item).ToListAsync();
                    }
                }
                return (result, true, "Get item success");
            }
            catch (Exception ex)
            {
                return (null, false, "Error while getting item: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(TEntity?, bool, string)> GetByIdAsync(Guid Id)
        {
            try
            {
                var filter = Builders<TEntity>.Filter.Eq("_id", Id.ToString());
                var result = await _collection.Find(filter).FirstOrDefaultAsync();
                if (result == null)
                {
                    return (null, false, "Item not found");
                }
                return (result, true, "Get item success");
            }
            catch (Exception ex)
            {
                return (null, false, "Error while getting item: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(IEnumerable<TEntity>?, bool, string)> GetPagingAsync(Dictionary<string, string> searchParams, string? sortField = null, int? pageSize = 5, int? skip = 1, BsonDocument[]? aggregation = null)
        {
            try
            {

                //Create Filter
                FilterDefinition<TEntity> filterDefinition = FilterDefinitionsBuilder(searchParams);

                //Query
                IAggregateFluent<TEntity> query = _collection.Aggregate().Match(filterDefinition);

                if (query == null)
                {
                    return (null, false, "Item not found");
                }
                //Sort
                if (sortField != null)
                {
                    SortDefinition<TEntity> sortDefinition = sortField.StartsWith("!") ? Builders<TEntity>.Sort.Ascending(sortField) : Builders<TEntity>.Sort.Descending(sortField);
                    query = query.Sort(sortDefinition);
                }

                // Paging
                var intSkip = skip == null ? 1 : (int)skip;
                var intPageSize = pageSize == null ? 5 : (int)pageSize;
                query = query.Skip((intSkip - 1) * intPageSize)
                             .Limit(intPageSize);

                if (aggregation != null)
                {
                    foreach (var item in aggregation)
                    {
                        query = query.AppendStage<TEntity>(item);
                    }
                }


                var result = await query.ToListAsync();

                return (result, true, "");
            }
            catch (Exception ex)
            {
                return (null, false, "Error while getting items: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(bool, string)> RemoveItemAsync(Guid Id)
        {
            try
            {

                var filter = Builders<TEntity>.Filter.Eq("_id", Id.ToString());

                await _collection.DeleteOneAsync(filter);

                return (true, "Delete item success");
            }
            catch (Exception ex)
            {
                return (false, "Error while deleting item: " + ex.Message + " | " + ex.ToString());
            }
        }


        public async Task<(bool, string)> SoftRemoveItemAsync(Guid Id)
        {
            try
            {
                var filter = Builders<TEntity>.Filter.Eq("_id", Id.ToString());

                var updateDefinition = Builders<TEntity>.Update.Set("IsDeleted", true);

                var result = await _collection.UpdateOneAsync(filter, updateDefinition);
                if (result.ModifiedCount > 0)
                {
                    return (true, "Soft delete item success");
                }
                else
                {
                    return (false, "Item not found or no changes made");
                }
            }
            catch (Exception ex)
            {
                return (false, "Error while soft deleting item: " + ex.Message + " | " + ex.ToString());
            }
        }

        public async Task<(bool, string)> UpdateItemAsync(Guid Id, TEntity newItem)
        {
            try
            {
                var updateDefinition = Builders<TEntity>.Update.Set(T => T, newItem);

                var filter = Builders<TEntity>.Filter.Eq("_id", Id.ToString());

                var result = await _collection.UpdateOneAsync(filter, updateDefinition);
                if (result.ModifiedCount > 0)
                {
                    return (true, "Update item success");
                }
                else
                {
                    return (false, "Item not found or no changes made");
                }
            }
            catch (Exception ex)
            {
                return (false, "Error while updating item: " + ex.Message + " | " + ex.ToString());
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------
        private FilterDefinition<TEntity> FilterDefinitionsBuilder(Dictionary<string, string> searchTerms)
        {
            var isNotDeletedFilter = Builders<TEntity>.Filter.Eq("isDeleted", false);
            var combinedFilter = isNotDeletedFilter;

            foreach (var term in searchTerms)
            {
                string fieldName = term.Key;
                string searchText = term.Value;

                combinedFilter = Builders<TEntity>.Filter.And(combinedFilter, BuildFilterForField(fieldName, searchText));
            }

            return combinedFilter;
        }

        private FilterDefinition<TEntity> BuildFilterForField(string fieldName, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return Builders<TEntity>.Filter.Empty;
            }

            if (searchText.Contains(","))
            {
                var multipleValues = searchText.Split(',');
                var orFilters = Builders<TEntity>.Filter.Empty;
                foreach (var singleValue in multipleValues)
                {
                    orFilters = Builders<TEntity>.Filter.And(orFilters, CreateSingleValueFilter(fieldName, singleValue));
                }
                return orFilters;
            }

            return CreateSingleValueFilter(fieldName, searchText);
        }

        private FilterDefinition<TEntity> CreateSingleValueFilter(string fieldName, string searchText)
        {
            var regexSearch = new BsonRegularExpression(new Regex(searchText, RegexOptions.None));
            var regexFilter = Builders<TEntity>.Filter.Regex(fieldName, regexSearch);

            if (bool.TryParse(searchText, out var booleanValue))
            {
                return Builders<TEntity>.Filter.Eq(fieldName, booleanValue);
            }

            if (int.TryParse(searchText, out var integerValue))
            {
                return Builders<TEntity>.Filter.Eq(fieldName, integerValue);
            }

            if (searchText.StartsWith("!"))
            {
                return Builders<TEntity>.Filter.Ne(fieldName, searchText.Substring(1));
            }

            return regexFilter;
        }
    }
}
