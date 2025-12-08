using LiteUa.BuiltIn;
using LiteUa.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.Discovery
{
    /// TODO: Add unit tests
    /// TODO: fix documentation comments
    /// TODO: Add ToString() method

    public class ApplicationDescription
    {
        public string? ApplicationUri { get; set; }
        public string? ProductUri { get; set; }
        public LocalizedText? ApplicationName { get; set; }
        public ApplicationType? Type { get; set; }
        public string? GatewayServerUri { get; set; }
        public string? DiscoveryProfileUri { get; set; }
        public string?[]? DiscoveryUrls { get; set; }

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
