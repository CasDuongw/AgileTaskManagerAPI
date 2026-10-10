import os
with open('AgileTaskManager.Desktop/CalendarView.xaml', 'r', encoding='utf-8-sig') as f:
    c = f.read()

c = c.replace('<ScrollViewer VerticalScrollBarVisibility=\"Auto\">', '<ScrollViewer VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Auto\">')

with open('AgileTaskManager.Desktop/CalendarView.xaml', 'w', encoding='utf-8-sig') as f:
    f.write(c)
