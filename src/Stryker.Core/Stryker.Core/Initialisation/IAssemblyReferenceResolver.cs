using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Stryker.Core.Initialisation
{
    public interface IAssemblyReferenceResolver
    {
        IEnumerable<PortableExecutableReference> ResolveReferences(string projectFile);
        IEnumerable<string> GetAssemblyPathsFromOutput(string paths);
        IEnumerable<string> GetReferencePathsFromOutput(IEnumerable<string> paths);
    }
}