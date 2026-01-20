using LiteUa.BuiltIn;

namespace LiteUa.Encoding
{
    /// <summary>
    /// Registry for custom OPC UA data type encoders and decoders.
    /// </summary>
    public static class CustomUaTypeRegistry
    {
        private static readonly Lock _lock = new();
        private static readonly Dictionary<NodeId, Func<OpcUaBinaryReader, object>> _decoders = [];

        private static readonly Dictionary<Type, Action<object, OpcUaBinaryWriter>> _encoders = [];

        private static readonly Dictionary<Type, NodeId> _typeIds = [];

        /// <summary>
        /// Registers a custom OPC UA data type with its encoder and decoder.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="encodingId">The encoding Id of the data type.</param>
        /// <param name="decoder">The decoding function.</param>
        /// <param name="encoder">The encoding function.</param>
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

        /// <summary>
        /// Tries to get the decoder function for the specified encoding Id.
        /// </summary>
        /// <param name="encodingId">The encoding id of the custom data type.</param>
        /// <param name="decoder">The registered decoder, if found.</param>
        /// <returns>A bool indicating whether the decoder was found.</returns>
        public static bool TryGetDecoder(NodeId encodingId, out Func<OpcUaBinaryReader, object>? decoder)
        {
            lock (_lock)
            {
                return _decoders.TryGetValue(encodingId, out decoder);
            }
        }

        /// <summary>
        /// Tries to get the encoder function and encoding Id for the specified type.
        /// </summary>
        /// <param name="type">The data type to get the encoding function and encoding id for.</param>
        /// <param name="encoder">The encoding function, if found.</param>
        /// <param name="encodingId">The encoding id, if found.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Unregisters the custom data type of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Data type to unregister.</typeparam>
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

        /// <summary>
        /// Clears all registered custom data types.
        /// </summary>
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