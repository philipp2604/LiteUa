using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.BuiltIn
{

    /// TODO: Add unit tests
    /// TODI: Add ToString() method
    
    /// <summary>
    /// Represents a DataValue in OPC UA, which includes a value, status code, and timestamps.
    /// </summary>
    public class DataValue
    {
        /// <summary>
        /// Gets or sets the value of type <see cref="Variant"/> represented by this instance.
        /// </summary>
        public Variant? Value { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="StatusCode"> of this DataValue.
        /// </summary>
        public StatusCode StatusCode { get; set; } = new StatusCode(0); // 0 = Good

        /// <summary>
        /// Gets or sets the source timestamp of this DataValue.
        /// </summary>
        public DateTime SourceTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the server timestamp of this DataValue.
        /// </summary>
        public DateTime ServerTimestamp { get; set; }

        public static DataValue Decode(OpcUaBinaryReader reader)
        {
            var dv = new DataValue();

            // Encoding Mask Byte (Spec Part 6, 5.2.2.17)
            // Bit 0: Value present
            // Bit 1: StatusCode present
            // Bit 2: SourceTimestamp present
            // Bit 3: ServerTimestamp present
            // ... (PicoSeconds etc. )

            ///TODO: PicoSeconds (Bits 4 and 5)

            byte mask = reader.ReadByte();
            
            if ((mask & 0x01) != 0) dv.Value = Variant.Decode(reader);
            if ((mask & 0x02) != 0) dv.StatusCode = StatusCode.Decode(reader);
            if ((mask & 0x04) != 0) dv.SourceTimestamp = reader.ReadDateTime();
            // SourcePicoSeconds (Bit 4) -> UInt16
            if ((mask & 0x10) != 0) reader.ReadUInt16();

            if ((mask & 0x08) != 0) dv.ServerTimestamp = reader.ReadDateTime();
            // ServerPicoSeconds (Bit 5) -> UInt16
            if ((mask & 0x20) != 0) reader.ReadUInt16();

            return dv;
        }

        public void Encode(OpcUaBinaryWriter writer)
        {
            byte mask = 0;
            if (Value != null) mask |= 0x01;
            if (StatusCode.Code != 0) mask |= 0x02;

            if (SourceTimestamp != DateTime.MinValue) mask |= 0x04;
            writer.WriteByte(mask);

            Value?.Encode(writer);
            if ((mask & 0x02) != 0) StatusCode.Encode(writer);
            if ((mask & 0x04) != 0) writer.WriteDateTime(SourceTimestamp);
        }
    }
}
