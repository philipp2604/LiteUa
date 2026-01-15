namespace LiteUa.Stack.Session.Identity
{
    /// <summary>
    /// Represents the type of user token used for authentication in OPC UA.
    /// </summary>
    public enum UserTokenType
    {
        Anonymous,
        Username,
        Certificate,
        IssuedToken
    }
}