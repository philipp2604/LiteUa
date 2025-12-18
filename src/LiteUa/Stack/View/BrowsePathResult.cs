using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.View
{
    public class BrowsePathResult
    {
        /// TODO: Add unit tests
        /// TODO: fix documentation comments
        /// TODO: Add ToString() method
        public StatusCode StatusCode { get; set; }

        public BrowsePathTarget[]? Targets { get; set; }

        public static BrowsePathResult Decode(OpcUaBinaryReader reader)
        {
            var res = new BrowsePathResult
            {
                StatusCode = StatusCode.Decode(reader)
            };

            int count = reader.ReadInt32();
            if (count > 0)
            {
                res.Targets = new BrowsePathTarget[count];
                for (int i = 0; i < count; i++) res.Targets[i] = BrowsePathTarget.Decode(reader);
            }
            return res;
        }
    }
}