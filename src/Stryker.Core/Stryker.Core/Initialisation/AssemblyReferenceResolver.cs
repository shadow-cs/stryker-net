using Buildalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Stryker.Core.Logging;
using Stryker.Core.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml.Linq;

namespace Stryker.Core.Initialisation
{
    /// <summary>
    /// Resolving the MetadataReferences for compiling later
    /// This has to be done using msbuild because currently msbuild is the only reliable way of resolving all referenced assembly locations
    /// </summary>
    public class AssemblyReferenceResolver : IAssemblyReferenceResolver
    {
        private IProcessExecutor _processExecutor { get; set; }
        private IMetadataReferenceProvider _metadataReference { get; set; }
        private ILogger _logger { get; set; }

        public AssemblyReferenceResolver(IProcessExecutor processExecutor, IMetadataReferenceProvider metadataReference)
        {
            _processExecutor = processExecutor;
            _metadataReference = metadataReference;
            _logger = ApplicationLogging.LoggerFactory.CreateLogger<AssemblyReferenceResolver>();
        }

        public AssemblyReferenceResolver() : this(new ProcessExecutor(), new MetadataReferenceProvider()) { }

        /// <summary>
        /// Uses Buildalyzer to resolve all references for the given test project
        /// </summary>
        /// <param name="projectFile">The test project file location</param>
        /// <returns>References</returns>
        public IEnumerable<PortableExecutableReference> ResolveReferences(string projectFile)
        {
            AnalyzerManager manager = new AnalyzerManager();
            ProjectAnalyzer analyzer = manager.GetProject(projectFile);
            var analyzerResult = analyzer.Build().First();

            foreach (var path in analyzerResult.References)
            {
                _logger.LogDebug("Resolved depedency {0}", path);
                yield return MetadataReference.CreateFromFile(path);
            }
        }


        /// <summary>
        /// Subtracts all paths from PathSeperator seperated string
        /// </summary>
        /// <returns>All references this project has</returns>
        public IEnumerable<string> GetAssemblyPathsFromOutput(string paths)
        {
            foreach (var path in paths.Split(';'))
            {
                if (Path.GetExtension(path) == ".dll")
                {
                    yield return path;
                }
            }
        }

        /// <summary>
        /// Subtracts all paths from PathSeperator seperated string
        /// </summary>
        /// <returns>All references this project has</returns>
        public IEnumerable<string> GetReferencePathsFromOutput(IEnumerable<string> paths)
        {
            foreach (var pathPrintOutput in paths)
            {
                var path = pathPrintOutput.Split(new string[] { " -> " }, StringSplitOptions.None).Last();

                if (Path.GetExtension(path) == ".dll")
                {
                    yield return path;
                }
            }
        }
    }
}
