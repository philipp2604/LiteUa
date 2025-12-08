using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.BuiltIn
{
    /// TODO: Add unit tests

    /// <summary>
    /// Represents a QualifiedName in OPC UA, consisting of a NamespaceIndex and a Name.
    /// </summary>
    public class QualifiedName(ushort ns, string name)
    {
        /// <summary>
        /// Gets or sets the NamespaceIndex of the QualifiedName.
        /// </summary>
        public ushort NamespaceIndex { get; set; } = ns;

        /// <summary>
        /// Gets or sets the Name of the QualifiedName.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// Encodes the QualifiedName using the provided OpcUaBinaryWriter.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteUInt16(NamespaceIndex);
            writer.WriteString(Name);
        }

        /// <summary>
        /// Decodes the QualifiedName using the provided OpcUaBinaryReader.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        public static QualifiedName Decode(OpcUaBinaryReader reader)
        {
            return new QualifiedName(reader.ReadUInt16(), reader.ReadString() ?? throw new InvalidDataException("Name must not be null."));
        }

        public override string ToString()
        {
            return $"{NamespaceIndex}:{Name}";
        }
    }
}
