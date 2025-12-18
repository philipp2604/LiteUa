using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Discovery;

namespace LiteUa.Stack.Session
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class ClientDescription
    {
        public string? ApplicationUri { get; set; }
        public string? ProductUri { get; set; }
        public LocalizedText? ApplicationName { get; set; }
        public ApplicationType Type { get; set; }
        public string? GatewayServerUri { get; set; }
        public string? DiscoveryProfileUri { get; set; }
        public string[]? DiscoveryUrls { get; set; }

        public void Encode(OpcUaBinaryWriter writer)
        {
            writer.WriteString(ApplicationUri);
            writer.WriteString(ProductUri);
            if (ApplicationName == null) new LocalizedText().Encode(writer);
            else ApplicationName.Encode(writer);

            writer.WriteInt32((int)Type);
            writer.WriteString(GatewayServerUri);
            writer.WriteString(DiscoveryProfileUri);

            if (DiscoveryUrls == null) writer.WriteInt32(-1);
            else
            {
                writer.WriteInt32(DiscoveryUrls.Length);
                foreach (var url in DiscoveryUrls) writer.WriteString(url);
            }
        }
    }
}