using Examine;
using MemberListView.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;

namespace MemberListView.Helpers
{
    public static class CSVExport
    {
        public static string CreateCSV(this IEnumerable<object> list, bool includeHeaders = true, char fieldSeparator = ',', char stringDelimiter = '"')
        {
            List<string> columns = new List<string>();
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            // Build up a collection of rows, adding empty fields where necessary to keep the data consistent.
            foreach (var line in list.ToDictionaryList<object>())
            {
                var row = new Dictionary<string, object>();
                if (columns.Any())
                {
                    // Add fields for the columns we already know about.
                    foreach (var column in columns)
                    {
                        row.Add(column, line.Keys.Contains(column) ? line[column] : string.Empty);
                    }
                }

                // Only add fields we haven't already added.
                foreach (var key in line.Keys.Where(k => !row.Keys.Contains(k)))
                {
                    if (!columns.Contains(key))
                    {
                        columns.Add(key);
                        // If there are already rows then we need to add the new field as an empty string.
                        foreach (var r in rows)
                        {
                            r.Add(key, string.Empty);
                        }
                    }
                    row.Add(key, line[key]);
                }
                rows.Add(row);
            }

            StringBuilder export = new StringBuilder();
            if (includeHeaders)
            {
                export.AppendLine(columns.Aggregate("", (a, b) => (a == "" ? a : a + fieldSeparator) + b));
            }
            foreach (var line in rows)
            {
                export.AppendLine(line.Aggregate("", (a, b) => (a == "" ? a : a + fieldSeparator) + ((b.Value is string) ? stringDelimiter.ToString() : "") + b.Value.ToString() + ((b.Value is string) ? stringDelimiter.ToString() : "")));
            }
            return export.ToString();
        }

        /// <summary>
        /// Turns an object into dictionary of it's properties
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static IDictionary<string, TVal> ToDictionary<TVal>(this object o)
        {
            if (o != null)
            {
                var d = new Dictionary<string, TVal>();
                if (o is SearchResult)
                {
                    var sr = o as SearchResult;
                    d.Add("Id", (TVal)(sr.Id as object));
                    foreach (var prop in sr.Fields)
                    {
                        d.Add(prop.Key, (TVal)(prop.Value as object));
                    }
                }
                else
                {
                    var props = TypeDescriptor.GetProperties(o);
                    foreach (var prop in props.Cast<PropertyDescriptor>())
                    {
                        var val = prop.GetValue(o);
                        // Flatten any dictionaries or lists.
                        if (val != null)
                        {
                            if (IsDictionary(val.GetType()))
                            {
                                var dict = (IDictionary)val;
                                foreach (var key in dict.Keys)
                                {
                                    // If the item name is not already defined, we don't need to qualify it.
                                    if (props.Cast<PropertyDescriptor>().Any(p => p.Name == key.ToString()))
                                        d.Add(string.Format("{0}_{1}", prop.Name, key), (TVal)dict[key]);
                                    else
                                        d.Add(key.ToString(), (TVal)dict[key]);
                                }
                            }
                            else if (!(val is string) && IsEnumerable(val.GetType()))
                            {
                                int i = 0;
                                foreach (var item in (IEnumerable)val)
                                {
                                    d.Add(string.Format("{0}_{1}", prop.Name, i++), (TVal)item);
                                }
                            }
                            else
                            {
                                d.Add(prop.Name, (TVal)val);
                            }
                        }
                    }
                }
                return d;
            }
            return new Dictionary<string, TVal>();
        }

        internal static bool IsEnumerable(Type type)
        {
            foreach (Type t in type.GetInterfaces())
            {
                if (t.IsGenericType
                    && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsDictionary(Type type)
        {
            foreach (Type t in type.GetInterfaces())
            {
                if (t.IsGenericType
                    && t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Turns a list of objects into it's dictionary counterpart
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        internal static IEnumerable<IDictionary<string, TVal>> ToDictionaryList<TVal>(this IEnumerable<object> items)
        {
            return items.Select(x => x.ToDictionary<TVal>());
        }


    }
}