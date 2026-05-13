using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace HappyHouse.App_Start
{
    // Trims leading/trailing whitespace on all string properties after model binding,
    // but skips trimming entirely for AdminTinTuc -> ThemMoi / SuaThongTin actions.
    public class TrimModelStringsFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Detect controller/action
            var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName ?? "";
            var actionName = filterContext.ActionDescriptor.ActionName ?? "";

            // Skip trimming for add/edit news forms
            if (string.Equals(controllerName, "AdminTinTuc", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(actionName, "ThemMoi", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(actionName, "SuaThongTin", StringComparison.OrdinalIgnoreCase)))
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            // Trim top-level action parameters (including simple string parameters used for search)
            var keys = filterContext.ActionParameters.Keys.ToList();
            foreach (var key in keys)
            {
                var val = filterContext.ActionParameters[key];

                // 1) If parameter is a plain string, trim and set back
                if (val is string s)
                {
                    var trimmed = s?.Trim();
                    filterContext.ActionParameters[key] = string.IsNullOrEmpty(trimmed) ? null : trimmed;
                    continue;
                }

                // 2) If parameter is a collection of strings (string[] or List<string>), trim elements
                if (val is IEnumerable enumerable && !(val is string))
                {
                    var elementType = GetEnumerableElementType(val.GetType());
                    if (elementType == typeof(string))
                    {
                        var trimmedList = ((IEnumerable)val)
                                            .Cast<object>()
                                            .Select(x => (x as string)?.Trim())
                                            .ToList();

                        // Preserve array vs list
                        if (val.GetType().IsArray)
                            filterContext.ActionParameters[key] = trimmedList.Cast<string>().ToArray();
                        else
                            filterContext.ActionParameters[key] = trimmedList;
                        continue;
                    }
                }

                // 3) Complex object — trim its string properties recursively
                TrimRecursively(val, new HashSet<object>());
            }

            base.OnActionExecuting(filterContext);
        }

        private Type GetEnumerableElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            var gen = type.GetInterfaces()
                          .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                          .Select(i => i.GetGenericArguments()[0])
                          .FirstOrDefault();
            return gen;
        }

        private void TrimRecursively(object obj, HashSet<object> visited)
        {
            if (obj == null) return;
            if (visited.Contains(obj)) return;
            visited.Add(obj);

            var type = obj.GetType();

            // Handle collections
            if (obj is IEnumerable enumerable && !(obj is string))
            {
                foreach (var item in enumerable)
                {
                    TrimRecursively(item, visited);
                }
                return;
            }

            // Only inspect complex objects (skip primitives)
            if (type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(DateTime))
                return;

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanRead && p.CanWrite);

            foreach (var prop in props)
            {
                // Skip indexers
                if (prop.GetIndexParameters().Length > 0) continue;

                if (prop.PropertyType == typeof(string))
                {
                    var val = (string)prop.GetValue(obj);
                    if (val != null)
                    {
                        var trimmed = val.Trim();
                        prop.SetValue(obj, string.IsNullOrEmpty(trimmed) ? null : trimmed);
                    }
                }
                else if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof(string))
                {
                    var child = prop.GetValue(obj);
                    if (child != null)
                        TrimRecursively(child, visited);
                }
            }
        }
    }
}