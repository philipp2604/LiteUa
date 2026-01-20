namespace LiteUa.Stack.SecureChannel
{
    /// <summary>
    /// Represents the type of security token request in OPC UA.
    /// </summary>
    public enum SecurityTokenRequestType : int
    {
        Issue = 0,
        Renew = 1
    }
}