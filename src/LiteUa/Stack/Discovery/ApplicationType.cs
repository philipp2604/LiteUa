namespace LiteUa.Stack.Discovery
{
    /// <summary>
    /// Represents the type of an application in OPC UA.
    /// </summary>
    public enum ApplicationType : int
    {
        Server = 0,
        Client = 1,
        ClientAndServer = 2,
        DiscoveryServer = 3
    }
}