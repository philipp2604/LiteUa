using LiteUa.BuiltIn;

namespace LiteUa.Stack.Method
{
    /// <summary>
    /// A custom attribute to specify OPC UA method parameter metadata.
    /// </summary>
    /// <param name="order">The argument's order.</param>
    /// <param name="type">The argument's type.</param>

    [AttributeUsage(AttributeTargets.Property)]
    public class OpcMethodParameterAttribute(int order, BuiltInType type) : System.Attribute
    {
        public int Order { get; } = order;
        public BuiltInType Type { get; } = type;
    }
}