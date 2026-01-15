using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Discovery
{
    /// <summary>
    /// Represents an application description in OPC UA.
    /// </summary>
    public class ApplicationDescription
    {
        /// <summary>
        /// Gets or sets the application URI.
        /// </summary>
        public string? ApplicationUri { get; set; }

        /// <summary>
        /// Gets or sets the product URI.
        /// </summary>
        public string? ProductUri { get; set; }

        /// <summary>
        /// Gets or sets the localized application name.
        /// </summary>
        public LocalizedText? ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the application type.
        /// </summary>
        public ApplicationType? Type { get; set; }

        /// <summary>
        /// Gets or sets the gateway server URI.
        /// </summary>
        public string? GatewayServerUri { get; set; }

        /// <summary>
        /// Gets or sets the discovery profile URI.
        /// </summary>
        public string? DiscoveryProfileUri { get; set; }

        /// <summary>
        /// Gets or sets the discovery URLs.
        /// </summary>
        public string?[]? DiscoveryUrls { get; set; }

        /// <summary>
        /// Decodes an ApplicationDescription using a <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use.</param>
        /// <returns>The decoded ApplicationDescription instance.</returns>
        public static ApplicationDescription Decode(OpcUaBinaryReader reader)
        {
            var app = new ApplicationDescription
            {
                ApplicationUri = reader.ReadString(),
                ProductUri = reader.ReadString(),
                ApplicationName = LocalizedText.Decode(reader),
                Type = (ApplicationType)reader.ReadInt32(),
                GatewayServerUri = reader.ReadString(),
                DiscoveryProfileUri = reader.ReadString()
            };

            int count = reader.ReadInt32();
            if (count > 0)
            {
                app.DiscoveryUrls = new string[count];
                for (int i = 0; i < count; i++) app.DiscoveryUrls[i] = reader.ReadString();
            }
            else
            {
                app.DiscoveryUrls = [];
            }
            return app;
        }
    }
}