using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace RincewindORMCore
{
    public interface IRepositoryInfo
    {
        int MaxCountOfParametersInQuery { get; }
        string TableInDbName { get; }
        List<string> ColumnsInDbNames { get; }
        int CountOfColumns { get; }
        int CountOfColumnsWithoutPrimaryKey { get; }
        int MaxCountOfEntitiesInQuery { get; }
        int MaxCountOfEntitiesOneQueryWithoutPrimaryKey { get; }
        string PrimaryKeyName { get; }
        ConcurrentDictionary<string, PropertyInfo> PropertiesInfo { get; }
        ConcurrentDictionary<string, PropertySetGet> PropertiesSetGet { get; }
        Type TypeOfEntity { get; }
    }
}