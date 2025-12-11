using LiteUa.BuiltIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteUa.Stack.View
{
    public static class BrowsePathParser
    {
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
