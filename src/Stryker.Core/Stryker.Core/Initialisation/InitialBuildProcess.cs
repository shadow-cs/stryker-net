using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using Stryker.Core.Exceptions;
using Stryker.Core.Logging;
using Stryker.Core.Testing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Stryker.Core.Initialisation
{
    public interface IInitialBuildProcess
    {
        //void InitialBuild(string path, string projectName);
        Task InitialBuild(AdhocWorkspace workspace);
    }
    
    public class InitialBuildProcess : IInitialBuildProcess
    {
        private IProcessExecutor _processExecutor { get; set; }
        private ILogger _logger { get; set; }

        public InitialBuildProcess(IProcessExecutor processExecutor = null)
        {
            _processExecutor = processExecutor ?? new ProcessExecutor();
            _logger = ApplicationLogging.LoggerFactory.CreateLogger<InitialBuildProcess>();
        }

        public async Task<bool> InitialBuild(AdhocWorkspace workspace)
        {
            //// compile workspaces
            //_logger.LogInformation("Starting initial build");
            //var result = _processExecutor.Start(path, "dotnet", $"build {projectName}");
            //_logger.LogDebug("Initial build output {0}", result.Output);
            //if (result.ExitCode != 0)
            //{
            //    // Initial build failed
            //    throw new StrykerInputException("Initial build of targeted project failed. Please make targeted project buildable.", result.Output);
            //}
            bool success;
            var solution = workspace.CurrentSolution;
            var graph = solution.GetProjectDependencyGraph();
            foreach (var projectId in graph.GetTopologicallySortedProjects())
            {
                var project = solution.GetProject(projectId);
                Compilation projectCompilation = project.GetCompilationAsync().Result;
                if (null != projectCompilation && !string.IsNullOrEmpty(projectCompilation.AssemblyName))
                {
                    using (var stream = new MemoryStream())
                    {
                        EmitResult result = projectCompilation.Emit(stream);
                        if (result.Success)
                        {
                            string fileName = string.Format("{0}.dll", projectCompilation.AssemblyName);

                            using (FileStream file = File.Create(project.OutputFilePath + '\\' + fileName))
                            {
                                stream.Seek(0, SeekOrigin.Begin);
                                stream.CopyTo(file);
                            }
                        }
                        else
                        {
                            success = false;
                        }
                    }
                }
                return success;
            }
        }
    }
}
