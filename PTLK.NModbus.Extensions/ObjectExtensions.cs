﻿using PTLK.NModbus.Extensions.ArrayExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PTLK.NModbus.Extensions
{
    static class ObjectExtensions
    {
        private static readonly MethodInfo? CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof(string)) return true;
            return type.IsValueType & type.IsPrimitive;
        }

        public static object Copy(this object originalObject)
        {
            object? copiedObject = InternalCopy(originalObject, new Dictionary<object, object?>(new ReferenceEqualityComparer()));

            if (copiedObject == null)
            {
                throw new NullReferenceException("Copied object is null.");
            }

            return copiedObject;
        }

        private static object? InternalCopy(object? originalObject, IDictionary<object, object?> visited)
        {
            if (originalObject == null) return null;
            Type typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            if (CloneMethod == null) return null;
            object? cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                Type? arrayType = typeToReflect.GetElementType();
                if (arrayType == null) return null;
                if (IsPrimitive(arrayType) == false)
                {
                    Array? clonedArray = cloneObject as Array;
                    clonedArray?.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray?.GetValue(indices), visited), indices));
                }

            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object originalObject, IDictionary<object, object?> visited, object? cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool>? filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                object? originalFieldValue = fieldInfo.GetValue(originalObject);
                object? clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }

        public static T Copy<T>(this T original) where T : notnull
        {
            return (T)Copy((object)original);
        }

        public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
        {
            List<PropertyInfo> sourceProps = typeof(T).GetProperties().Where(c => c.CanRead).ToList();
            List<PropertyInfo> destProps = typeof(TU).GetProperties().Where(c => c.CanWrite).ToList();

            foreach (PropertyInfo sourceProp in sourceProps)
            {
                PropertyInfo? prop = destProps.FirstOrDefault(c => c.Name == sourceProp.Name);
                prop?.SetValue(dest, sourceProp.GetValue(source, null), null);
            }
        }
    }

    class ReferenceEqualityComparer : EqualityComparer<object>
    {
        public override bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }
        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        static class ArrayExtensions
        {
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new(array);
                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        class ArrayTraverse
        {
            public int[] Position;
            private readonly int[] maxLengths;

            public ArrayTraverse(Array array)
            {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i)
                {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            public bool Step()
            {
                for (int i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] < maxLengths[i])
                    {
                        Position[i]++;
                        for (int j = 0; j < i; j++)
                        {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }
}