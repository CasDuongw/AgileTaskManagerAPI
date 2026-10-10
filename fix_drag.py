import os
with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = [line for line in lines if 'SetDragHandler' not in line]

with open('AgileTaskManager.Desktop/CalendarView.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.writelines(new_lines)
