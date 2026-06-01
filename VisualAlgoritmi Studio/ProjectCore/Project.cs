using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.ProjectCore
{
    public class Project
    {
        public string Name { get; }
        public string RootPath { get; }
        public string UserCodePath { get; }
        public VisualizedDataStructure VisualizedDataStructure { get; }

        internal Project(string name, string rootPath, VisualizedDataStructure visualizedDataStructure)
        {
            Name = name;
            RootPath = rootPath;
            VisualizedDataStructure = visualizedDataStructure;
            UserCodePath = Path.Combine(rootPath, ProjectIO.UserCodeFileName);
        }  
 
        public async Task<string> GetUserCodeAsync()
        {
            if (!File.Exists(UserCodePath))
            {
                ThrowHelper.ThrowEntryFileMissing(UserCodePath);
            }

            return await File.ReadAllTextAsync(UserCodePath);
        }

        public async Task SaveUserCodeAsync(string code)
        {
            if (!File.Exists(UserCodePath))
            {
                ThrowHelper.ThrowEntryFileMissing(UserCodePath);
            }

            await File.WriteAllTextAsync(UserCodePath, code);
        }   

        private static class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowEntryFileMissing(string entryFile)
            {
                throw new IOException($"Entry file '{entryFile}' does not exist.");
            }
        }
    }
}
