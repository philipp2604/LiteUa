using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a StatusChangeNotification in the OPC UA protocol.
    /// </summary>
    public class StatusChangeNotification
    {
        /// <summary>
        /// Gets or sets the status code of the notification.
        /// </summary>
        public StatusCode Status { get; set; }

        /// <summary>
        /// Gets or sets the diagnostic information associated with the notification.
        /// </summary>
        public DiagnosticInfo? DiagnosticInfo { get; set; }

        /// <summary>
        /// Decodes a StatusChangeNotification using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded instance of <see cref="StatusChangeNotification"/>.</returns>
        public static StatusChangeNotification Decode(OpcUaBinaryReader reader)
        {
            var scn = new StatusChangeNotification
            {
                Status = StatusCode.Decode(reader),
                DiagnosticInfo = DiagnosticInfo.Decode(reader)
            };
            return scn;
        }
    }
}