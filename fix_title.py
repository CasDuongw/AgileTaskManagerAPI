import os
with open('AgileTaskManager.Desktop/KanbanColumn.xaml.cs', 'r', encoding='utf-8-sig') as f:
    c = f.read()

handler = '''        private async void TxtTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is KanbanColumnViewModel vm)
            {
                var updatePayload = new
                {
                    ColumnId = vm.ColumnId,
                    ColumnName = vm.ColumnName,
                    OrderIndex = vm.OrderIndex,
                    ProjectId = vm.ProjectId,
                    DueDate = vm.DueDate,
                    Status = vm.Status,
                    IsActive = true
                };
                var apiService = (Application.Current as App)?.ServiceProvider.GetRequiredService<AgileTaskManager.Desktop.Services.ApiService>();
                if (apiService != null)
                {
                    await apiService.UpdateColumnAsync(vm.ColumnId, updatePayload);
                }
            }
        }

        private void TxtTitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Unfocus the textbox to trigger LostFocus
                Keyboard.ClearFocus();
                var uiElement = e.OriginalSource as UIElement;
                if (uiElement != null)
                {
                    uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }
'''

c = c.replace('public KanbanColumn()', handler + '\n        public KanbanColumn()')
with open('AgileTaskManager.Desktop/KanbanColumn.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.write(c)
