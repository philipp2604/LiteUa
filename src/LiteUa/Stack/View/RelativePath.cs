using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    public class RelativePath(RelativePathElement[] elements)
    {
        /// TODO: Add unit tests
        /// TODO: fix documentation comments
        /// TODO: Add ToString() method
        public RelativePathElement[] Elements { get; set; } = elements;

        public void Encode(OpcUaBinaryWriter writer)
        {
            if (Elements == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(Elements.Length);
                foreach (var e in Elements) e.Encode(writer);
            }
        }
    }
}