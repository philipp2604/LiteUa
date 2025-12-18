using LiteUa.BuiltIn;

namespace LiteUa.Stack.Method
{
    /// TODO: fix documentation comments

    [AttributeUsage(AttributeTargets.Property)]
    public class OpcMethodParameterAttribute(int order, BuiltInType type) : System.Attribute
    {
        public int Order { get; } = order;
        public BuiltInType Type { get; } = type;
    }
}