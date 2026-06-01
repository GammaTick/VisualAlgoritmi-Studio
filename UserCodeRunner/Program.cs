using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Loader;
using VisualAlgoritmi.Runtime.Operations;

namespace UserCodeRunner
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Missing arguments.");
                Console.Error.WriteLine("Expected: <pipeName> <userAssemblyPath>");
                return 1;
            }

            string pipeName = args[0];
            string userAssemblyPath = args[1];

            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            OperationRecorder.Clear();
            VisualStructureIdProvider.Reset();

            try
            {
                if (!File.Exists(userAssemblyPath))
                {
                    Console.Error.WriteLine("User assembly not found:");
                    Console.Error.WriteLine(userAssemblyPath);

                    await SendRecordedOperationsAsync(pipeName);
                    return 1;
                }

                int exitCode = await RunUserAssemblyAsync(userAssemblyPath);

                await SendRecordedOperationsAsync(pipeName);

                return exitCode;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Console.Error.WriteLine(ex.InnerException);

                await SendRecordedOperationsAsync(pipeName);

                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);

                await SendRecordedOperationsAsync(pipeName);

                return 1;
            }
        }

        private static async Task<int> RunUserAssemblyAsync(string userAssemblyPath)
        {
            string runnerDirectory = AppContext.BaseDirectory;
            string? userAssemblyDirectory = Path.GetDirectoryName(userAssemblyPath);

            AssemblyLoadContext.Default.Resolving += (_, assemblyName) =>
            {
                string dependencyFileName = assemblyName.Name + ".dll";

                if (!string.IsNullOrWhiteSpace(userAssemblyDirectory))
                {
                    string dependencyNearUserProgram = Path.Combine(
                        userAssemblyDirectory,
                        dependencyFileName);

                    if (File.Exists(dependencyNearUserProgram))
                    {
                        return AssemblyLoadContext.Default.LoadFromAssemblyPath(
                            dependencyNearUserProgram);
                    }
                }

                string dependencyNearRunner = Path.Combine(
                    runnerDirectory,
                    dependencyFileName);

                if (File.Exists(dependencyNearRunner))
                {
                    return AssemblyLoadContext.Default.LoadFromAssemblyPath(
                        dependencyNearRunner);
                }

                return null;
            };

            if (!string.IsNullOrWhiteSpace(userAssemblyDirectory))
            {
                Directory.SetCurrentDirectory(userAssemblyDirectory);
            }

            Assembly userAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(userAssemblyPath);

            MethodInfo? entryPoint = userAssembly.EntryPoint;

            if (entryPoint == null)
            {
                Console.Error.WriteLine("No Main method found in user program.");
                return 1;
            }

            object? result = InvokeEntryPoint(entryPoint);

            if (result is Task task)
            {
                await task;

                object? taskResult = GetTaskResult(task);

                if (taskResult is int asyncExitCode)
                {
                    return asyncExitCode;
                }
            }

            if (result is int exitCode)
            {
                return exitCode;
            }

            return 0;
        }

        private static async Task SendRecordedOperationsAsync(string pipeName)
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous);

            await pipe.ConnectAsync(5000);

            OperationRecorder.WriteTo(pipe);
        }

        private static object? InvokeEntryPoint(MethodInfo entryPoint)
        {
            ParameterInfo[] parameters = entryPoint.GetParameters();

            if (parameters.Length == 0)
            {
                return entryPoint.Invoke(null, null);
            }

            return entryPoint.Invoke(null, [Array.Empty<string>()]);
        }

        private static object? GetTaskResult(Task task)
        {
            Type taskType = task.GetType();

            if (!taskType.IsGenericType)
            {
                return null;
            }

            PropertyInfo? resultProperty = taskType.GetProperty("Result");

            return resultProperty?.GetValue(task);
        }
    }
}