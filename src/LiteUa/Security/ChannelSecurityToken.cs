using LiteUa.Encoding;

/// TODO: Add unit tests
/// TODI: Add ToString() method

namespace LiteUa.Security
{
    /// <summary>
    /// A class representing a Channel Security Token in OPC UA.
    /// </summary>
    public class ChannelSecurityToken
    {
        /// <summary>
        /// Gets or sets the ChannelId of the ChannelSecurityToken.
        /// </summary>
        public uint ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the TokenId of the ChannelSecurityToken.
        /// </summary>
        public uint TokenId { get; set; }

        /// <summary>
        /// Gets or sets the CreatedAt timestamp of the ChannelSecurityToken.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the RevisedLifetime of the ChannelSecurityToken.
        /// </summary>
        public uint RevisedLifetime { get; set; }

        /// <summary>
        /// Decodes a ChannelSecurityToken using the provided OpcUaBinaryReader.
        /// </summary>
        /// <param name="reader">The <see cref="OpcUaBinaryReader"/> used for encoding.</param>
        /// <returns>The decoded instance of <see cref="ChannelSecurityToken"/>.</returns>
        public static ChannelSecurityToken Decode(OpcUaBinaryReader reader)
        {
            return new ChannelSecurityToken
            {
                ChannelId = reader.ReadUInt32(),
                TokenId = reader.ReadUInt32(),
                CreatedAt = reader.ReadDateTime(),
                RevisedLifetime = reader.ReadUInt32()
            };
        }
    }
}