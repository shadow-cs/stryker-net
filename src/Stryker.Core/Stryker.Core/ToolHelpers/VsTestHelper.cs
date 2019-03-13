﻿using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Stryker.Core.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;

namespace Stryker.Core.ToolHelpers
{
    public class VsTestHelper
    {
        private readonly StrykerOptions _options;
        private readonly IFileSystem _fileSystem;
        private readonly Dictionary<OSPlatform, string> _vstestPaths = new Dictionary<OSPlatform, string>();

        public VsTestHelper(StrykerOptions options, IFileSystem fileSystem = null)
        {
            _options = options;
            _fileSystem = fileSystem ?? new FileSystem();
        }

        public string GetCurrentPlatformVsTestToolPath()
        {
            foreach (var path in GetVsTestToolPaths())
            {
                if (RuntimeInformation.IsOSPlatform(path.Key))
                {
                    return path.Value;
                }
            }

            throw new PlatformNotSupportedException(
                $"The current OS is not any of the following supported: " +
                $"{ OSPlatform.Windows.ToString() }, " +
                $"{ OSPlatform.Linux.ToString() } " +
                $"or " +
                $"{ OSPlatform.OSX.ToString() }");
        }

        public string GetDefaultVsTestExtensionsPath()
        {
            //string vstestMainPath = vstestToolPath.Substring(0, vstestToolPath.LastIndexOf(FilePathUtils.ConvertPathSeparators("\\")));
            //string extensionPath = Path.Combine(vstestMainPath, "Extensions");
            //if (_fileSystem.Directory.Exists(extensionPath))
            //{
            //    return extensionPath;
            //}
            //else
            //{
            //    return "";
            //}
            return "";
        }

        private Dictionary<OSPlatform, string> GetVsTestToolPaths()
        {
            // If any of the found paths is for the current OS, just return the paths as we have what we need
            if (_vstestPaths.Any(p => RuntimeInformation.IsOSPlatform(p.Key)))
            {
                return _vstestPaths;
            }

            if (_vstestPaths.Count == 0)
            {
                var nugetPackageFolders = CollectNugetPackageFolders();

                if (SearchNugetPackageFolders(nugetPackageFolders) is var nugetAssemblies && !(nugetAssemblies.Count == 0))
                {
                    Merge(_vstestPaths, nugetAssemblies);
                }
                if (DeployEmbeddedVsTestBinaries() is var deployedPaths && !(deployedPaths.Count == 0))
                {
                    Merge(_vstestPaths, deployedPaths);
                }
                if (_vstestPaths.Count == 0)
                {
                    throw new ApplicationException("Could not find or deploy vstest. Exiting.");
                }
            }

            return _vstestPaths;
        }

        private void Merge(Dictionary<OSPlatform, string> to, Dictionary<OSPlatform, string> from)
        {
            foreach (var val in from)
            {
                to[val.Key] = val.Value;
            }
        }

        private Dictionary<OSPlatform, string> SearchNugetPackageFolders(IEnumerable<string> nugetPackageFolders)
        {
            Dictionary<OSPlatform, string> vsTestPaths = new Dictionary<OSPlatform, string>();
            string versionString = FileVersionInfo.GetVersionInfo(typeof(IVsTestConsoleWrapper).Assembly.Location).ProductVersion;
            string portablePackageName = "microsoft.testplatform.portable";
            bool dllFound = false;
            bool exeFound = false;

            foreach (string nugetPackageFolder in nugetPackageFolders)
            {
                if (dllFound && exeFound)
                {
                    break;
                }

                string portablePackageFolder = _fileSystem.Directory.GetDirectories(nugetPackageFolder, portablePackageName, SearchOption.AllDirectories).First();

                string dllPath = FilePathUtils.ConvertPathSeparators(
                    Path.Combine(nugetPackageFolder, portablePackageFolder, versionString, "tools", "netcoreapp2.0", "vstest.console.dll"));
                string exePath = FilePathUtils.ConvertPathSeparators(
                    Path.Combine(nugetPackageFolder, portablePackageFolder, versionString, "tools", "net451", "vstest.console.exe"));

                if (!dllFound && _fileSystem.File.Exists(dllPath))
                {
                    vsTestPaths[OSPlatform.Linux] = dllPath;
                    vsTestPaths[OSPlatform.OSX] = dllPath;
                    dllFound = true;
                }
                if (!exeFound && _fileSystem.File.Exists(exePath))
                {
                    vsTestPaths[OSPlatform.Windows] = exePath;
                    exeFound = true;
                }
            }

            return vsTestPaths;
        }

        private IEnumerable<string> CollectNugetPackageFolders()
        {
            if (Environment.GetEnvironmentVariable("USERPROFILE") is var userProfile && !string.IsNullOrWhiteSpace(userProfile))
            {
                yield return Path.Combine(userProfile, ".nuget", "packages");
            }
            if (Environment.GetEnvironmentVariable("NUGET_PACKAGES") is var nugetPackagesLocation && !(string.IsNullOrWhiteSpace(nugetPackagesLocation)))
            {
                yield return Environment.GetEnvironmentVariable(@"NUGET_PACKAGES");
            }
        }

        private Dictionary<OSPlatform, string> DeployEmbeddedVsTestBinaries()
        {
            var paths = new Dictionary<OSPlatform, string>();

            var vsTestResources = typeof(VsTestHelper).Assembly
                .GetManifestResourceNames()
                .Where(r => r.Contains("vstest.console"));

            foreach (var vstest in vsTestResources)
            {
                using (var stream = typeof(VsTestHelper).Assembly
                .GetManifestResourceStream(vstest))
                {
                    var extension = Path.GetExtension(vstest);

                    var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                    var binaryPath = Path.Combine(tempDir, ".vstest", $"vstest.console{extension}");
                    _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(binaryPath));

                    using (var file = _fileSystem.FileStream.Create(binaryPath, FileMode.Create))
                    {
                        stream.CopyTo(file);
                    }

                    if (extension == ".exe")
                    {
                        paths[OSPlatform.Windows] = binaryPath;
                    }
                    else if (extension == ".dll")
                    {
                        paths[OSPlatform.Linux] = binaryPath;
                        paths[OSPlatform.OSX] = binaryPath;
                    }
                }
            }

            return paths;
        }

        public void Cleanup()
        {
            IList<string> pathsCleaned = new List<string>();
            var nugetPackageFolders = CollectNugetPackageFolders();
            foreach (var vstestConsole in _vstestPaths)
            {
                if (!nugetPackageFolders.Any(nf => vstestConsole.Value.Contains(nf)))
                {
                    if (!pathsCleaned.Contains(vstestConsole.Value))
                    {
                        pathsCleaned.Add(vstestConsole.Value);
                        _fileSystem.File.Delete(vstestConsole.Value);
                    }
                }
            }
        }
    }
}
