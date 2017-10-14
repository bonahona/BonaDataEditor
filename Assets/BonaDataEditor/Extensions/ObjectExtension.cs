using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BonaDataEditor
{
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class ObjectExtension
    {
        public static T To<T>(this object o)
        {
            return (T)o;
        }

        public static T ToOrDefault<T>(this object o)
        {
            try {
                return (T)o;
            } catch (Exception) {
                return default(T);
            }
        }

        public static bool In<T>(this T item, params T[] items)
        {
            if (items == null) {
                throw new ArgumentNullException();
            }

            return items.Contains(item);
        }

        public static T GetInterface<T>(this UnityEngine.Object o)
        {
            if (o is Component) {
                var component = o.To<Component>();
                return component.GetComponentInterface<T>();
            } else if (o is ScriptableObject) {
                var scriptableObject = o.To<ScriptableObject>();
                return scriptableObject.GetScriptableObjectInterface<T>();
            } else {
                return default(T);
            }
        }

        public static List<T> GetInterfaces<T>(this UnityEngine.Object o)
        {
            if (o is Component) {
                var component = o.To<Component>();
                return component.GetComponentInterfaces<T>();
            } else if (o is ScriptableObject) {
                var scriptableObject = o.To<ScriptableObject>();
                return scriptableObject.GetScriptableObjectInterfaces<T>();
            } else {
                return new List<T>();
            }
        }

        public static T GetComponentInterface<T>(this UnityEngine.Component o)
        {
            var targetType = typeof(T);
            var components = o.GetComponents<Component>();

            foreach (var component in components) {
                foreach (var objectInterface in component.GetType().GetInterfaces()) {
                    if (objectInterface == targetType) {
                        return component.To<T>();
                    }
                }
            }

            return default(T);
        }

        public static List<T> GetComponentInterfaces<T>(this UnityEngine.Component o)
        {
            var result = new List<T>();
            var targetType = typeof(T);
            var components = o.GetComponents<Component>();

            foreach (var component in components) {
                foreach (var objectInterface in component.GetType().GetInterfaces()) {
                    if (objectInterface == targetType) {
                        result.Add(component.To<T>());
                    }
                }
            }

            return result;
        }

        public static T GetScriptableObjectInterface<T>(this UnityEngine.ScriptableObject o)
        {
            var targetType = typeof(T);
            var objectType = o.GetType();
            foreach (var objectInterface in objectType.GetInterfaces()) {
                if (objectInterface == targetType) {
                    return o.To<T>();
                }
            }

            return default(T);
        }

        public static List<T> GetScriptableObjectInterfaces<T>(this UnityEngine.ScriptableObject o)
        {
            var result = new List<T>();
            var targetType = typeof(T);
            var objectType = o.GetType();
            foreach (var objectInterface in objectType.GetInterfaces()) {
                if (objectInterface == targetType) {
                    result.Add(o.To<T>());
                }
            }

            return result;
        }

        public static U Transform<T, U>(this T item, Func<T, U> function)
        {
            return function(item);
        }

        public static T Do<T, U>(this T item, Func<T, U> function)
        {
            return item;
        }

        public static bool IsInstanceOf(this UnityEngine.Object o, System.Type type)
        {
            if (o == null) {
                return false;
            }

            return type.IsAssignableFrom(o.GetType());
        }
    }
}
