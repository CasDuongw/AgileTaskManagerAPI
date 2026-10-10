using AgileTaskManager.Desktop.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System;

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

        private int _orderIndex;
        public int OrderIndex
        {
            get => _orderIndex;
            set { _orderIndex = value; OnPropertyChanged(); }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private int? _assigneeId;
        public int? AssigneeId
        {
            get => _assigneeId;
            set { _assigneeId = value; OnPropertyChanged(); }
        }

        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(); }
        }

        private string _tags;
        public string Tags
        {
            get => _tags;
            set { _tags = value; OnPropertyChanged(); }
        }

        public ICommand DeleteTaskCommand { get; set; }
        public ICommand EditTaskCommand { get; set; }
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

        public ICommand DeleteColumnCommand { get; set; }
    }

    public class KanbanBoardViewModel : ViewModelBase
    {
        public ObservableCollection<KanbanColumnViewModel> Columns { get; } = new ObservableCollection<KanbanColumnViewModel>();
    }

    public class CalendarCellViewModel : ViewModelBase
    {
        public DateTime SlotTime { get; set; }
        public ObservableCollection<KanbanTaskViewModel> Tasks { get; } = new ObservableCollection<KanbanTaskViewModel>();
    }

    public class CalendarViewModel : ViewModelBase
    {
        public ObservableCollection<CalendarCellViewModel> Cells { get; } = new ObservableCollection<CalendarCellViewModel>();
        public ObservableCollection<KanbanColumnViewModel> Columns { get; set; }
        public ObservableCollection<KanbanTaskViewModel> UnscheduledTasks { get; } = new ObservableCollection<KanbanTaskViewModel>();
        
        private DateTime _currentWeekStart;
        public DateTime CurrentWeekStart 
        { 
            get => _currentWeekStart; 
            set { _currentWeekStart = value; OnPropertyChanged(); } 
        }
    }
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) { _execute = execute; }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }
}
