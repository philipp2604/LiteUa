using LiteUa.BuiltIn;
using LiteUa.Encoding;

namespace LiteUa.Stack.Subscription
{
    /// <summary>
    /// Represents a NotificationMessage used in OPC UA subscriptions.
    /// </summary>
    public class NotificationMessage
    {
        /// <summary>
        /// Gets or sets the sequence number of the notification message.
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets the publish time of the notification message.
        /// </summary>
        public DateTime PublishTime { get; set; }

        /// <summary>
        /// Gets or sets the array of notification data as <see cref="ExtensionObject"/>.
        /// </summary>
        public ExtensionObject[]? NotificationData { get; set; }

        /// <summary>
        /// Decodes a NotificationMessage using the provided <see cref="OpcUaBinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> to use for decoding.</param>
        /// <returns>The decoded <see cref="NotificationMessage"/> instance.</returns>
        public static NotificationMessage Decode(OpcUaBinaryReader reader)
        {
            var msg = new NotificationMessage
            {
                SequenceNumber = reader.ReadUInt32(),
                PublishTime = reader.ReadDateTime()
            };

            int count = reader.ReadInt32();
            if (count > 0)
            {
                msg.NotificationData = new ExtensionObject[count];
                for (int i = 0; i < count; i++) msg.NotificationData[i] = ExtensionObject.Decode(reader);
            }
            return msg;
        }
    }
}