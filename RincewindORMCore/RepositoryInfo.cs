using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace RincewindORMCore
{
    internal class RepositoryInfo<TEntity> : IRepositoryInfo
    {
        public int MaxCountOfParametersInQuery { get; protected set; }

        public string TableInDbName { get; protected set; }

        public List<string> ColumnsInDbNames { get; protected set; }

        public int CountOfColumns { get; protected set; }

        public int CountOfColumnsWithoutPrimaryKey { get; protected set; }

        public int MaxCountOfEntitiesInQuery { get; protected set; }

        public int MaxCountOfEntitiesOneQueryWithoutPrimaryKey { get; protected set; }

        public string PrimaryKeyName { get; protected set; }

        public ConcurrentDictionary<string, PropertyInfo> PropertiesInfo { get; protected set; }

        public ConcurrentDictionary<string, PropertySetGet> PropertiesSetGet { get; protected set; }

        public Type TypeOfEntity { get; protected set; }

        public RepositoryInfo(int maxCountOfParametersInQuery)
        {
            ColumnsInDbNames = new List<string>();

            PropertiesInfo = new ConcurrentDictionary<string, PropertyInfo>();

            PropertiesSetGet = new ConcurrentDictionary<string, PropertySetGet>();

            MaxCountOfParametersInQuery = maxCountOfParametersInQuery;

            TypeOfEntity = typeof(TEntity);

            SetTableName();

            SetPropertiesInfo();

            CountOfColumns = PropertiesInfo.Count;

            SetPrimaryKeyNameToFirstPosition();

            CountOfColumnsWithoutPrimaryKey = CountOfColumns - 1;

            MaxCountOfEntitiesOneQueryWithoutPrimaryKey = MaxCountOfParametersInQuery / CountOfColumnsWithoutPrimaryKey - CountOfColumns;

            if (MaxCountOfEntitiesOneQueryWithoutPrimaryKey > 1000) MaxCountOfEntitiesOneQueryWithoutPrimaryKey = 1000;

            MaxCountOfEntitiesInQuery = MaxCountOfParametersInQuery / CountOfColumns - CountOfColumns;

            if (MaxCountOfEntitiesInQuery > 1000) MaxCountOfEntitiesInQuery = 1000;
        }

        private void SetTableName()
        {
            if (TypeOfEntity.IsDefined(typeof(TableAttribute), false))
            {
                var tableAttribute =
                    Attribute.GetCustomAttribute(TypeOfEntity, typeof(TableAttribute)) as TableAttribute;

                TableInDbName = tableAttribute.Name;
            }
            else
            {
                TableInDbName = TypeOfEntity.Name;
            }
        }

        private void SetPropertiesInfo()
        {
            var properties = TypeOfEntity.GetProperties();

            var entityHaveKeyAttribute = false;

            foreach (var property in properties)
            {
                string columnName;

                if (property.IsDefined(typeof(ColumnAttribute), false))
                {
                    var columnAttributeValue = Attribute.GetCustomAttribute(property, typeof(ColumnAttribute)) as ColumnAttribute;

                    if (PropertiesInfo.ContainsKey(columnAttributeValue.Name))
                    {
                        throw new Exception("The '" + TypeOfEntity.Name + "' has two columns with name " + columnAttributeValue.Name);
                    }

                    columnName = columnAttributeValue.Name;
                }
                else
                {
                    columnName = property.Name;
                }

                if (property.IsDefined(typeof(KeyAttribute), false))
                {
                    if (entityHaveKeyAttribute)
                        throw new Exception("The '" + TypeOfEntity.Name + "' has two primary keys");

                    entityHaveKeyAttribute = true;
                    PrimaryKeyName = columnName;
                }


                PropertiesInfo.TryAdd(columnName, property);

                PropertiesSetGet.TryAdd(columnName, new PropertySetGet(
                    property.GetGetMethod(),
                    property.GetSetMethod()
                ));


                ColumnsInDbNames.Add(columnName);
            }

            if (!entityHaveKeyAttribute)
            {
                CheckPrimaryKey();
            }
        }

        private void SetPrimaryKeyNameToFirstPosition()
        {
            for (var i = 0; i < CountOfColumns; i++)
            {
                if (ColumnsInDbNames[i] != PrimaryKeyName) continue;

                var buf = ColumnsInDbNames[0];
                ColumnsInDbNames[0] = PrimaryKeyName;
                ColumnsInDbNames[i] = buf;
                break;
            }
        }

        private void CheckPrimaryKey()
        {
            const string tableKey1 = "Id";

            var tableKey2 = string.Format("{0}Id", TableInDbName);

            foreach (var colName in ColumnsInDbNames)
            {
                if (colName != tableKey1 && colName != tableKey2) continue;

                PrimaryKeyName = colName;
                return;
            }

            throw new Exception(string.Format("Type \"{0}\" does not have primary key", TableInDbName));
        }
    }
}
