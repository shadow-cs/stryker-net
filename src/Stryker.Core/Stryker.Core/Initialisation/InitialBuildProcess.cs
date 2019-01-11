using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using Stryker.Core.Logging;
using Stryker.Core.Testing;
using System;

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

            var vsInstance = MSBuildLocator.RegisterDefaults();
            
            var result = _processExecutor.Start(path, vsInstance.MSBuildPath, "");

            if(result.ExitCode != 0)
            {
                throw new Exception(result.Output.ToString());
            } else
            {
                _logger.LogInformation("Initial build successful");
            }
        }
    }
}
