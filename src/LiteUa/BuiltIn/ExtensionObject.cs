using LiteUa.Encoding;

namespace LiteUa.BuiltIn
{
    /// TODO: Add unit tests
    /// TODI: Add ToString() method

    /// <summary>
    /// Represents an ExtensionObject in OPC UA, which can encapsulate complex data types.
    /// </summary>
    public class ExtensionObject
    {
        /// <summary>
        /// The <see cref="NodeId"> that identifies the type of the encoded object.
        /// </summary>
        public NodeId TypeId { get; set; } = new NodeId(0); // Default: Null NodeId

        /// <summary>
        /// Gets or sets the encoding type of the ExtensionObject.
        /// </summary>
        public byte Encoding { get; set; } = 0x00; // 0x00 = No Body, 0x01 = ByteString, 0x02 = Xml

        /// <summary>
        /// Gets or sets the raw byte array representing the body of the ExtensionObject as fallback.
        /// </summary>
        public byte[]? Body { get; set; }

        /// <summary>
        /// Gets or sets the decoded value of the ExtensionObject as a typed object.
        /// </summary>
        public object? DecodedValue { get; set; }

        /// <summary>
        /// Encodes the ExtensionObject using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        /// <exception cref="Exception"></exception>
        public void Encode(OpcUaBinaryWriter writer)
        {
            // Case 1: typed object
            if (DecodedValue != null)
            {
                // check if encoder is registered
                if (CustomUaTypeRegistry.TryGetEncoder(DecodedValue.GetType(), out var encoder, out var encodingId))
                {
                    if (encodingId == null)
                        throw new InvalidOperationException($"No encoding Id found for the given type: {DecodedValue.GetType()}");
                    if (encoder == null)
                        throw new InvalidOperationException($"No encoder found for the given type: {DecodedValue.GetType()}");

                    // 1. Encode type id
                    encodingId.Encode(writer);

                    // 2. Encoding Mask (0x01 = ByteString Body)
                    writer.WriteByte(0x01);

                    // 3. Body
                    using var ms = new System.IO.MemoryStream();
                    var tempWriter = new OpcUaBinaryWriter(ms);
                    encoder(DecodedValue, tempWriter);
                    byte[] bodyBytes = ms.ToArray();

                    writer.WriteInt32(bodyBytes.Length);
                    writer.WriteBytes(bodyBytes);
                    return;
                }
                else
                {
                    throw new InvalidOperationException($"No encoder found for the given type: {DecodedValue.GetType()}");
                }
            }

            // Case 2: raw Body bytes or empty ExtensionObject
            // 1. TypeId
            if (TypeId == null) new NodeId(0).Encode(writer);
            else TypeId.Encode(writer);

            // 2. Encoding Mask
            writer.WriteByte(Encoding);

            // 3. Body
            if (Encoding == 0x01 || Encoding == 0x02)
            {
                if (Body == null)
                {
                    writer.WriteInt32(-1);
                }
                else
                {
                    writer.WriteInt32(Body.Length);
                    writer.WriteBytes(Body);
                }
            }
        }

        /// <summary>
        /// Returns an ExtensionObject instance that represents a null or empty ExtensionObject.
        /// </summary>
        public static readonly ExtensionObject Null = new();

        /// <summary>
        /// Decodes an ExtensionObject using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="ExtensionObject"/>.</returns>
        public static ExtensionObject Decode(OpcUaBinaryReader reader)
        {
            var result = new ExtensionObject
            {
                // 1. TypeId
                TypeId = NodeId.Decode(reader),

                // 2. Encoding Mask
                Encoding = reader.ReadByte()
            };

            // 3. Body
            if (result.Encoding == 0x01) // ByteString / Binary
            {
                // Body length
                int length = reader.ReadInt32();

                if (length > 0)
                {
                    result.Body = reader.ReadBytes(length);

                    // 4. Check Registry
                    if (CustomUaTypeRegistry.TryGetDecoder(result.TypeId, out var decoder))
                    {
                        if (decoder != null)
                        {
                            using var bodyStream = new System.IO.MemoryStream(result.Body);
                            var bodyReader = new OpcUaBinaryReader(bodyStream);
                            try
                            {
                                result.DecodedValue = decoder(bodyReader);
                            }
                            catch (Exception)
                            {
                                // parsing failed -> DecodedValue remains null, Body contains bytes.
                            }
                        }
                    }
                }
                else if (length == 0)
                {
                    result.Body = [];
                }
            }
            else if (result.Encoding == 0x02) // XML
            {
                result.Body = reader.ReadByteString();
            }

            return result;
        }
    }
}