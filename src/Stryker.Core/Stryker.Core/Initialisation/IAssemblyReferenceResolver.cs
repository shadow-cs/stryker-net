using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Stryker.Core.Initialisation
{
    public interface IAssemblyReferenceResolver
    {
        IEnumerable<PortableExecutableReference> ResolveReferences(string projectPath, string projectFileName, string projectUnderTestAssemblyName, IEnumerable<string> fullFrameworkCsProjes);
        IEnumerable<string> GetAssemblyPathsFromOutput(string paths);
        IEnumerable<string> GetReferencePathsFromOutput(IEnumerable<string> paths);
    }
}