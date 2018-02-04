using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace RincewindORMCore
{
    public class UnitOfWork
    {
        protected SqlConnection DbConnection;

        protected readonly ConcurrentBag<ISqlQuery> SqlQueries = new ConcurrentBag<ISqlQuery>();

        protected readonly ConcurrentDictionary<string, object> Repositories = new ConcurrentDictionary<string, object>();

        public UnitOfWork(string connectionStrin)
        {
            DbConnection = new SqlConnection(connectionStrin);
        }

        public Repository<TEntity> Repository<TEntity>() where TEntity : class, new()
        {
            var entityName = typeof(TEntity).Name;

            if (Repositories.ContainsKey(entityName))
                return (Repository<TEntity>) Repositories[entityName];

            var repository = new Repository<TEntity>(SqlQueries, DbConnection);
            Repositories.TryAdd(entityName, repository);

            return (Repository<TEntity>)Repositories[entityName];
        }

        public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object> parametrs = null)
        {
            var result = 0;

            await DbConnection.OpenAsync();

            using (var transaction = DbConnection.BeginTransaction())
            {
                using (var command = DbConnection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = query;
                    if (parametrs != null)
                    {
                        foreach (var parametr in parametrs)
                        {
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = parametr.Key;
                            parameter.Value = parametr.Value;
                            command.Parameters.Add(parameter);
                        }
                    }

                    result += command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            DbConnection.Close();

            return result;
        }

        public int ExecuteNonQuery(string query, Dictionary<string, object> parametrs = null)
        {
            var result = 0;

            DbConnection.Open();

            using (var transaction = DbConnection.BeginTransaction())
            {
                using (var command =  DbConnection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = query;
                    if (parametrs != null)
                    {
                        foreach (var parametr in parametrs)
                        {
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = parametr.Key;
                            parameter.Value = parametr.Value;
                            command.Parameters.Add(parameter);
                        }
                    }

                    result += command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            DbConnection.Close();

            return result;
        }

        public async Task<int> SaveChangesAsync()
        {
            var result = 0;

            await DbConnection.OpenAsync();

            using (var transaction = DbConnection.BeginTransaction())
            {
                using (var command = DbConnection.CreateCommand())
                {
                    command.Transaction = transaction;
                    foreach (var query in SqlQueries)
                    {
                        var countOfParametersInQuery = query.Parameters.Count;
                        var indexOfLastAddParameter = 0;

                        command.CommandText = query.GetQueryText;
                        for (var i = 0; i < countOfParametersInQuery; i++)
                        {
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = QueriesBuilder.GetParameretName(indexOfLastAddParameter);
                            parameter.Value = query.Parameters[indexOfLastAddParameter];
                            command.Parameters.Add(parameter);
                            indexOfLastAddParameter++;
                        }
                        result += command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
                    SqlQueries.Clear();
                }
                transaction.Commit();
            }
            DbConnection.Close();

            return result;
        }

        public int SaveChanges()
        {
            var result = 0;

            DbConnection.Open();

            using (var transaction = DbConnection.BeginTransaction())
            {
                using (var command = DbConnection.CreateCommand())
                {
                    command.Transaction = transaction;
                    foreach (var query in SqlQueries)
                    {
                        var countOfParametersInQuery = query.Parameters.Count;
                        var indexOfLastAddParameter = 0;

                        command.CommandText = query.GetQueryText;
                        for (var i = 0; i < countOfParametersInQuery; i++)
                        {
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = QueriesBuilder.GetParameretName(indexOfLastAddParameter);
                            parameter.Value = query.Parameters[indexOfLastAddParameter];
                            command.Parameters.Add(parameter);
                            indexOfLastAddParameter++;
                        }
                        result += command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
                    SqlQueries.Clear();
                }
                transaction.Commit();
            }
            DbConnection.Close();

            return result;
        }

        ~UnitOfWork()
        {
            if (DbConnection == null) return;

            DbConnection.Dispose();
            DbConnection = null;
        }
    }
}
