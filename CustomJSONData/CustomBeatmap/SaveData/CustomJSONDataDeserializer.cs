using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using JetBrains.Annotations;

namespace CustomJSONData.CustomBeatmap
{
    public class CustomJSONDataDeserializer
    {
        private static readonly object _deserializerLock = new();
        private static CustomJSONDataDeserializer[] _deserializers = Array.Empty<CustomJSONDataDeserializer>();

        private readonly Dictionary<string, MethodInfo> _methods = new();
        private bool _enabled = true;

#pragma warning disable 8618
        private CustomJSONDataDeserializer(Type type)
#pragma warning restore 8618
        {
            foreach (MethodInfo method in type.GetMethods(AccessTools.allDeclared))
            {
                JSONDeserializer attribute = method.GetCustomAttribute<JSONDeserializer>();
                if (attribute == null)
                {
                    continue;
                }

                _methods.Add(attribute.PropertyName, method);
            }

            if (_methods.Count == 0)
            {
                throw new ArgumentException($"[{type.FullName}] does not contain a method marked with [{nameof(JSONDeserializer)}].", nameof(type));
            }
        }

        [PublicAPI]
        public bool Enabled
        {
            get => Volatile.Read(ref _enabled);
            set => Volatile.Write(ref _enabled, value);
        }

        [PublicAPI]
        public static CustomJSONDataDeserializer Register<T>()
        {
            CustomJSONDataDeserializer deserializer = new(typeof(T));
            lock (_deserializerLock)
            {
                CustomJSONDataDeserializer[] current = _deserializers;
                CustomJSONDataDeserializer[] updated = new CustomJSONDataDeserializer[current.Length + 1];
                Array.Copy(current, updated, current.Length);
                updated[current.Length] = deserializer;
                Volatile.Write(ref _deserializers, updated);
            }

            return deserializer;
        }

        public static bool Activate(object[] inputs, string field)
        {
            // Snapshot membership so registration cannot invalidate a parser worker's
            // enumeration. Invoke callbacks outside the registration lock.
            CustomJSONDataDeserializer[] deserializers = Volatile.Read(ref _deserializers);
            foreach (CustomJSONDataDeserializer deserializer in deserializers)
            {
                if (!deserializer.Enabled)
                {
                    continue;
                }

                if (deserializer._methods.TryGetValue(field, out MethodInfo method))
                {
                    return (bool)method.Invoke(null, method.ActualParameters(inputs));
                }
            }

            return true;
        }

        [PublicAPI]
        [MeansImplicitUse]
        [AttributeUsage(AttributeTargets.Method)]
        public class JSONDeserializer : Attribute
        {
            public JSONDeserializer(string propertyName)
            {
                PropertyName = propertyName;
            }

            internal string PropertyName { get; }
        }
    }
}
