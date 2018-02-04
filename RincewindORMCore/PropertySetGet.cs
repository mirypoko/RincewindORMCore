using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RincewindORMCore
{
    public class PropertySetGet
    {
        public MethodInfo Get { get; }

        public MethodInfo Set { get; }

        public PropertySetGet(MethodInfo get, MethodInfo set)
        {
            Get = get;
            Set = set;
        }
    }
}
