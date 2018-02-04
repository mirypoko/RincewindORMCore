using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace RincewindORMCore
{
    public class Repository<TEntity> where TEntity : class, new()
    {
        protected readonly ConcurrentBag<ISqlQuery> SqlQueries;

        protected readonly SqlConnection Connection;

        protected static QueriesBuilder QueriesBuilder { get; set; }

        protected static IRepositoryInfo RepositoryInfo { get; set; }

        public Repository(ConcurrentBag<ISqlQuery> sqlQueries, SqlConnection connection)
        {
            SqlQueries = sqlQueries;

            Connection = connection;

            if (RepositoryInfo != null) return;

            RepositoryInfo = new RepositoryInfo<TEntity>(2100);
            QueriesBuilder = new QueriesBuilder(RepositoryInfo);
        }

        public void Add(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!TryAddingEntitiesToThePreviousQuery(TypeOfQuery.Insert, entity))
            {
                SqlQueries.Add(new SqlQuery<TEntity>(TypeOfQuery.Insert, QueriesBuilder, entity));
            }
        }

        public void AddRange(List<TEntity> entities)
        {
            var bufCollection = entities.ToList();

            bufCollection = TryAddingEntitiesToThePreviousQuery(TypeOfQuery.Insert, bufCollection);

            while (bufCollection.Count > RepositoryInfo.MaxCountOfEntitiesOneQueryWithoutPrimaryKey)
            {

                var entitiesForQuery = bufCollection.Take(RepositoryInfo.MaxCountOfEntitiesOneQueryWithoutPrimaryKey);
                SqlQueries.Add(new SqlQuery<TEntity>(TypeOfQuery.Insert, QueriesBuilder, entitiesForQuery));
                bufCollection.RemoveRange(0, RepositoryInfo.MaxCountOfEntitiesOneQueryWithoutPrimaryKey);
            }

            if (bufCollection.Any())
            {
                SqlQueries.Add(new SqlQuery<TEntity>(TypeOfQuery.Insert, QueriesBuilder, bufCollection));
            }
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            AddRange(entities.ToList());

        }

        public void AddRange(params TEntity[] entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            AddRange(entities.ToList());
        }

        public void Remove(TEntity entity)
        {
            if (entity == null) return;

            if (TryAddingEntitiesToThePreviousQuery(TypeOfQuery.Delete, entity)) return;

            SqlQueries.Add(new SqlQuery<TEntity>(TypeOfQuery.Delete, QueriesBuilder, entity));
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            var entityList = TryAddingEntitiesToThePreviousQuery(TypeOfQuery.Delete, entities.ToList());

            if (!entityList.Any()) return;

            if (entityList.Count < RepositoryInfo.MaxCountOfEntitiesOneQueryWithoutPrimaryKey)
            {
                SqlQueries.Add(new SqlQuery<TEntity>(TypeOfQuery.Delete, QueriesBuilder, entityList));
                return;
            }

            SqlQuery<TEntity> query = null;

            foreach (var entity in entityList)
            {
                if (query == null)
                {
                    query = new SqlQuery<TEntity>(TypeOfQuery.Delete, QueriesBuilder, entity);
                }
                else
                {
                    query.AddRecord(entity);
                }

                if (query.CountOfRecords == query.MaxCountOfRecords)
                {
                    SqlQueries.Add(query);
                    query = null;
                }
            }

            if (query != null)
                SqlQueries.Add(query);
        }

        public void Update(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            SqlQueries.Add(new SqlQuery<TEntity>(TypeOfQuery.Update, QueriesBuilder, entity));
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }

        public async Task<TEntity> FindAsync(object key)
        {
            var typeOfKey = key.GetType();

            if (typeOfKey != RepositoryInfo.PropertiesInfo[RepositoryInfo.ColumnsInDbNames[0]].PropertyType
                && typeOfKey != typeof(string))
            {
                throw new ArgumentException("The primary key is of an inappropriate type", nameof(key));
            }

            var entity = new TEntity();

            using (var command = Connection.CreateCommand())
            {
                command.CommandText = QueriesBuilder.GetQuery;
                await Connection.OpenAsync();

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@key";
                parameter.Value = key;
                command.Parameters.Add(parameter);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var fieldCount = reader.FieldCount;
                        for (int i = 0; i < fieldCount; i++)
                        {
                            var value = reader.GetValue(i);
                            var valueName = reader.GetName(i);
                            RepositoryInfo.PropertiesInfo.TryGetValue(valueName, out var propertyInfo);
                            if (propertyInfo != null)
                            {
                                propertyInfo.SetValue(entity, value, null);
                            }
                        }
                    }
                }
                Connection.Close();
            }
            return entity;
        }

        public TEntity Find(object key)
        {
            var typeOfKey = key.GetType();

            if (typeOfKey != RepositoryInfo.PropertiesInfo[RepositoryInfo.ColumnsInDbNames[0]].PropertyType
                && typeOfKey != typeof(string))
            {
                throw new ArgumentException("The primary key is of an inappropriate type", nameof(key));
            }

            var entity = new TEntity();

            using (var command = Connection.CreateCommand())
            {
                command.CommandText = QueriesBuilder.GetQuery;
                Connection.Open();

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@key";
                parameter.Value = key;
                command.Parameters.Add(parameter);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fieldCount = reader.FieldCount;
                        for (int i = 0; i < fieldCount; i++)
                        {
                            var value = reader.GetValue(i);
                            var valueName = reader.GetName(i);
                            RepositoryInfo.PropertiesInfo.TryGetValue(valueName, out var propertyInfo);
                            if (propertyInfo != null)
                            {
                                propertyInfo.SetValue(entity, value, null);
                            }
                        }
                    }
                }
                Connection.Close();
            }
            return entity;
        }

        public async Task<List<TEntity>> ToListAsync(int limit = -1)
        {
            List<TEntity> result;
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = limit != -1 ? QueriesBuilder.GetAllLimitQuery.Replace("@LIMITOFRECORDS", limit.ToString()) : QueriesBuilder.GetAllQuery;

                await Connection.OpenAsync();

                result = await GetDataFromQueryAsync(command);
                Connection.Close();
            }
            return result;
        }

        public List<TEntity> ToList(int limit = -1)
        {
            List<TEntity> result = new List<TEntity>(); ;

            SqlConnection connection = new SqlConnection(Connection.ConnectionString);
            connection.Open();

            using (IDbCommand command = connection.CreateCommand())
            {
                if (limit != -1)
                {
                    command.CommandText = QueriesBuilder.GetAllLimitQuery.Replace("@LIMITOFRECORDS", limit.ToString());
                }
                else
                {
                    command.CommandText = QueriesBuilder.GetAllQuery;
                }
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = new TEntity();

                        int i = 0;
                        foreach (var columnName in RepositoryInfo.ColumnsInDbNames)
                        {
                            object value = reader.GetValue(i++);

                            PropertyInfo propertyInfo;

                            RepositoryInfo.PropertiesInfo.TryGetValue(columnName, out propertyInfo);
                            propertyInfo.SetValue(entity, value, null);
                        }
                        result.Add(entity);
                    }
                }
                connection.Close();
            }
            return result;
        }

        public async Task<List<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var wherePart = WhereBuilder.ToSql<TEntity>(predicate);
            string sqlText = QueriesBuilder.GetAllQuery;
            sqlText = sqlText.Insert(sqlText.Length - 1, String.Concat(" Where ", wherePart.Sql));

            List<TEntity> result;
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = sqlText;
                await Connection.OpenAsync();

                foreach (var parametr in wherePart.Parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = parametr.Key;
                    parameter.Value = parametr.Value;
                    command.Parameters.Add(parameter);
                }

                result = await GetDataFromQueryAsync(command);
                Connection.Close();
            }
            return result;
        }

        public List<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            var wherePart = WhereBuilder.ToSql<TEntity>(predicate);
            string sqlText = QueriesBuilder.GetAllQuery;
            sqlText = sqlText.Insert(sqlText.Length - 1, String.Concat(" Where ", wherePart.Sql));

            List<TEntity> result;
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = sqlText;
                Connection.Open();

                foreach (var parametr in wherePart.Parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = parametr.Key;
                    parameter.Value = parametr.Value;
                    command.Parameters.Add(parameter);
                }

                result = GetDataFromQuery(command);
                Connection.Close();
            }
            return result;
        }

        #region Helpers

        private bool TryAddingEntitiesToThePreviousQuery(TypeOfQuery typeOfQuery, TEntity entity)
        {
            if (SqlQueries != null && SqlQueries.Any())
            {
                var bufQueryObject = SqlQueries.Last();

                if (bufQueryObject.TypeOfQuery == TypeOfQuery.Update) return false;

                var lastQuery = bufQueryObject;

                if (lastQuery.QueriesBuilder != QueriesBuilder || lastQuery.TypeOfQuery != typeOfQuery ||
                    typeOfQuery == TypeOfQuery.Update) return false;

                if (lastQuery.CountOfRecords >= lastQuery.MaxCountOfRecords) return false;

                lastQuery.AddRecord(entity);

                return true;
            }
            return false;
        }

        private List<TEntity> TryAddingEntitiesToThePreviousQuery(TypeOfQuery typeOfQuery, List<TEntity> entities)
        {
            if (!SqlQueries.Any()) return entities;

            var lastQuery = SqlQueries.Last();

            if (lastQuery.QueriesBuilder != QueriesBuilder || lastQuery.TypeOfQuery != typeOfQuery ||
                typeOfQuery == TypeOfQuery.Update) return entities;

            if (lastQuery.CountOfRecords >= lastQuery.MaxCountOfRecords) return entities;

            var freeSpaceForNewRecords = lastQuery.MaxCountOfRecords - lastQuery.CountOfRecords;

            var countOfEntitiesToAddInLastQuery = freeSpaceForNewRecords > entities.Count ? entities.Count : freeSpaceForNewRecords;

            for (var i = 0; i < countOfEntitiesToAddInLastQuery; i++)
            {
                lastQuery.AddRecord(entities[i]);
            }

            entities.RemoveRange(0, countOfEntitiesToAddInLastQuery);
            return entities;
        }

        private static async Task<List<TEntity>> GetDataFromQueryAsync(SqlCommand command)
        {
            var result = new List<TEntity>();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var entity = new TEntity();

                    var fieldCount = reader.FieldCount;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var value = reader.GetValue(i);
                        var valueName = reader.GetName(i);
                        RepositoryInfo.PropertiesInfo.TryGetValue(valueName, out var propertyInfo);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(entity, value, null);
                        }
                    }

                    result.Add(entity);
                }
            }

            return result;
        }

        private static List<TEntity> GetDataFromQuery(IDbCommand command)
        {
            var result = new List<TEntity>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var entity = new TEntity();

                    var fieldCount = reader.FieldCount;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var value = reader.GetValue(i);
                        var valueName = reader.GetName(i);
                        RepositoryInfo.PropertiesInfo.TryGetValue(valueName, out var propertyInfo);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(entity, value, null);
                        }
                        //RepositoryInfo.PropertiesSetGet.TryGetValue(valueName, out var getSet);

                        //getSet?.Set.Invoke(entity, new[] { value });
                    }

                    result.Add(entity);
                }
            }

            return result;
        }

        #endregion
    }
}
