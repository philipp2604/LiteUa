using LiteUa.BuiltIn;
using LiteUa.Encoding;
using LiteUa.Stack.Discovery;

namespace LiteUa.Stack.Session
{
    /// <summary>
    /// Represents a description of an OPC UA client.
    /// </summary>
    public class ClientDescription
    {
        /// <summary>
        /// Gets or sets the application URI of the client.
        /// </summary>
        public string? ApplicationUri { get; set; }

        /// <summary>
        /// Gets or sets the product URI of the client.
        /// </summary>
        public string? ProductUri { get; set; }

        /// <summary>
        /// Gets or sets the localized name of the application.
        /// </summary>
        public LocalizedText? ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the type of the application.
        /// </summary>
        public ApplicationType Type { get; set; }

        /// <summary>
        /// Gets or sets the URI of the gateway server, if applicable.
        /// </summary>
        public string? GatewayServerUri { get; set; }

        /// <summary>
        /// Gets or sets the URI of the discovery profile.
        /// </summary>
        public string? DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gets or sets the array of discovery URLs.
        /// </summary>
        public string[]? DiscoveryUrls { get; set; }

        /// <summary>
        /// Encodes the ClientDescription using the provided <see cref="OpcUaBinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="OpcUaBinaryWriter"/> to use for encoding.</param>
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