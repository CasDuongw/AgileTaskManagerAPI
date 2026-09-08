using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AgileTaskManager.Desktop.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class KanbanTaskViewModel : ViewModelBase
    {
        private int _taskId;
        public int TaskId
        {
            get => _taskId;
            set { _taskId = value; OnPropertyChanged(); }
        }

        private string _taskName;
        public string TaskName
        {
            get => _taskName;
            set { _taskName = value; OnPropertyChanged(); }
        }

        private int _columnId;
        public int ColumnId
        {
            get => _columnId;
            set { _columnId = value; OnPropertyChanged(); }
        }
    }

    public class KanbanColumnViewModel : ViewModelBase
    {
        private int _columnId;
        public int ColumnId
        {
            get => _columnId;
            set { _columnId = value; OnPropertyChanged(); }
        }

        private string _columnName;
        public string ColumnName
        {
            get => _columnName;
            set { _columnName = value; OnPropertyChanged(); }
        }

        private int _projectId;
        public int ProjectId
        {
            get => _projectId;
            set { _projectId = value; OnPropertyChanged(); }
        }

        public ObservableCollection<KanbanTaskViewModel> Tasks { get; } = new ObservableCollection<KanbanTaskViewModel>();
    }

    public class KanbanBoardViewModel : ViewModelBase
    {
        public ObservableCollection<KanbanColumnViewModel> Columns { get; } = new ObservableCollection<KanbanColumnViewModel>();
    }
}
