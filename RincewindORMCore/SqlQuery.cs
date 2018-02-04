using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RincewindORMCore
{
    internal sealed class SqlQuery<TEntity> : ISqlQuery where TEntity : class 
    {
        public TypeOfQuery TypeOfQuery { get; }

        public string GetQueryText
        {
            get
            {
                switch (TypeOfQuery)
                {
                    case TypeOfQuery.Delete:
                        return QueriesBuilder.GetDeleteRangeQuery(CountOfRecords);
                    case TypeOfQuery.Update:
                        return QueriesBuilder.UpdateQuery;
                    default:
                        return QueriesBuilder.InsertRangeQueries[CountOfRecords-1];
                }
            }
        }

        public QueriesBuilder QueriesBuilder { get; private set; }

        public List<object> Parameters { get; private set; }

        public int CountOfRecords { get; private set; }

        public int MaxCountOfRecords
        {
            get
            {
                switch (TypeOfQuery)
                {
                    case TypeOfQuery.Delete:
                        return QueriesBuilder.RepositoryInfo.MaxCountOfParametersInQuery - 2;
                    case TypeOfQuery.Update:
                        return QueriesBuilder.RepositoryInfo.MaxCountOfEntitiesInQuery;
                    case TypeOfQuery.Insert:
                        return QueriesBuilder.RepositoryInfo.MaxCountOfEntitiesOneQueryWithoutPrimaryKey;
                    default:
                        return QueriesBuilder.RepositoryInfo.CountOfColumnsWithoutPrimaryKey;
                }
            }
        }

        public SqlQuery(TypeOfQuery typeOfQuery, QueriesBuilder queriesBuilder, IEnumerable<TEntity> entities)
        {
            TypeOfQuery = typeOfQuery;
            QueriesBuilder = queriesBuilder;
            Parameters = GetParametersForQuery(entities);
        }

        public SqlQuery(TypeOfQuery typeOfQuery, QueriesBuilder queriesBuilder, TEntity entity)
        {
            TypeOfQuery = typeOfQuery;
            QueriesBuilder = queriesBuilder;
            Parameters = new List<object>();
            Parameters.AddRange(GetParametersForQuery(entity));
            CountOfRecords = 1;
        }

        public void AddRecord(object recordObject)
        {
            CountOfRecords++;
            Parameters.AddRange(GetParametersForQuery((TEntity)recordObject));
        }

        private List<object> GetParametersForQuery(IEnumerable<TEntity> entities)
        {
            var parameters = new List<object>();

            foreach (var entity in entities)
            {
                parameters.AddRange(GetParametersForQuery(entity));
                CountOfRecords++;
            }
            return parameters;
        }

        private IEnumerable<object> GetParametersForQuery(TEntity entity)
        {
            var parameters = new List<object>();

            if (TypeOfQuery == TypeOfQuery.Delete)
            {
                var value = QueriesBuilder.RepositoryInfo
                    .PropertiesInfo[QueriesBuilder.RepositoryInfo.ColumnsInDbNames[0]]
                    .GetValue(entity, null);
                parameters.Add(value);
                return parameters;
            }

            var i = TypeOfQuery == TypeOfQuery.Update ? 0 : 1;

            //RepositoryConfig.ColumnsNames[0] - primary key
            for (; i < QueriesBuilder.RepositoryInfo.CountOfColumns; i++)
            {
                var value = QueriesBuilder.RepositoryInfo.PropertiesInfo[QueriesBuilder.RepositoryInfo.ColumnsInDbNames[i]]
                    .GetValue(entity, null);
                if (value == null)
                {
                    if (QueriesBuilder.RepositoryInfo.PropertiesInfo[QueriesBuilder.RepositoryInfo.ColumnsInDbNames[i]]
                        .IsDefined(typeof(RequiredAttribute), false))
                    {
                        throw new ArgumentException("Column " + QueriesBuilder.RepositoryInfo.ColumnsInDbNames[i] +
                                                    " is required");
                    }
                }

                parameters.Add(value);
            }

            return parameters;
        }
    }
}
