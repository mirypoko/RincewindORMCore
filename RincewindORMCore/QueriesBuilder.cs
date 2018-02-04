using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace RincewindORMCore
{
    public class QueriesBuilder
    {
        public IRepositoryInfo RepositoryInfo { get; protected set; }

        #region Strings helpers

        protected readonly string AllColumnsWithKeyString;

        protected readonly string AllColumnsString;

        //protected readonly string Set

        /// <summary>
        /// Key - count of parameters
        /// </summary>
        protected ConcurrentDictionary<int, string> ValuesArrayStrings = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// Key - count of parameters
        /// </summary>
        protected ConcurrentDictionary<int, string> ValuesStrings = new ConcurrentDictionary<int, string>();

        protected string PrimaryKeyNameWithAtSymbol { get; set; }

        #endregion

        #region Queries

        public string GetQuery { get; protected set; }

        public string GetAllQuery { get; protected set; }

        public string GetAllLimitQuery { get; protected set; }

        public string UpdateQuery { get; protected set; }

        /// <summary>
        /// Key - count of records
        /// </summary>
        public string[] InsertRangeQueries { get; }

        ///// <summary>
        ///// Key - count of records
        ///// </summary>
        protected string[] UpdateRangeQueries { get; }

        /// <summary>
        /// Key - count of records
        /// </summary>
        protected ConcurrentDictionary<int, string> DeleteRangQueries = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// Key - number of parameter
        /// </summary>
        protected static ConcurrentDictionary<int, string> ParametersNames = new ConcurrentDictionary<int, string>();

        #endregion

        #region Public constructor

        public QueriesBuilder(IRepositoryInfo repositoryInfo)
        {
            RepositoryInfo = repositoryInfo;

            PrimaryKeyNameWithAtSymbol = "@" + RepositoryInfo.PrimaryKeyName;

            AllColumnsString = GetAllColumns();

            AllColumnsWithKeyString = GetAllColumnsWithKey();

            UpdateQuery = GetUpdateString();

            GetAllQuery = String.Concat("SELECT ", AllColumnsWithKeyString, " FROM [", RepositoryInfo.TableInDbName, "];");

            GetQuery = String.Concat("SELECT TOP 1 ", AllColumnsWithKeyString, " FROM [", RepositoryInfo.TableInDbName, "] WHERE [", RepositoryInfo.PrimaryKeyName, "] = @key;");

            GetAllLimitQuery = String.Concat("SELECT TOP (@LIMITOFRECORDS) ", AllColumnsWithKeyString, " FROM [", RepositoryInfo.TableInDbName, "];");

            InsertRangeQueries = new string[repositoryInfo.MaxCountOfEntitiesOneQueryWithoutPrimaryKey];
            SetInsertQueries(repositoryInfo.MaxCountOfEntitiesOneQueryWithoutPrimaryKey);

            UpdateRangeQueries = new string[repositoryInfo.MaxCountOfEntitiesInQuery];


        }

        #endregion

        #region Build query 

        public void SetInsertQueries(int maxCountOfEntities)
        {
            for (int i = 0; i < maxCountOfEntities; i++)
            {
                InsertRangeQueries[i] = String.Concat("SET NOCOUNT ON;INSERT INTO [", RepositoryInfo.TableInDbName, "] ", AllColumnsString, GetValuesString(i+1), ";");
            }
        }

        public string GetDeleteRangeQuery(int countOfEntities)
        {
            DeleteRangQueries.TryGetValue(countOfEntities, out var result);

            if (result != null) { return result; }

            result = String.Concat("SET NOCOUNT ON; DELETE FROM [", RepositoryInfo.TableInDbName, "] WHERE [", RepositoryInfo.PrimaryKeyName, "] IN ", GetValuesArray(countOfEntities), ";");

            DeleteRangQueries.TryAdd(countOfEntities, result);

            return result;
        }

        #endregion

        #region Build string helpers

        protected string GetValuesArray(int countOfValues)
        {
            ValuesArrayStrings.TryGetValue(countOfValues, out var result);

            if (result != null) { return result; }

            StringBuilder stringBuilder = new StringBuilder(countOfValues * 4 + 2);
            stringBuilder.Append("(");
            stringBuilder.Append(GetParameretName(0));
            for (int i = 1; i < countOfValues; i++)
            {
                stringBuilder.Append(", ");
                stringBuilder.Append(GetParameretName(i));
            }
            stringBuilder.Append(")");

            result = stringBuilder.ToString();
            ValuesArrayStrings.TryAdd(countOfValues, result);

            return stringBuilder.ToString();
        }

        protected string GetValuesString(int countOfEntities)
        {
            ValuesStrings.TryGetValue(countOfEntities, out var result);
            if (result != null) return result;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("VALUES (");
            int countOfParamaters = countOfEntities * (RepositoryInfo.CountOfColumnsWithoutPrimaryKey);

            int indexOfNextParameter = 0;

            while (indexOfNextParameter < countOfParamaters)
            {
                stringBuilder.Append(GetParameretName(indexOfNextParameter++));
                if (indexOfNextParameter % RepositoryInfo.CountOfColumnsWithoutPrimaryKey == 0
                    && indexOfNextParameter != countOfParamaters)
                {
                    stringBuilder.Append("), (");
                    continue;
                }
                if (indexOfNextParameter != countOfParamaters)
                {
                    stringBuilder.Append(", ");
                }
            }
            stringBuilder.Append(")");

            result = stringBuilder.ToString();
            ValuesStrings.TryAdd(countOfEntities, result);

            return result;
        }

        protected string GetAllColumnsWithKey()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            int counOfStep = RepositoryInfo.CountOfColumns - 1;
            int i = 0;
            for (; i < counOfStep; i++)
            {
                stringBuilder.Append(RepositoryInfo.ColumnsInDbNames[i]);
                stringBuilder.Append("], [");
            }
            stringBuilder.Append(RepositoryInfo.ColumnsInDbNames[i]);
            stringBuilder.Append("] ");
            return stringBuilder.ToString();
        }

        protected string GetAllColumns()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("([");
            int counOfStep = RepositoryInfo.CountOfColumns - 1;
            int i = 1;
            for (; i < counOfStep; i++)
            {
                stringBuilder.Append(RepositoryInfo.ColumnsInDbNames[i]);
                stringBuilder.Append("], [");
            }
            stringBuilder.Append(RepositoryInfo.ColumnsInDbNames[i]);
            stringBuilder.Append("]) ");
            return stringBuilder.ToString();
        }

        protected string GetUpdateString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("SET NOCOUNT ON; UPDATE [");
            stringBuilder.Append(RepositoryInfo.TableInDbName);
            stringBuilder.Append("] SET [");

            int counOfStep = RepositoryInfo.CountOfColumns - 1;
            int i = 1;
            for (; i < counOfStep; i++)
            {
                stringBuilder.Append(RepositoryInfo.ColumnsInDbNames[i]);
                stringBuilder.Append("] = ");
                stringBuilder.Append(GetParameretName(i));
                stringBuilder.Append(", [");
            }
            stringBuilder.Append(RepositoryInfo.ColumnsInDbNames[i]);
            stringBuilder.Append("] = ");
            stringBuilder.Append(GetParameretName(i));
            stringBuilder.Append(" WHERE [");
            stringBuilder.Append(RepositoryInfo.PrimaryKeyName);
            stringBuilder.Append("] = ");
            stringBuilder.Append(GetParameretName(0));
            stringBuilder.Append(";");
            return stringBuilder.ToString();
        }

        public static string GetParameretName(int indexOfParameter)
        {
            ParametersNames.TryGetValue(indexOfParameter, out var result);
            if (result == null)
            {
                result = string.Concat("@", indexOfParameter);
                ParametersNames.TryAdd(indexOfParameter, result);
            }

            return result;
        }

        #endregion

    }
}
