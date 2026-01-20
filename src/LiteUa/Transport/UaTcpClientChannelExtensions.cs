using LiteUa.BuiltIn;
using LiteUa.Stack.Method;
using System.Reflection;

namespace LiteUa.Transport
{
    /// <summary>
    /// Extension methods for IUaTcpClientChannel to support typed method calls.
    /// </summary>
    public static class UaTcpClientChannelExtensions
    {
        /// <summary>
        /// Calls an OPC UA method using strongly-typed input and output objects, automatically mapping properties to method arguments.
        /// </summary>
        /// <typeparam name="TInput">The class type representing the input arguments of the method.</typeparam>
        /// <typeparam name="TOutput">The class type representing the output arguments of the method. Must have a parameterless constructor.</typeparam>
        /// <param name="channel">The <see cref="IUaTcpClientChannel"/> used to perform the call.</param>
        /// <param name="objectId">The <see cref="NodeId"/> of the object that contains the method.</param>
        /// <param name="methodId">The <see cref="NodeId"/> of the method to be invoked.</param>
        /// <param name="input">An instance of <typeparamref name="TInput"/> containing the values to be passed as input arguments.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to control the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> resulting in an instance of <typeparamref name="TOutput"/> populated with the method's return values.</returns>
        /// <exception cref="Exception">Thrown if the number of output variants returned by the server does not match the number of properties defined in <typeparamref name="TOutput"/>.</exception>
        public static async Task<TOutput> CallTypedAsync<TInput, TOutput>(
            this IUaTcpClientChannel channel, NodeId objectId, NodeId methodId, TInput input, CancellationToken cancellationToken = default)
            where TInput : class
            where TOutput : class, new()
        {
            // --- INPUT MAPPING ---
            var inputProps = GetOrderedParameters<TInput>();
            var inputVariants = new Variant[inputProps.Count];

            for (int i = 0; i < inputProps.Count; i++)
            {
                var (prop, attr) = inputProps[i];
                var val = prop.GetValue(input);

                inputVariants[i] = MapToVariant(val, attr.Type);
            }

            // --- CALL ---
            var outputVariants = await channel.CallAsync(objectId, methodId, cancellationToken, inputVariants);

            // --- OUTPUT MAPPING ---
            var outputResult = new TOutput();
            var outputProps = GetOrderedParameters<TOutput>();

            if (outputVariants.Length != outputProps.Count)
                throw new Exception("Output count mismatch");

            for (int i = 0; i < outputProps.Count; i++)
            {
                var (prop, _) = outputProps[i];
                var variant = outputVariants[i];

                object? convertedValue = MapFromVariant(variant, prop.PropertyType);
                prop.SetValue(outputResult, convertedValue);
            }

            return outputResult;
        }

        // --- HELPER: C# Object -> Variant ---
        private static Variant MapToVariant(object? value, BuiltInType expectedType)
        {
            if (value == null) return new Variant(null, expectedType);

            Type type = value.GetType();

            // 1. Array Handling
            if (type.IsArray && type != typeof(byte[])) // Byte[] is ByteString (Scalar)
            {
                // Transform Struct Array to ExtensionObject Array
                if (expectedType == BuiltInType.ExtensionObject)
                {
                    var arr = (Array)value;
                    var extObjArr = new ExtensionObject[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                    {
                        extObjArr[i] = new ExtensionObject { DecodedValue = arr.GetValue(i) };
                    }
                    return new Variant(extObjArr, BuiltInType.ExtensionObject);
                }

                // Primitive Array (int[], string[])
                return new Variant(value, expectedType);
            }

            // 2. Struct Handling (Single)
            if (expectedType == BuiltInType.ExtensionObject)
            {
                var extObj = new ExtensionObject { DecodedValue = value };
                return new Variant(extObj, BuiltInType.ExtensionObject);
            }

            // 3. Scalar Primitive
            return new Variant(value, expectedType);
        }

        // --- HELPER: Variant -> C# Object ---
        private static object? MapFromVariant(Variant variant, Type targetType)
        {
            if (variant.Value == null) return null;

            // 1. Array Handling
            if (variant.IsArray)
            {
                var sourceArray = (Array)variant.Value;

                // ExtensionObject Array to Primitive/Struct Array

                if (targetType.IsArray)
                {
                    Type? elementType = targetType.GetElementType() ?? throw new Exception("Target array type has no element type");
                    Array destinationArray = Array.CreateInstance(elementType, sourceArray.Length);

                    for (int i = 0; i < sourceArray.Length; i++)
                    {
                        object? item = sourceArray.GetValue(i);

                        // unpack extensionobject
                        if (item is ExtensionObject ext)
                        {
                            item = ext.DecodedValue;
                        }

                        // Convert / Cast
                        object? convertedItem = ConvertValue(item, elementType);
                        destinationArray.SetValue(convertedItem, i);
                    }
                    return destinationArray;
                }
            }

            // 2. Struct Handling (Single ExtensionObject)
            if (variant.Value is ExtensionObject extObj)
            {
                return extObj.DecodedValue;
            }

            // 3. Scalar Primitive
            return ConvertValue(variant.Value, targetType);
        }

        private static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;
            if (value is IConvertible) return Convert.ChangeType(value, targetType);
            return value;
        }

        private static List<(PropertyInfo, OpcMethodParameterAttribute)> GetOrderedParameters<T>()
        {
            return [.. typeof(T).GetProperties()
                .Select(p => (p, attr: p.GetCustomAttribute<OpcMethodParameterAttribute>()!))
                .Where(x => x.attr != null)
                .OrderBy(x => x.attr?.Order)];
        }
    }
}