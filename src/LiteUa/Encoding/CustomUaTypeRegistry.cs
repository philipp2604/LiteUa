using LiteUa.BuiltIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// TODO: Fix documentation comments
/// TODO: Add unit tests

namespace LiteUa.Encoding
{
    public static class CustomUaTypeRegistry
    {
#if NET9_0_OR_GREATER
        private static readonly System.Threading.Lock _lock = new();
#else
        private static readonly object _lock = new();
#endif
        private static readonly Dictionary<NodeId, Func<OpcUaBinaryReader, object>> _decoders = [];

        private static readonly Dictionary<Type, Action<object, OpcUaBinaryWriter>> _encoders = [];

        private static readonly Dictionary<Type, NodeId> _typeIds = [];

        public static void Register<T>(NodeId encodingId, Func<OpcUaBinaryReader, T> decoder, Action<T, OpcUaBinaryWriter> encoder)
        {
            lock (_lock)
            {
                if (!_decoders.ContainsKey(encodingId))
                    _decoders[encodingId] = reader => decoder(reader)!;

                var type = typeof(T);
                if (!_encoders.ContainsKey(type))
                {
                    _encoders[type] = (obj, writer) => encoder((T)obj, writer);
                    _typeIds[type] = encodingId;
                }
            }
        }

        public static bool TryGetDecoder(NodeId encodingId, out Func<OpcUaBinaryReader, object>? decoder)
        {
            lock (_lock)
            {
                return _decoders.TryGetValue(encodingId, out decoder);
            }
        }

        public static bool TryGetEncoder(Type type, out Action<object, OpcUaBinaryWriter>? encoder, out NodeId? encodingId)
        {
            lock (_lock)
            {
                encodingId = null;

                if (_encoders.TryGetValue(type, out encoder) && _typeIds.TryGetValue(type, out encodingId))
                {
                    return true;
                }
                return false;
            }
        }

        public static void Unregister<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_typeIds.TryGetValue(type, out var encodingId))
                {
                    _typeIds.Remove(type);
                    _encoders.Remove(type);
                    _decoders.Remove(encodingId);
                }
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _decoders.Clear();
                _encoders.Clear();
                _typeIds.Clear();
            }
        }
    }
}
