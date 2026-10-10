import os

with open('AgileTaskManager.Desktop/ViewModels/KanbanViewModels.cs', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    new_lines.append(line)
    if 'public ICommand DeleteColumnCommand { get; set; }' in line:
        new_lines.append('        public System.Windows.Input.ICommand EditColumnTitleCommand { get; set; }\n')
        new_lines.append('        public KanbanColumnViewModel()\n')
        new_lines.append('        {\n')
        new_lines.append('            EditColumnTitleCommand = new AgileTaskManager.Desktop.Commands.RelayCommand(ExecuteEditColumnTitle);\n')
        new_lines.append('        }\n')
        new_lines.append('        private void ExecuteEditColumnTitle(object parameter)\n')
        new_lines.append('        {\n')
        new_lines.append('            IsEditingTitle = true;\n')
        new_lines.append('        }\n')

with open('AgileTaskManager.Desktop/ViewModels/KanbanViewModels.cs', 'w', encoding='utf-8-sig') as f:
    f.writelines(new_lines)
