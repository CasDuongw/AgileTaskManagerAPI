import os

with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    if 'string dayName = i == 6' in line:
        new_lines.append('                string dayName = i == 6 ? \"Ch? nh?t\" : $\"Th? {i + 2}\";\n')
    elif 'lblWeekInfo.Text =' in line:
        new_lines.append('            lblWeekInfo.Text = $\"Tu?n ({_viewModel.CurrentWeekStart:dd/MM} - {_viewModel.CurrentWeekStart.AddDays(6):dd/MM})\";\n')
    elif 'CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });' in line:
        new_lines.append('                CalendarGrid.RowDefinitions.Add(new RowDefinition { MinHeight = 100, Height = GridLength.Auto });\n')
    elif 'CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 0 ? new GridLength(80)' in line:
        new_lines.append('                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = i == 0 ? 80 : 120, Width = i == 0 ? new GridLength(80) : new GridLength(1, GridUnitType.Star) });\n')
    else:
        new_lines.append(line)

with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.writelines(new_lines)
