namespace LiteUa.Stack.Attribute
{
    /// <summary>
    /// Specifies which timestamps to return with a read operation.
    /// </summary>
    public enum TimestampsToReturn : uint
    {
        Source = 0,
        Server = 1,
        Both = 2,
        Neither = 3
    }
}