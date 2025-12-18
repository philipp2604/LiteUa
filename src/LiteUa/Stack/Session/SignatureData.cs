using LiteUa.Encoding;

namespace LiteUa.Stack.Session
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class SignatureData
    {
        public string? Algorithm { get; set; }
        public byte[]? Signature { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteString(Algorithm);
            writer.WriteByteString(Signature);
        }

        public static readonly SignatureData Null = new();
    }
}