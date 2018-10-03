using System.Collections.Generic;

namespace Stryker.Core.Initialisation
{
    public class ProjectFile
    {
        public ProjectFile()
        {
            FullFrameworkReferences = new List<string>();
        }
        public string ProjectReference { get; set; }
        public string TargetFramework { get; set; }
        public string AssemblyName { get; set; }
        public List<string> FullFrameworkReferences { get; set; }
    }
}