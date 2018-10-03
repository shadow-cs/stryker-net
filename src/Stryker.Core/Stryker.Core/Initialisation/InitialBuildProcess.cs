using Microsoft.Extensions.Logging;
using Stryker.Core.Logging;
using Stryker.Core.Testing;
using System;
using System.Collections.Generic;

namespace Stryker.Core.Initialisation
{
    public class InitialBuildProcess : IInitialBuildProcess
    {
        private IProcessExecutor _processExecutor { get; set; }
        private ILogger _logger { get; set; }

        public InitialBuildProcess(IProcessExecutor processExecutor = null)
        {
            _processExecutor = processExecutor ?? new ProcessExecutor();
            _logger = ApplicationLogging.LoggerFactory.CreateLogger<InitialBuildProcess>();
        }

        public void InitialBuild(string path, string projectName)
        {
            _logger.LogInformation("Starting initial build");
            var result = _processExecutor.Start(path, "dotnet", $"build {projectName}");
            _logger.LogDebug("Initial build output {0}", result.Output);
            if (result.ExitCode != 0)
            {
                // Initial build failed
                _logger.LogInformation("Could not build with dotnet build");
                TryBuildMSBuild(path);
            }
            _logger.LogInformation("Initial build successful");
        }

        private void TryBuildMSBuild(string path)
        {
            _logger.LogInformation("Starting initial build with msbuild");
            //var globalProperties = new Dictionary<string, string>();
            //var buildRequest = new BuildRequestData(path, globalProperties, null, new string[] { "Build" }, null);
            //var pc = new ProjectCollection();

            //var result = BuildManager.DefaultBuildManager.Build(new BuildParameters(pc), buildRequest);
            var result = _processExecutor.Start(path, @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe", "");

            if(result.ExitCode != 0)
            {
                throw new Exception(result.Output.ToString());

            }
        }
    }
}
