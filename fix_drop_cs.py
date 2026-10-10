import os
with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'r', encoding='utf-8-sig') as f:
    c = f.read()

new_logic = '''        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var column = dropInfo.Data as KanbanColumnViewModel;
            if (column == null) return;

            if (dropInfo.VisualTarget == txtUnscheduledHeader)
            {
                var sourceList = dropInfo.DragInfo.SourceCollection as System.Collections.ObjectModel.ObservableCollection<KanbanColumnViewModel>;
                if (sourceList != null && sourceList != _viewModel.UnscheduledTasks)
                {
                    sourceList.Remove(column);
                    _viewModel.UnscheduledTasks.Insert(0, column);
                    column.DueDate = null;
                    _ = UpdateColumnApiAsync(column);
                }
                else if (sourceList == _viewModel.UnscheduledTasks)
                {
                    sourceList.Remove(column);
                    _viewModel.UnscheduledTasks.Insert(0, column);
                }
                return;
            }
'''

c = c.replace('        void IDropTarget.Drop(IDropInfo dropInfo)\n        {\n            var column = dropInfo.Data as KanbanColumnViewModel;\n            if (column == null) return;', new_logic)

with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.write(c)
