using Microsoft.Extensions.Logging;
using Stryker.Core.Initialisation.ProjectComponent;
using Stryker.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml.Linq;

namespace Stryker.Core.Initialisation
{
    /// <summary>
    ///  - Reads .csproj to find project under test
    ///  - Scans project under test and store files to mutate
    ///  - Build composite for files
    /// </summary>
    public class InputFileResolver : IInputFileResolver
    {
        private IEnumerable<string> _foldersToIgnore = new string[] { "obj", "bin", "node_modules" };
        private IFileSystem _fileSystem { get; }
        private ILogger _logger { get; set; }

        public InputFileResolver(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _logger = ApplicationLogging.LoggerFactory.CreateLogger<InputFileResolver>();
        }

        public InputFileResolver() : this(new FileSystem()) { }

        /// <summary>
        /// Finds the referencedProjects and looks for all files that should be mutated in those projects
        /// </summary>
        public ProjectInfo ResolveInput(string currentDirectory, string projectName)
        {
            string projectFile = ScanProjectFile(currentDirectory);
            var file = _fileSystem.File.OpenText(projectFile);
            XDocument xDocument = XDocument.Load(projectFile);

            var currentProjectInfo = ReadProjectFile(projectFile, projectName, currentDirectory);
            var fullFrameworkReferences = GetFullFrameworkReferences(xDocument, currentDirectory);
            var projectUnderTestPath = Path.GetDirectoryName(Path.GetFullPath($"{currentDirectory}{Path.DirectorySeparatorChar}{currentProjectInfo.ProjectReference}"));
            var projectUnderTestInfo = FindProjectUnderTestAssemblyName(Path.GetFullPath($"{projectUnderTestPath}{Path.DirectorySeparatorChar}{Path.GetFileName(currentProjectInfo.ProjectReference)}"));
            var inputFiles = FindInputFiles(projectUnderTestPath);

            return new ProjectInfo()
            {
                FullFrameworkRefernces = fullFrameworkReferences,
                TestProjectPath = currentDirectory,
                TestProjectFileName = Path.GetFileName(projectFile),
                TargetFramework = currentProjectInfo.TargetFramework,
                ProjectContents = inputFiles,
                ProjectUnderTestPath = projectUnderTestPath,
                ProjectUnderTestAssemblyName = projectUnderTestInfo ?? Path.GetFileNameWithoutExtension(currentProjectInfo.ProjectReference),
                ProjectUnderTestProjectName = Path.GetFileNameWithoutExtension(currentProjectInfo.ProjectReference)
            };
        }

        /// <summary>
        /// Resursively scans the given directory for files to mutate
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private FolderComposite FindInputFiles(string path)
        {
            var folderComposite = new FolderComposite()
            {
                Name = Path.GetFullPath(path)
            };
            foreach (var folder in _fileSystem.Directory.EnumerateDirectories(path).Where(x => !_foldersToIgnore.Contains(Path.GetFileName(x))))
            {
                folderComposite.Add(FindInputFiles(folder));
            }
            foreach(var file in _fileSystem.Directory.GetFiles(path, "*.cs", SearchOption.TopDirectoryOnly))
            {
                folderComposite.Add(new FileLeaf()
                {
                    SourceCode = _fileSystem.File.ReadAllText(file),
                    Name = Path.GetFileName(file),
                    FullPath = file
                });
            }
            return folderComposite;
        }

        public string ScanProjectFile(string currentDirectory)
        {
            var projectFiles = _fileSystem.Directory.GetFiles(currentDirectory, "*.csproj", SearchOption.AllDirectories);
            _logger.LogTrace("Scanned the current directory for *.csproj files: found {0}", projectFiles);
            if (projectFiles.Count() > 1)
            {
                throw new FileNotFoundException("Expected exactly one .csproj file, found more than one. Please fix your project contents");
            } else if (!projectFiles.Any())
            {
                throw new FileNotFoundException($"No .csproj file found, please check your project directory at {Directory.GetCurrentDirectory()}");
            }
            _logger.LogInformation("Using {0} as project file", projectFiles.First());
            return projectFiles.First();
        }

        public ProjectFile ReadProjectFile(string projectFilePath, string projectName, string basePath)
        {
            var projectFile = _fileSystem.File.OpenText(projectFilePath);
            XDocument xDocument = XDocument.Load(projectFile);
            var projectInfo = new ProjectFileReader(_fileSystem).ReadProjectFile(xDocument, projectName);
            List<string> fullFrameworkReferences = new List<string>();
            if (projectInfo.TargetFramework.StartsWith("v"))
            {
                projectInfo.FullFrameworkReferences = GetFullFrameworkReferences(xDocument, basePath);
                Console.WriteLine($"{fullFrameworkReferences}");
            }
            
            _logger.LogDebug("Values found in project file {@0}", projectInfo);

            return projectInfo;
        }

        public string FindProjectUnderTestAssemblyName(string projectFilePath)
        {
            var projectFile = _fileSystem.File.OpenText(projectFilePath);
            XDocument xDocument = XDocument.Load(projectFile);
            return new ProjectFileReader(_fileSystem).FindAssemblyName(xDocument);
        }

        private List<string> GetFullFrameworkReferences(XDocument document, string basePath)
        {
            var projectReferences = document.Elements()
                .Descendants()
                .Where(x => string.Equals(x.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Attributes()
                    .Where(a => string.Equals(a.Name.LocalName, "include", StringComparison.OrdinalIgnoreCase))
                    .Single()
                    .Value)
                .ToList();
            List<string> filePaths = new List<string>();
            foreach (var path in projectReferences)
            {
                //Change get currentdirectory
                Console.WriteLine($"BaseUri: {basePath}");
                string fullFilePath = Path.GetFullPath(path, basePath);
                basePath = Path.GetFullPath(@"..\", fullFilePath);
                var file = _fileSystem.File.OpenText(fullFilePath);

                var filesToAdd = GetFullFrameworkReferences(XDocument.Load(file), basePath);
                Console.WriteLine($"Count csprojfiles: {filesToAdd.Count}");
                if (filesToAdd.Count > 0)
                {
                    Console.WriteLine($"{filesToAdd.FirstOrDefault()}");
                    filePaths.AddRange(filesToAdd);
                }
                filePaths.Add(fullFilePath);
            }
            return filePaths;
        }
    }
}
