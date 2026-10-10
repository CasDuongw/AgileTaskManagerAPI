import os
with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'r', encoding='utf-8-sig') as f:
    c = f.read()

c = c.replace('private void TxtColumnName_KeyDown(object sender, KeyEventArgs e)', 'private void TxtColumnName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)')
c = c.replace('if (e.Key == Key.Enter)', 'if (e.Key == System.Windows.Input.Key.Enter)')
c = c.replace('Keyboard.ClearFocus();', 'System.Windows.Input.Keyboard.ClearFocus();')
c = c.replace('new TraversalRequest(FocusNavigationDirection.Next)', 'new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next)')

with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.write(c)

with open('AgileTaskManager.Desktop/KanbanColumn.xaml.cs', 'r', encoding='utf-8-sig') as f:
    c = f.read()

c = c.replace('private void TxtTitle_KeyDown(object sender, KeyEventArgs e)', 'private void TxtTitle_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)')
c = c.replace('if (e.Key == Key.Enter)', 'if (e.Key == System.Windows.Input.Key.Enter)')
c = c.replace('Keyboard.ClearFocus();', 'System.Windows.Input.Keyboard.ClearFocus();')
c = c.replace('new TraversalRequest(FocusNavigationDirection.Next)', 'new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next)')

with open('AgileTaskManager.Desktop/KanbanColumn.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.write(c)

