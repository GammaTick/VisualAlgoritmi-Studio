using System;

namespace VisualAlgoritmi_Studio.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel = null!;

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (_currentViewModel is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                    
                SetProperty(ref _currentViewModel, value);
            }
        }

        public MainWindowViewModel()
        {
            CurrentViewModel = new HomeViewModel(this);
        }
    }
}
