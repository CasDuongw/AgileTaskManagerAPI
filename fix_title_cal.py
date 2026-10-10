import os
with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'r', encoding='utf-8-sig') as f:
    c = f.read()

handler = '''        private async void TxtColumnName_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb?.DataContext is KanbanColumnViewModel vm)
            {
                await UpdateColumnApiAsync(vm);
            }
        }

        private void TxtColumnName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                var uiElement = e.OriginalSource as UIElement;
                if (uiElement != null)
                {
                    uiElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }
'''

c = c.replace('public CalendarView(KanbanBoardViewModel boardVm)', handler + '\n        public CalendarView(KanbanBoardViewModel boardVm)')
with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.write(c)
