using Microsoft.CodeAnalysis.Emit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace VisualAlgoritmi_Studio.Compilation
{
    public static class Compiler
    {
        public static async Task<CompileResult> CompileToDll(Microsoft.CodeAnalysis.Compilation compilation)
        {
            string executionFolder = Path.Combine(
                Path.GetTempPath(),
                "VisualAlgoritmiStudio",
                "Executions",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(executionFolder);

            string assemblyPath = Path.Combine(executionFolder, "UserProgram.dll");
            string pdbPath = Path.Combine(executionFolder, "UserProgram.pdb");

            await using FileStream assemblyStream = File.Create(assemblyPath);
            await using FileStream pdbStream = File.Create(pdbPath);

            EmitResult emitResult = compilation.Emit(
                assemblyStream,
                pdbStream,
                options: new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.PortablePdb));

            if (!emitResult.Success)
            {
                return CompileResult.CompilationError([.. emitResult.Diagnostics]);
            }

            return CompileResult.Success(assemblyPath, pdbPath);
        }
    }
}