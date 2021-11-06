using ClosedXML.Excel;
//using DocumentFormat.OpenXml;
using MemberListView.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
#if NET5_0_OR_GREATER
using Umbraco.Cms.Core.Models;
#else
using Umbraco.Core;
using Umbraco.Core.Models;
#endif

namespace MemberListView.Extensions
{
    public static class ExportExtension
    {
        public static async System.Threading.Tasks.Task CreateCSVAsync(this IEnumerable<object> list, Stream stream, bool includeHeaders = true, char fieldSeparator = ',', char stringDelimiter = '"')
        {
            var rows = new List<Dictionary<string, object>>();
            var fileColumns = new List<string>();

            // Build up a collection of rows, adding empty fields where necessary to keep the data consistent.
            foreach (var line in list.ToDictionaryList<object>())
            {
                var row = new Dictionary<string, object>();
                // Add fields for the columns we already know about.
                foreach (var column in fileColumns)
                {
                    row.Add(column, line.Keys.Contains(column) ? line[column] : string.Empty);
                }

                // Only add fields we haven't already added.
                foreach (var key in line.Keys.Where(k => !row.Keys.Contains(k)))
                {
                    if (!fileColumns.Contains(key))
                    {
                        fileColumns.Add(key);
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

            // Can't use using because it will dispose of the stream!
            var sw = new StreamWriter(stream, Encoding.UTF8);
            if (includeHeaders)
            {
                await sw.WriteAsync($"{fileColumns.Aggregate("", (a, b) => (string.IsNullOrEmpty(a) ? a : a + fieldSeparator) + b)}{Environment.NewLine}");
            }
            foreach (var row in rows)
            {
                await sw.WriteAsync($"{row.Aggregate("", (a, b) => string.IsNullOrEmpty(a) ? b.GetValue(stringDelimiter) : $"{a}{fieldSeparator}{b.GetValue(stringDelimiter)}")}{Environment.NewLine}");
            }

            // We can't close the stream, so we need to flush it.
            sw.Flush();
        }

        internal static string GetValue(this KeyValuePair<string, object> value, char delimiter)
        {
            if (value.Value is string)
            {
                return $"{delimiter}{value.Value}{delimiter}";
            }
            return $"{value.Value}";
        }

        public static async System.Threading.Tasks.Task CreateExcelAsync(this IEnumerable<object> list, Stream stream, string sheetName, bool includeHeaders = true)
        {
            //Creating the workbook
            var t = System.Threading.Tasks.Task.Run(() =>
            {
                list.CreateExcel(stream, sheetName, includeHeaders);
            });

            await t;
        }

        public static void CreateExcel(this IEnumerable<object> list, Stream stream, string sheetName, bool includeHeaders = true)
        {
            var workBook = new XLWorkbook();
            var sheet = workBook.AddWorksheet(sheetName);

            int rowIndex = includeHeaders ? 2 : 1;
            int headerRowIndex = 1;
            var columnPositions = new Dictionary<string, int>();
            foreach (var line in list.ToDictionaryList<object>())
            {
                foreach (var column in line.Keys)
                {
                    // If the columns doesn't already exist in the dataset, then add it.
                    if (!columnPositions.ContainsKey(column))
                    {
                        columnPositions.Add(column, columnPositions.Count + 1);

                        if (includeHeaders)
                        {
                            sheet.Cell(headerRowIndex, columnPositions[column]).Value = column;
                        }
                    }

                    var cellValue = line[column];
                    var strValue = cellValue?.ToString();

                    // Attempt to get the type.
                    if (cellValue is DateTime || cellValue is bool || cellValue.IsNumber())
                    {
                        sheet.Cell(rowIndex, columnPositions[column]).Value = cellValue;
                    }
                    else if (DateTime.TryParse(strValue, out DateTime dateValue))
                    {
                        sheet.Cell(rowIndex, columnPositions[column]).Value = dateValue;
                    }
                    else if (bool.TryParse(strValue, out bool boolValue))
                    {
                        sheet.Cell(rowIndex, columnPositions[column]).Value = boolValue;
                    }
                    else if (decimal.TryParse(strValue, out decimal decValue))
                    {
                        // Try to convert from string.
                        if (strValue.StartsWith("0"))
                        {
                            // Add an apostrophe so excel doesn't do away with any leading zeros...
                            sheet.Cell(rowIndex, columnPositions[column]).Value = $"'{strValue}";
                        }
                        else
                        {
                            sheet.Cell(rowIndex, columnPositions[column]).Value = decValue;
                        }
                    }
                    else
                    {
                        sheet.Cell(rowIndex, columnPositions[column]).Value = strValue;
                    }

                }
                rowIndex++;
            }

            if (includeHeaders)
            {
                var rngTable = sheet.Range($"A1:{GetColumnId(columnPositions.Count)}{rowIndex}");
                rngTable.FirstRow().Style
                        .Font.SetBold()
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
            workBook.SaveAs(stream, new SaveOptions { ValidatePackage = true });
        }

        private static string GetColumnId(int position)
        {
            string id = "";
            int asciiStart = 'A';
            int asciiEnd = 'Z';
            while (asciiStart + position > asciiEnd)
            {
                id += (char)asciiStart;
                position -= (asciiEnd - asciiStart + 1);
            }
            id += (char)(asciiStart + position);

            return id;
        }

        private static bool IsNumber(this object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }

        /// <summary>
        /// Turns an object into a dictionary of it's properties
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static IDictionary<string, TVal> ToDictionary<TVal>(this object o)
        {
            if (o != null)
            {
                var d = new Dictionary<string, TVal>();

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
                                    d.Add($"{prop.Name}_{key}", (TVal)dict[key]);
                                else
                                    d.Add(key.ToString(), (TVal)dict[key]);
                            }
                        }
                        else if (!(val is string) && IsEnumerable(val.GetType()))
                        {
                            int i = 0;
                            foreach (var item in (IEnumerable)val)
                            {
                                d.Add($"{prop.Name}_{i++}", (TVal)item);
                            }
                        }
                        else
                        {
                            d.Add(prop.Name, (TVal)val);
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


        internal static IEnumerable<MemberColumn> GetColumns(this IMemberType type, string[] excludedColumns)
        {
            foreach (var group in type.PropertyGroups.OrderBy(g => g.SortOrder))
            {
                // TODO: Check for sensitive data.
                foreach (var property in group.PropertyTypes
                                            .Where(p => !excludedColumns.Contains(p.Alias))
                                            .OrderBy(p => p.SortOrder)
                                            .Select(p => new MemberColumn { Id = p.Alias, Alias = p.Alias, Name = p.Name }))
                {
                    yield return property;
                }
            }
        }

    }
}