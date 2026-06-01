using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.Models
{
    public class RecentProjectCard
    {
        public string ProjectName { get; }
        public string ProjectPath { get; }
        public DateTime LastOpened { get; }
        public string LastOpenedFormatted { get; }
        public string DataStructureType { get; }
        public ICommand RemoveFromListCommand { get; }
        public ICommand OpenProjectLocationCommand { get; }

        public RecentProjectCard(string projectName, string projectPath, DateTime lastOpened, VisualizedDataStructure visualizedDataStructure, Func<string, Task> removeFromList)
        {
            ProjectName = projectName;
            ProjectPath = projectPath;
            LastOpened = lastOpened;
            LastOpenedFormatted = lastOpened.ToString("dd.MM.yyyy г. HH:mm");
            DataStructureType = visualizedDataStructure.ToString();

            RemoveFromListCommand = new AsyncRelayCommand(() => removeFromList(projectPath));

            OpenProjectLocationCommand = new RelayCommand(() =>
            {
                if (Directory.Exists(projectPath))
                {
                    Process.Start(new ProcessStartInfo 
                    { 
                        FileName = projectPath,
                        UseShellExecute = true
                    });
                }
            });
        }
    }
}
