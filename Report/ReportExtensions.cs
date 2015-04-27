using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Joe.Business.Report.Attributes;

namespace Joe.Business.Report
{
    public static class ReportExtensions
    {
        public static ICollection<T> AddTotals<T>(this ICollection<T> list)
           where T : new()
        {
            var sumProperties = typeof(T).GetProperties().Where(prop => prop.GetCustomAttribute<SumAttribute>() != null);
            var averageProperties = typeof(T).GetProperties().Where(prop => prop.GetCustomAttribute<AverageAttribute>() != null);

            var totalRow = Activator.CreateInstance<T>();
            foreach (var prop in sumProperties)
            {
                var attribute = prop.GetCustomAttribute<SumAttribute>();
                var sum = list.Sum(i =>
                {
                    var value = prop.GetValue(i);

                    if (value != null)
                        return Math.Round(double.Parse(value.ToString()), attribute.Precision);

                    return 0;
                });
                if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                {
                    var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                    prop.SetValue(totalRow, Convert.ChangeType(sum, underlyingType));
                }
                else
                    prop.SetValue(totalRow, Convert.ChangeType(sum, prop.PropertyType));
            }

            foreach (var prop in averageProperties)
            {
                var attribute = prop.GetCustomAttribute<AverageAttribute>();
                var sum = list.Average(i =>
                {
                    var value = prop.GetValue(i);

                    if (attribute.IsPercent)
                        value = value.ToString().Replace("%", String.Empty);

                    if (value != null)
                        return double.Parse(value.ToString());

                    return 0;
                });

                sum = Math.Round(sum, attribute.Precision);
                Object endValue;
                if (attribute.IsPercent)
                    endValue = sum.ToString() + '%';
                else
                    endValue = sum;

                if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                {
                    var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                    prop.SetValue(totalRow, Convert.ChangeType(endValue, underlyingType));
                }
                else
                    prop.SetValue(totalRow, Convert.ChangeType(endValue, prop.PropertyType));
            }

            list.Add(totalRow);
            return list;
        }
    }
}