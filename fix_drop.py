import os
with open('AgileTaskManager.Desktop/CalendarView.xaml', 'r', encoding='utf-8-sig') as f:
    c = f.read()

c = c.replace('<TextBlock Grid.Row=\"0\" Text=\"Chua x?p l?ch\" FontWeight=\"Bold\" FontSize=\"14\" Foreground=\"#475569\" Margin=\"0,0,0,10\"/>', 
              '<TextBlock x:Name=\"txtUnscheduledHeader\" Grid.Row=\"0\" Text=\"Chua x?p l?ch\" FontWeight=\"Bold\" FontSize=\"14\" Foreground=\"#475569\" Margin=\"0,0,0,10\" AllowDrop=\"True\" dd:DragDrop.IsDropTarget=\"True\" dd:DragDrop.DropHandler=\"{Binding RelativeSource={RelativeSource AncestorType=UserControl}}\"/>')

with open('AgileTaskManager.Desktop/CalendarView.xaml', 'w', encoding='utf-8-sig') as f:
    f.write(c)
