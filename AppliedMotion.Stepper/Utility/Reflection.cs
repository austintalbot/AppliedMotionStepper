using System.Collections.Generic;
using System.Linq;

namespace AppliedMotion.Stepper.Utility
{
    internal class Reflection
    {
        #region Methods

        internal static List<string> ReflectTrueBoolPropertiesToList<T>(T obj)
        {
            List<System.Reflection.PropertyInfo> propInfo = obj.GetType().GetProperties().Where(p => p.PropertyType == typeof(bool)).ToList();
            return (from pInfo in propInfo where (bool)pInfo.GetValue(obj) select pInfo.Name).ToList();
        }

        #endregion Methods
    }
}