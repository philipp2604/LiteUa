namespace LiteUa.Stack.SecureChannel
{
    /// <summary>
    /// Represents the security mode for messages in OPC UA.
    /// </summary>
    public enum MessageSecurityMode : int
    {
        Invalid = 0,
        None = 1,
        Sign = 2,
        SignAndEncrypt = 3
    }
}