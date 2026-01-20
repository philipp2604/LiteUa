using LiteUa.BuiltIn;

namespace LiteUa.Stack.View
{
    /// <summary>
    /// A static parser for converting string representations of browse paths into <see cref="RelativePath"/> objects.
    /// </summary>
    public static class BrowsePathParser
    {
        /// <summary>
        /// Parses a string representation of a relative path into a corresponding <see cref="RelativePath"/> instance.
        /// </summary>
        /// <param name="path">The relative path string to parse. Each element should be separated by a forward slash ('/'). Elements may
        /// optionally specify a namespace index using the format "nsIndex:Name"; if omitted, the namespace index
        /// defaults to 0.</param>
        /// <returns>A <see cref="RelativePath"/> instance representing the parsed path elements. The returned object contains
        /// one element for each valid segment in the input string.</returns>
        public static RelativePath Parse(string path)
        {
            var parts = path.Split('/');
            var elements = new List<RelativePathElement>();

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                // Format: "nsIndex:Name" or "Name" (Default ns=0)
                ushort ns = 0;
                string name = part;

                if (part.Contains(':'))
                {
                    var segments = part.Split(':');
                    if (segments.Length == 2 && ushort.TryParse(segments[0], out ns))
                    {
                        name = segments[1];
                    }
                }

                elements.Add(new RelativePathElement
                {
                    ReferenceTypeId = new NodeId(33), // HierarchicalReferences
                    IsInverse = false,
                    IncludeSubtypes = true,
                    TargetName = new QualifiedName(ns, name)
                });
            }

            return new RelativePath([.. elements]);
        }
    }
}