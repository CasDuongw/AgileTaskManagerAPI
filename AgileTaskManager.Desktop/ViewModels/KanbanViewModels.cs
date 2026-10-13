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

    public class SubtaskViewModel : ViewModelBase
    {
        private int _subtaskId;
        public int SubtaskId { get => _subtaskId; set { _subtaskId = value; OnPropertyChanged(); } }

        private string _title = string.Empty;
        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

        private bool _isCompleted;
        public bool IsCompleted { get => _isCompleted; set { _isCompleted = value; OnPropertyChanged(); } }
    }

    public class KanbanTaskViewModel : ViewModelBase
    {
        private int _taskId;
        public int TaskId
        {
            get => _taskId;
            set { _taskId = value; OnPropertyChanged(); }
        }

        private string _taskName = string.Empty;
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

        private int? _assigneeId;
        public int? AssigneeId
        {
            get => _assigneeId;
            set { _assigneeId = value; OnPropertyChanged(); }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(); }
        }

        private int _orderIndex;
        public int OrderIndex
        {
            get => _orderIndex;
            set { _orderIndex = value; OnPropertyChanged(); }
        }

        private string _tags = string.Empty;
        public string Tags
        {
            get => _tags;
            set { _tags = value; OnPropertyChanged(); }
        }

        // Tương thích hiển thị Calendar
        public string ColumnName
        {
            get => _taskName;
            set { TaskName = value; }
        }

        public string DisplayTitle => _taskName;

        private bool _isEditingTitle;
        public bool IsEditingTitle
        {
            get => _isEditingTitle;
            set { _isEditingTitle = value; OnPropertyChanged(); }
        }

        private string _status = "Vừa giao";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Statuses { get; } = new ObservableCollection<string> { "Vừa giao", "Đang làm", "Hoàn thành", "Trễ hạn" };

        public ICommand? EditColumnTitleCommand { get; set; }
        public ICommand? EditTitleCommand { get; set; }

        public KanbanTaskViewModel()
        {
            EditColumnTitleCommand = new AgileTaskManager.Desktop.Commands.RelayCommand(() => IsEditingTitle = true);
            EditTitleCommand = EditColumnTitleCommand;
        }

        public ICommand? DeleteTaskCommand { get; set; }
        public ICommand? EditTaskCommand { get; set; }

        public ObservableCollection<SubtaskViewModel> Subtasks { get; } = new ObservableCollection<SubtaskViewModel>();
    }

    public class KanbanColumnViewModel : ViewModelBase
    {
        private int _columnId;
        public int ColumnId
        {
            get => _columnId;
            set { _columnId = value; OnPropertyChanged(); }
        }

        private string _columnName = string.Empty;
        public string ColumnName
        {
            get => _columnName;
            set { _columnName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public string DisplayTitle => _columnName;

        private bool _isEditingTitle;
        public bool IsEditingTitle
        {
            get => _isEditingTitle;
            set { _isEditingTitle = value; OnPropertyChanged(); }
        }

        private int _projectId;
        public int ProjectId
        {
            get => _projectId;
            set { _projectId = value; OnPropertyChanged(); }
        }

        public ObservableCollection<KanbanTaskViewModel> Tasks { get; } = new ObservableCollection<KanbanTaskViewModel>();

        public ICommand? DeleteColumnCommand { get; set; }
        public ICommand? EditColumnTitleCommand { get; set; }
        public ICommand? EditTitleCommand => EditColumnTitleCommand;

        public KanbanColumnViewModel()
        {
            EditColumnTitleCommand = new AgileTaskManager.Desktop.Commands.RelayCommand(ExecuteEditColumnTitle);
        }
        private void ExecuteEditColumnTitle()
        {
            IsEditingTitle = true;
        }

        private int _orderIndex;
        public int OrderIndex { get => _orderIndex; set { _orderIndex = value; OnPropertyChanged(); } }

        private DateTime? _dueDate;
        public DateTime? DueDate { get => _dueDate; set { _dueDate = value; OnPropertyChanged(); } }

        public ObservableCollection<string> Statuses { get; } = new ObservableCollection<string> { "Vừa giao", "Đang làm", "Hoàn thành", "Trễ hạn" };

        private string _status = "Vừa giao";
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
    }

    public class KanbanBoardViewModel : ViewModelBase
    {
        public ObservableCollection<KanbanColumnViewModel> Columns { get; } = new ObservableCollection<KanbanColumnViewModel>();
    }

    public class CalendarCellViewModel : ViewModelBase
    {
        public DateTime SlotTime { get; set; }
        public ObservableCollection<ViewModelBase> Tasks { get; } = new ObservableCollection<ViewModelBase>();
    }

    public class CalendarViewModel : ViewModelBase
    {
        public ObservableCollection<CalendarCellViewModel> Cells { get; } = new ObservableCollection<CalendarCellViewModel>();
        public ObservableCollection<KanbanColumnViewModel>? Columns { get; set; }
        public ObservableCollection<string> Statuses { get; } = new ObservableCollection<string> { "Vừa giao", "Đang làm", "Hoàn thành", "Trễ hạn" };
        public ObservableCollection<ViewModelBase> UnscheduledTasks { get; } = new ObservableCollection<ViewModelBase>();
        
        private DateTime _currentWeekStart;
        public DateTime CurrentWeekStart 
        { 
            get => _currentWeekStart; 
            set { _currentWeekStart = value; OnPropertyChanged(); } 
        }
    }
}
