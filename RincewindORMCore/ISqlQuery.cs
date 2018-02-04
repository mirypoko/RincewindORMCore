using System.Collections.Generic;

namespace RincewindORMCore
{
    public interface ISqlQuery
    {
        TypeOfQuery TypeOfQuery { get; }
        string GetQueryText { get; }
        QueriesBuilder QueriesBuilder { get; }
        List<object> Parameters { get; }
        int CountOfRecords { get; }
        int MaxCountOfRecords { get; }
        void AddRecord(object recordObject);
    }
}