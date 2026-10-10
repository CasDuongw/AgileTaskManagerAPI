import os

with open('AgileTaskManager.Desktop/DashboardWindow.xaml.cs', 'r', encoding='utf-8-sig') as f:
    code = f.read()

old_response = '''        public class ColumnResponse
        {
            public int columnId { get; set; }
            public string columnName { get; set; }
            public int orderIndex { get; set; }
            public int projectId { get; set; }
        }'''

new_response = '''        public class ColumnResponse
        {
            public int columnId { get; set; }
            public string columnName { get; set; }
            public int orderIndex { get; set; }
            public int projectId { get; set; }
            public DateTime? dueDate { get; set; }
            public string status { get; set; }
        }'''

code = code.replace(old_response, new_response)

old_init = '''                    var colVm = new KanbanColumnViewModel
                    {
                        ColumnId = col.columnId,
                        ColumnName = col.columnName,
                        ProjectId = projectId
                    };'''

new_init = '''                    var colVm = new KanbanColumnViewModel
                    {
                        ColumnId = col.columnId,
                        ColumnName = col.columnName,
                        ProjectId = projectId,
                        OrderIndex = col.orderIndex,
                        DueDate = col.dueDate,
                        Status = string.IsNullOrEmpty(col.status) ? "V?a giao" : col.status
                    };'''

code = code.replace(old_init, new_init)

with open('AgileTaskManager.Desktop/DashboardWindow.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.write(code)

