using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using VisualAlgoritmi_Studio.Canvas.Operations;
using VisualAlgoritmi_Studio.Compilation;
using VisualAlgoritmi_Studio.Controls.Canvas.Canvases;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;
using VisualAlgoritmi_Studio.Controls.Canvas.Operations;
using VisualAlgoritmi_Studio.Controls.Console;
using VisualAlgoritmi_Studio.Controls.Editor;
using VisualAlgoritmi_Studio.Execution;
using VisualAlgoritmi_Studio.Execution.BinaryPipeline;
using VisualAlgoritmi_Studio.Models;
using VisualAlgoritmi_Studio.ProjectCore;
using VisualAlgoritmi_Studio.RoslynCore;
using VisualAlgoritmi_Studio.RoslynCore.Metadata;
using VisualAlgoritmi_Studio.Views.Dialogs;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.ViewModels
{
    internal class VisualizationViewModel : ViewModelBase
    {
        public ICommand GoHomeCommand { get; }
        public ICommand SaveCodeCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CommentLinesCommand { get; }
        public ICommand UncommentLinesCommand { get; }
        public ICommand RunCodeCommand { get; }
        public ICommand ReverseStepCommand { get; }
        public ICommand AdvanceStepCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ShowEditorOnlyCommand { get; }
        public ICommand ShowNormalCommand { get; }
        public ICommand ShowCanvasOnlyCommand { get; }
        public ICommand ShowErrorListCommand { get; }
        public ICommand ShowOutputCommand { get; }
        public ICommand ExportAnimationCommand { get; }
        public ICommand ResetViewCommand { get; }
        public ICommand NavigateToErrorCommand { get; }
        public ICommand CopyErrorMessageCommand { get; }
        public ICommand ImportCodeCommand { get; }
        public ICommand OpenLocationCommand { get; }
        public ICommand ExportCodeCommand { get; }
        public ICommand LoadExampleCodeCommand { get; }

        public ObservableCollection<EditorError> Errors { get; } = [];

        public string RunButtonText => IsCodeRunning ? "Спри" : "Изпълни";

        public string RunButtonIcon => IsCodeRunning
            ? "avares://VisualAlgoritmi_Studio/Assets/Icons/stop-execution.svg"
            : "avares://VisualAlgoritmi_Studio/Assets/Icons/run-code.svg";

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public LayoutMode CurrentLayout
        {
            get => _layoutMode;
            set
            {
                if (SetProperty(ref _layoutMode, value))
                {
                    OnPropertyChanged(nameof(EditorColumnWidth));
                    OnPropertyChanged(nameof(CanvasColumnWidth));
                    OnPropertyChanged(nameof(IsEditorAreaVisible));
                    OnPropertyChanged(nameof(IsCanvasAreaVisible));
                }
            }
        }

        public BottomPanelMode CurrentBottomPanel
        {
            get => _bottomPanelMode;
            set
            {
                if (SetProperty(ref _bottomPanelMode, value))
                {
                    OnPropertyChanged(nameof(IsErrorListVisible));
                    OnPropertyChanged(nameof(IsOutputVisible));
                    OnPropertyChanged(nameof(IsErrorListSelected));
                    OnPropertyChanged(nameof(IsOutputSelected));
                }
            }
        }

        public GridLength EditorColumnWidth =>
            _layoutMode == LayoutMode.CanvasOnly
                ? GridLength.Auto
                : new GridLength(1, GridUnitType.Star);

        public GridLength CanvasColumnWidth =>
            _layoutMode == LayoutMode.EditorOnly
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);

        public bool IsEditorAreaVisible => _layoutMode != LayoutMode.CanvasOnly;
        public bool IsCanvasAreaVisible => _layoutMode != LayoutMode.EditorOnly;
        public bool IsErrorListVisible => _bottomPanelMode == BottomPanelMode.ErrorList;
        public bool IsOutputVisible => _bottomPanelMode == BottomPanelMode.Output;
        public bool IsErrorListSelected => _bottomPanelMode == BottomPanelMode.ErrorList;
        public bool IsOutputSelected => _bottomPanelMode == BottomPanelMode.Output;

        public string SelectedDataStructureName => GetDataStructureDisplayName(_visualizedDataStructure);

        public string ExecutionStatusText
        {
            get => _executionStatusText;
            private set => SetProperty(ref _executionStatusText, value);
        }
        public bool IsCodeRunning => _codeExecutionState == CodeExecutionState.Running;

        public string CurrentStepValueText =>
            $" {(_visualizerCanvas.CurrentStep + 1).ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ")} / {_visualizerCanvas.StepCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ")}";

        public string CurrentOperationsText =>
            _visualizerCanvas.GetOperationsAtCurrentStep();

        public string OperationsPrefixText =>
            _visualizerCanvas.GetOperationCountAtCurrentStep() == 1 ? "Операция:" : "Операции:";

        public string CanvasZoomText => $"Мащаб: {_visualizerCanvas.ZoomPercentage:F0}%";

        public string CanvasOffsetText
        {
            get
            {
                var (x, y) = _visualizerCanvas.GetOffsetFromCenter();
                return $"Отместване: {x}, {y}";
            }
        }

        public VisualizerCanvasBase VisualizerCanvas
        {
            get => _visualizerCanvas;
        }

        private readonly MainWindowViewModel _main;
        private readonly ProjectCore.Project _projectManager;
        private IClipboard? _clipboard;
        private readonly VisualizedDataStructure _visualizedDataStructure;

        private readonly DataStructureMetadata _dataStructureMetadata;
        private readonly VisualizerCanvasBase _visualizerCanvas;

        private readonly CanvasOperationBinaryPipeline _operationPipeline = new();

        private CodeEditor? _codeEditor;
        private ConsoleControl? _consoleControl;
        private IStorageProvider? _storageProvider;
        private LayoutMode _layoutMode = LayoutMode.Normal;
        private BottomPanelMode _bottomPanelMode = BottomPanelMode.ErrorList;
        private string _executionStatusText = "Не се изпълнява";
        private DispatcherTimer? _executionTimer;
        private DateTime _executionStartTime;
        private bool _hasUnsavedChanges;
        private CodeExecutionState _codeExecutionState = CodeExecutionState.Idle;
        private bool _runnerConsoleEventsSubscribed;

        private readonly UserCodeProcessRunner _userCodeProcessRunner = new();

        public VisualizationViewModel(MainWindowViewModel main, ProjectCore.Project projectManager, VisualizedDataStructure visualizedDataStructure)
        {
            _main = main;
            _projectManager = projectManager;
            _visualizedDataStructure = visualizedDataStructure;

            _dataStructureMetadata = DataStructureMetadataFactory.Create(visualizedDataStructure);

            _visualizerCanvas = visualizedDataStructure switch
            {
                VisualizedDataStructure.ArrayList => new ArrayListCanvas(),
                VisualizedDataStructure.List => new ListCanvas(),
                VisualizedDataStructure.LinkedList => new LinkedListCanvas(),
                VisualizedDataStructure.Queue => new QueueCanvas(),
                VisualizedDataStructure.Stack => new StackCanvas(),

                _ => throw new NotSupportedException($"Visualization for {visualizedDataStructure} is not supported."),
            };

            GoHomeCommand = new AsyncRelayCommand(async () =>
            {
                string message = HasUnsavedChanges
                    ? "Имате незапазени промени. Наистина ли искате да се върнете на началния екран?"
                    : "Наистина ли искате да се върнете на началния екран?";

                var result = await MessageBox.ShowAsync(
                    "Потвърждение",
                    message,
                    MessageBoxButtons.YesNo,
                    HasUnsavedChanges ? MessageBoxIcon.Warning : MessageBoxIcon.None);

                if (result == MessageBoxResult.Yes)
                {
                    _main.CurrentViewModel = new HomeViewModel(_main);
                }
            });

            SaveCodeCommand = new AsyncRelayCommand(SaveUserCode);

            UndoCommand = new RelayCommand(() => _codeEditor?.UndoChange());
            RedoCommand = new RelayCommand(() => _codeEditor?.RedoChange());
            CommentLinesCommand = new RelayCommand(() => _codeEditor?.CommentOutSelectedLines());
            UncommentLinesCommand = new RelayCommand(() => _codeEditor?.UncommentSelectedLines());

            AdvanceStepCommand = new RelayCommand(() =>
            {
                _visualizerCanvas.StepForward();
                NotifyStepChanged();
            });

            ReverseStepCommand = new RelayCommand(() =>
            {
                _visualizerCanvas.StepBack();
                NotifyStepChanged();
            });

            RestartCommand = new RelayCommand(() =>
            {
                _visualizerCanvas.ResetSteps();
                NotifyStepChanged();
            });

            RunCodeCommand = new AsyncRelayCommand(
                RunCode,
                AsyncRelayCommandOptions.AllowConcurrentExecutions);

            ShowEditorOnlyCommand = new RelayCommand(() => CurrentLayout = LayoutMode.EditorOnly);
            ShowNormalCommand = new RelayCommand(() => CurrentLayout = LayoutMode.Normal);
            ShowCanvasOnlyCommand = new RelayCommand(() => CurrentLayout = LayoutMode.CanvasOnly);
            ShowErrorListCommand = new RelayCommand(() => CurrentBottomPanel = BottomPanelMode.ErrorList);
            ShowOutputCommand = new RelayCommand(() => CurrentBottomPanel = BottomPanelMode.Output);

            ExportAnimationCommand = new AsyncRelayCommand(ExportAnimation);

            ResetViewCommand = new RelayCommand(() => _visualizerCanvas.ResetView());

            NavigateToErrorCommand = new RelayCommand<EditorError>(error =>
            {
                if (_codeEditor == null || error == null)
                {
                    return;
                }

                _codeEditor.SetCaretPosition(error.Line - 1, error.Column - 1);
            });

            CopyErrorMessageCommand = new AsyncRelayCommand<EditorError>(async error =>
            {
                if (_clipboard == null || error == null)
                {
                    return;
                }

                string text = $"{error.Code}  {error.Message}  (Ред {error.Line}, Колона {error.Column})";
                await _clipboard.SetTextAsync(text);
            });

            ImportCodeCommand = new AsyncRelayCommand(ImportCode);
            OpenLocationCommand = new RelayCommand(OpenLocation);
            ExportCodeCommand = new AsyncRelayCommand(ExportCode);
            LoadExampleCodeCommand = new AsyncRelayCommand(LoadExampleCode);

            _visualizerCanvas.ViewChanged += OnCanvasViewChanged;
            _visualizerCanvas.StepChanged += OnStepChanged;
        }

        private static string GetDataStructureDisplayName(VisualizedDataStructure dataStructure)
        {
            return dataStructure switch
            {
                VisualizedDataStructure.ArrayList => "ArrayList<T>",
                VisualizedDataStructure.List => "List<T>",
                VisualizedDataStructure.LinkedList => "LinkedList<T>",
                VisualizedDataStructure.Queue => "Queue<T>",
                VisualizedDataStructure.Stack => "Stack<T>",
                _ => dataStructure.ToString()
            };
        }

        private void OnCodeContentChanged(object? sender, EventArgs e)
        {
            HasUnsavedChanges = true;
        }

        private void OnStepChanged(object? sender, EventArgs e)
        {
            NotifyStepChanged();
        }

        private void NotifyStepChanged()
        {
            OnPropertyChanged(nameof(CurrentStepValueText));
            OnPropertyChanged(nameof(CurrentOperationsText));
            OnPropertyChanged(nameof(OperationsPrefixText));
        }

        private void OnCanvasViewChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanvasZoomText));
            OnPropertyChanged(nameof(CanvasOffsetText));
        }

        private void SetCodeExecutionState(CodeExecutionState state)
        {
            if (SetProperty(ref _codeExecutionState, state))
            {
                OnPropertyChanged(nameof(IsCodeRunning));
                OnPropertyChanged(nameof(RunButtonText));
                OnPropertyChanged(nameof(RunButtonIcon));
            }
        }

        private async Task SaveUserCode()
        {
            if (_codeEditor == null)
            {
                return;
            }

            await _projectManager.SaveUserCodeAsync(_codeEditor.GetCode());
            HasUnsavedChanges = false;
        }

        private async Task ImportCode()
        {
            if (_codeEditor == null || _storageProvider == null)
            {
                return;
            }

            IReadOnlyList<IStorageFile> files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Импортирай код",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("C# Source File") { Patterns = ["*.cs"] }
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            await using Stream stream = await files[0].OpenReadAsync();
            using StreamReader reader = new(stream);
            string code = await reader.ReadToEndAsync();

            _codeEditor.SetCode(code);
        }

        private void OpenLocation()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _projectManager.RootPath,
                UseShellExecute = true
            });
        }

        private async Task LoadExampleCode()
        {
            if (_codeEditor == null)
            {
                return;
            }

            MessageBoxResult result;

            if (_codeEditor.GetCodeLength() == 0)
            {
                result = await MessageBox.ShowAsync(
                    "Зареди примерен код",
                    "Наистина ли искате да заредите примерния код?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.None);
            }
            else
            {
                result = await MessageBox.ShowAsync(
                    "Зареди примерен код",
                    "Зареждането на примерния код ще замени текущото съдържание на редактора. Наистина ли искате да продължите?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
            }

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string exampleCode = await ProjectIO.GetExampleCodeAsync(_visualizedDataStructure);
            _codeEditor.SetCode(exampleCode);
        }

        private async Task ExportCode()
        {
            if (_codeEditor == null || _storageProvider == null)
            {
                return;
            }

            IStorageFile? file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Експортирай код",
                DefaultExtension = "cs",
                FileTypeChoices =
                [
                    new FilePickerFileType("C# Source File") { Patterns = ["*.cs"] }
                ],
                SuggestedFileName = "UserCode.cs"
            });

            if (file == null)
            {
                return;
            }

            string code = _codeEditor.GetCode();

            await using Stream stream = await file.OpenWriteAsync();
            await using StreamWriter writer = new(stream);
            await writer.WriteAsync(code);
        }

        private async Task ExportAnimation()
        {
            if (_storageProvider == null)
            {
                return;
            }

            if (!_visualizerCanvas.HasExecuted)
            {
                await MessageBox.ShowAsync(
                    "Няма налична анимация",
                    "Моля, изпълнете кода, преди да експортирате анимацията.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Warning);

                return;
            }

            CanvasTimeline? canvasTimeline = _visualizerCanvas.Timeline;

            if (_visualizerCanvas.StepCount == 0 || canvasTimeline == null)
            {
                var confirmResult = await MessageBox.ShowAsync(
                    "Празна анимация",
                    "Анимацията не съдържа стъпки. Сигурни ли сте, че искате да я експортирате?",
                    MessageBoxButtons.YesCancel,
                    MessageBoxIcon.Warning);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            string dataStructureName = _visualizedDataStructure.ToString();
            string suggestedFileName = $"{dataStructureName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.vaanim";

            IStorageFile? file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Експортирай анимация",
                DefaultExtension = "vaanim",
                FileTypeChoices =
                [
                    new FilePickerFileType("Visual Algoritmi Animation") { Patterns = ["*.vaanim"] }
                ],
                SuggestedFileName = suggestedFileName
            });

            if (file == null)
            {
                return;
            }

            string body = CanvasTimelineSerializer.Serialize(canvasTimeline);

            string header =
                $"FileFormatVersion:{AppInfo.AnimationFileFormatVersion}{Environment.NewLine}" +
                $"AppVersion:{AppInfo.Version}{Environment.NewLine}" +
                $"DataStructure:{dataStructureName}{Environment.NewLine}";

            int headerLines = header.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length + 1;

            header = header.Insert(0, $"HeaderLines:{headerLines}{Environment.NewLine}");

            string content = header + body;

            await using Stream stream = await file.OpenWriteAsync();
            await using StreamWriter writer = new(stream);
            await writer.WriteAsync(content);
        }

        private async Task RunCode()
        {
            if (_codeEditor == null)
            {
                return;
            }

            if (_codeEditor.HasErrors())
            {
                CurrentBottomPanel = BottomPanelMode.ErrorList;
                return;
            }

            if (_codeExecutionState == CodeExecutionState.Running)
            {
                await StopRunningCodeAsync();
                return;
            }

            if (_codeExecutionState is CodeExecutionState.Preparing
                or CodeExecutionState.ForceStopped
                or CodeExecutionState.Finalizing)
            {
                return;
            }

            SetCodeExecutionState(CodeExecutionState.Preparing);

            try
            {
                var session = _codeEditor.CodeAnalysisSession;

                session.FlushPendingSourceText();

                await SaveUserCode();

                EnsureConsoleConnectedToRunner();

                _consoleControl?.Clear();

                CurrentLayout = LayoutMode.Normal;
                CurrentBottomPanel = BottomPanelMode.Output;

                Dispatcher.UIThread.Post(() =>
                {
                    _consoleControl?.FocusConsole();
                }, DispatcherPriority.Background);

                var rewrittenCompilation = await RewriteUserCode(session);

                if (rewrittenCompilation == null)
                {
                    return;
                }

                ExecutionStatusText = "Компилира се…";

                var compileResult = await CompileUserCode(rewrittenCompilation);

                if (!compileResult.IsSuccess)
                {
                    return;
                }

                await DisposeOperationPipelineIfOpenAsync();

                string pipelineName = _operationPipeline.Open();

                Task<MemoryStream> captureTask = _operationPipeline.CaptureToMemoryStreamAsync();

                SetCodeExecutionState(CodeExecutionState.Running);

                _consoleControl?.BeginSession();

                Dispatcher.UIThread.Post(() =>
                {
                    _consoleControl?.FocusConsole();
                }, DispatcherPriority.Background);

                StartExecutionTimer();

                var executionResult = await _userCodeProcessRunner.RunAsync(
                    compileResult.AssemblyPath!,
                    pipelineName);

                StopExecutionTimer();

                if (!executionResult.IsSuccess)
                {
                    await HandleNonSuccessfulExecutions(executionResult);
                    return;
                }

                SetCodeExecutionState(CodeExecutionState.Finalizing);

                using MemoryStream operationStream = await captureTask.WaitAsync(TimeSpan.FromSeconds(2));

                operationStream.Position = 0;

                OperationBinaryPipelineReader reader = new(operationStream, _visualizedDataStructure);
                CanvasTimeline timeline = reader.ReadAllOperations();

                _visualizerCanvas.LoadTimelineAndResetView(timeline);
                _visualizerCanvas.ResetSteps();

                NotifyStepChanged();
            }
            catch (Exception ex) when (_codeExecutionState == CodeExecutionState.ForceStopped && IsExpectedStopException(ex))
            {
                ExecutionStatusText = "Изпълнението беше спряно.";
            }
            catch (Exception ex)
            {
                ExecutionStatusText = "Грешка в хоста";

                await MessageBox.ShowAsync(
                    "Грешка при изпълнение",
                    $"Възникна грешка при изпълнение на кода: {ex.Message}",
                    MessageBoxButtons.OkCopy,
                    MessageBoxIcon.Error);
            }
            finally
            {
                StopExecutionTimer();

                _consoleControl?.EndSession();

                if (_codeExecutionState == CodeExecutionState.ForceStopped)
                {
                    ExecutionStatusText = "Изпълнението беше спряно.";
                }

                await DisposeOperationPipelineIfOpenAsync();

                SetCodeExecutionState(CodeExecutionState.Idle);
            }
        }

        private void EnsureConsoleConnectedToRunner()
        {
            if (!_runnerConsoleEventsSubscribed)
            {
                _runnerConsoleEventsSubscribed = true;

                _userCodeProcessRunner.StandardOutputReceived += text =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _consoleControl?.AppendOutput(text);
                    });
                };

                _userCodeProcessRunner.StandardErrorReceived += text =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _consoleControl?.AppendError(text);
                    });
                };
            }

            if (_consoleControl != null)
            {
                _consoleControl.InputSubmittedAsync =
                    _userCodeProcessRunner.SendStandardInputLineAsync;
            }
        }

        private static bool IsExpectedStopException(Exception exception)
        {
            return exception is IOException
                or ObjectDisposedException
                or TimeoutException
                or OperationCanceledException;
        }

        private async Task StopRunningCodeAsync()
        {
            if (_codeExecutionState != CodeExecutionState.Running)
            {
                return;
            }

            SetCodeExecutionState(CodeExecutionState.ForceStopped);

            StopExecutionTimer();

            ExecutionStatusText = "Спиране…";

            await _userCodeProcessRunner.StopAsync();
        }

        private async Task<Microsoft.CodeAnalysis.Compilation?> RewriteUserCode(CodeAnalysisSession session)
        {
            Document? document = session.GetDocument();

            if (document is null)
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    "Документът с кода не беше намерен.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return null;
            }

            Microsoft.CodeAnalysis.Compilation? compilation = await document.Project.GetCompilationAsync();

            if (compilation is null)
            {
                await MessageBox.ShowAsync(
                    "Грешка при компилация",
                    "Компилацията не можа да бъде създадена.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return null;
            }

            SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();

            if (syntaxTree is null)
            {
                await MessageBox.ShowAsync(
                    "Грешка при компилация",
                    "Синтактичното дърво не беше намерено.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return null;
            }

            return await Rewriting.UserCodeRewriter.RewriteAsync(
                document,
                compilation,
                syntaxTree,
                _dataStructureMetadata);
        }

        private async Task<CompileResult> CompileUserCode(Microsoft.CodeAnalysis.Compilation rewrittenCompilation)
        {
            CompileResult compileResult = await Compiler.CompileToDll(rewrittenCompilation);

            if (!compileResult.IsSuccess)
            {
                ExecutionStatusText = "Компилацията не беше успешна.";

                string errorMessage = compileResult.FailureMessage ?? "Компилацията не беше успешна.";

                if (compileResult.Diagnostics.Length > 0)
                {
                    errorMessage += Environment.NewLine
                        + Environment.NewLine
                        + string.Join(Environment.NewLine, compileResult.Diagnostics);
                }

                await MessageBox.ShowAsync(
                    "Грешка при компилация",
                    errorMessage,
                    MessageBoxButtons.OkCopy,
                    MessageBoxIcon.Error);
            }

            return compileResult;
        }

        private async Task DisposeOperationPipelineIfOpenAsync()
        {
            if (_operationPipeline.IsOpen)
            {
                await _operationPipeline.DisposeAsync();
            }
        }

        private async Task HandleNonSuccessfulExecutions(UserCodeExecutionResult executionResult)
        {
            double elapsed = (DateTime.UtcNow - _executionStartTime).TotalSeconds;

            if (_codeExecutionState == CodeExecutionState.ForceStopped)
            {
                ExecutionStatusText = $"Изпълнението беше спряно ({FormatElapsed(elapsed)})";
                return;
            }

            if (executionResult.Status == UserCodeExecutionStatus.FailedToStart)
            {
                ExecutionStatusText = $"Не можа да стартира ({FormatElapsed(elapsed)})";

                string errorMessage =
                    !string.IsNullOrWhiteSpace(executionResult.StandardError)
                        ? executionResult.StandardError
                        : executionResult.FailureMessage ?? "Програмата не можа да бъде стартирана.";

                await MessageBox.ShowAsync(
                    "Грешка при стартиране",
                    errorMessage,
                    MessageBoxButtons.OkCopy,
                    MessageBoxIcon.Error);

                return;
            }

            if (executionResult.Status == UserCodeExecutionStatus.RuntimeError)
            {
                ExecutionStatusText = $"Завърши за {FormatElapsed(elapsed)} с грешка";

                string errorMessage =
                    !string.IsNullOrWhiteSpace(executionResult.StandardError)
                        ? executionResult.StandardError
                        : executionResult.FailureMessage ?? "Възникна грешка по време на изпълнение.";

                await MessageBox.ShowAsync(
                    "Грешка при изпълнение",
                    errorMessage,
                    MessageBoxButtons.OkCopy,
                    MessageBoxIcon.Error);
            }
        }

        private void StartExecutionTimer()
        {
            _executionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _executionStartTime = DateTime.UtcNow;

            ExecutionStatusText = $"Изпълнява се… ({FormatElapsed(0)})";

            _executionTimer.Tick += (_, _) =>
            {
                double elapsed = (DateTime.UtcNow - _executionStartTime).TotalSeconds;
                ExecutionStatusText = $"Изпълнява се… ({FormatElapsed(elapsed)})";
            };

            _executionTimer.Start();
        }

        private static string FormatElapsed(double totalSeconds)
        {
            TimeSpan elapsed = TimeSpan.FromSeconds(totalSeconds);

            int minutes = (int)elapsed.TotalMinutes;
            int seconds = elapsed.Seconds;
            int milliseconds = elapsed.Milliseconds;

            if (minutes > 0)
            {
                return $"{minutes} мин {seconds} сек {milliseconds} ms";
            }

            if (seconds > 0)
            {
                return $"{seconds} сек {milliseconds} ms";
            }

            return $"{milliseconds} ms";
        }

        private void StopExecutionTimer()
        {
            _executionTimer?.Stop();
            _executionTimer = null;
        }

        public async Task AttachCodeEditor(CodeEditor codeEditor)
        {
            _codeEditor = codeEditor;

            string initialCode = await _projectManager.GetUserCodeAsync();
            _codeEditor.SetCode(initialCode);

            codeEditor.DiagnosticsUpdated += DiagnosticsUpdated;
            codeEditor.CodeContentChanged += OnCodeContentChanged;
        }

        private void DiagnosticsUpdated(IReadOnlyList<Diagnostic> diagnostics)
        {
            Errors.Clear();

            foreach (var diagnostic in diagnostics)
            {
                var span = diagnostic.Location.GetLineSpan();

                int line = span.StartLinePosition.Line + 1;
                int column = span.StartLinePosition.Character + 1;

                Errors.Add(new EditorError(
                    diagnostic.Id,
                    diagnostic.GetMessage(),
                    line,
                    column
                ));
            }
        }

        public void AttachConsoleControl(ConsoleControl consoleControl)
        {
            _consoleControl = consoleControl;
        }

        public void AttachStorageProvider(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
        }

        public void AttachClipboard(IClipboard clipboard)
        {
            _clipboard = clipboard;
        }
    }

    internal enum LayoutMode 
    { 
        EditorOnly,
        Normal,
        CanvasOnly
    }

    internal enum BottomPanelMode 
    { 
        ErrorList,
        Output
    }

    internal enum CodeExecutionState
    {
        Idle,
        Preparing,
        Running,
        ForceStopped,
        Finalizing
    }
}
