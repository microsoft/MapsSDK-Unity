// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Encapsulates the property path format that Unity uses for serializing in SerializedProperty.propertyPath.
    /// </summary>
    /// <remarks>
    /// Portions of this code are from Unity Answers, provided by HiddenMonk: http://answers.unity.com/answers/1149219/view.html.
    /// </remarks>
    public static class UnitySerializationPath
    {
        private const string ArrayPrefix = "data[";
        private const string ArraySuffix = "]";

        /// <summary>
        /// Gets the object that contains the property specified by a path.
        /// The property name is not validated as it is not the property that is returned, but the object.
        /// </summary>
        public static object GetNestedObject(string propertyPath, object obj, out string fieldName)
        {
            string[] fieldStructure = propertyPath.Split('.');
            bool inArray = false;
            for (int i = 0; i < fieldStructure.Length - 1; i++)
            {
                if (obj == null)
                {
                    throw new ArgumentException(nameof(propertyPath));
                }

                if (fieldStructure[i] == "Array")
                {
                    // Arrays or lists are represented as "Array.data[0]" in the property path.
                    inArray = true;
                }
                else if (inArray)
                {
                    inArray = false;

                    fieldName = fieldStructure[i];
                    if (fieldName.StartsWith(ArrayPrefix))
                    {
                        var list = (IList)obj;
                        int index = int.Parse(fieldName.Substring(ArrayPrefix.Length, fieldName.Length - ArrayPrefix.Length - ArraySuffix.Length));
                        obj = list[index];
                    }
                    else
                    {
                        throw new ArgumentException(nameof(propertyPath));
                    }
                }
                else
                {
                    obj = GetFieldOrPropertyValue(fieldStructure[i], obj, true);
                }
            }

            fieldName = fieldStructure.Last();
            return obj;
        }

        /// <summary>
        /// Gets a field or property by name from within an object.
        /// </summary>
        public static object GetFieldOrPropertyValue(string fieldName, object obj, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (fieldName.StartsWith(ArrayPrefix))
            {
                var list = (IList)obj;
                int index = int.Parse(fieldName.Substring(ArrayPrefix.Length, fieldName.Length - ArrayPrefix.Length - ArraySuffix.Length));

                if (index >= list.Count)
                {
                    throw new ArgumentException("List is not long enough to hold property. Was SerializedObject.ApplyModifiedProperties called after the list was expanded?");
                }

                return list[index];
            }
            else
            {
                FieldInfo field = obj.GetType().GetField(fieldName, bindings);
                if (field != null)
                {
                    return field.GetValue(obj);
                }

                PropertyInfo property = obj.GetType().GetProperty(fieldName, bindings);
                if (property != null)
                {
                    return property.GetValue(obj, null);
                }

                if (includeAllBases)
                {
                    foreach (Type type in GetBaseClassesAndInterfaces(obj.GetType()))
                    {
                        field = type.GetField(fieldName, bindings);
                        if (field != null)
                        {
                            return field.GetValue(obj);
                        }

                        property = type.GetProperty(fieldName, bindings);
                        if (property != null)
                        {
                            return property.GetValue(obj, null);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all base classes and interfaces that the specified type is derived from.
        /// </summary>
        private static IEnumerable<Type> GetBaseClassesAndInterfaces(this Type type, bool includeSelf = false)
        {
            List<Type> allTypes = new List<Type>();

            if (includeSelf) allTypes.Add(type);

            if (type.BaseType == typeof(object))
            {
                allTypes.AddRange(type.GetInterfaces());
            }
            else
            {
                allTypes.AddRange(
                        Enumerable
                        .Repeat(type.BaseType, 1)
                        .Concat(type.GetInterfaces())
                        .Concat(type.BaseType.GetBaseClassesAndInterfaces())
                        .Distinct());
            }

            return allTypes;
        }
    }
}
