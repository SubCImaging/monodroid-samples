using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SubCTools.Helpers
{
    public static class Properties
    {
        public static bool ContainsAttribute<T>(this PropertyInfo property) =>
            property.GetCustomAttributes(typeof(T), true).Count() > 0;

        public static PropertyInfo GetProperty(object sender, string propertyName) => sender.GetType().GetProperty(propertyName);
    }
}
