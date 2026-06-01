using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VisualAlgoritmi_Studio.Diagnostics
{
    internal static class CrashReporter
    {
        private static readonly object LockObject = new();

        public static void RegisterGlobalHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    WriteCrashReport(ex, "Unhandled application exception");
                }
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                WriteCrashReport(e.Exception, "Unobserved task exception");
                e.SetObserved();
            };
        }

        public static void WriteCrashReport(Exception exception, string source)
        {
            try
            {
                lock (LockObject)
                {
                    string folder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "VisualAlgoritmi Studio",
                        "CrashReports");

                    Directory.CreateDirectory(folder);

                    string fileName = $"crash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                    string path = Path.Combine(folder, fileName);

                    var report = new StringBuilder();

                    report.AppendLine("VisualAlgoritmi Studio Crash Report");
                    report.AppendLine("-----------------------------------");
                    report.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    report.AppendLine($"Source: {source}");
                    report.AppendLine($"OS: {Environment.OSVersion}");
                    report.AppendLine($".NET: {Environment.Version}");
                    report.AppendLine();
                    report.AppendLine(exception.ToString());

                    File.WriteAllText(path, report.ToString());
                }
            }
            catch
            {
                // Never let the crash logger crash the app.
            }
        }
    }
}