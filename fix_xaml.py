import os

with open('AgileTaskManager.Desktop/CalendarView.xaml', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    if 'lblWeekInfo' in line:
        new_lines.append('                <TextBlock x:Name=\"lblWeekInfo\" Text=\"Tu?n (09/11 - 15/11)\" FontSize=\"16\" FontWeight=\"Bold\" VerticalAlignment=\"Center\"/>\n')
    elif '&lt; Tu n tr&gt;c' in line or '&lt; Tu n' in line or '&lt; Tu' in line:
        if 'PrevWeek_Click' in line:
            new_lines.append('                <Button Content=\"&lt; Tu?n tru?c\" Margin=\"10,0,10,0\" Padding=\"10,5\" Click=\"PrevWeek_Click\"/>\n')
        else:
            new_lines.append(line)
    elif 'Tu n hi' in line or 'Tu n' in line:
        if 'CurrentWeek_Click' in line:
            new_lines.append('                <Button Content=\"Tu?n hi?n t?i\" Background=\"#3B82F6\" Foreground=\"White\" BorderThickness=\"0\" Padding=\"10,5\" Click=\"CurrentWeek_Click\"/>\n')
        elif 'NextWeek_Click' in line:
            new_lines.append('                <Button Content=\"Tu?n sau &gt;\" Margin=\"10,0,0,0\" Padding=\"10,5\" Click=\"NextWeek_Click\"/>\n')
        else:
            new_lines.append(line)
    elif 'Cha xp lch' in line or 'Ch' in line:
        if 'Grid.Row=\"0\" Text=\"' in line:
            new_lines.append('                    <TextBlock Grid.Row=\"0\" Text=\"Chua x?p l?ch\" FontWeight=\"Bold\" FontSize=\"14\" Foreground=\"#475569\" Margin=\"0,0,0,10\"/>\n')
        else:
            new_lines.append(line)
    else:
        new_lines.append(line)

with open('AgileTaskManager.Desktop/CalendarView.xaml', 'w', encoding='utf-8-sig') as f:
    f.writelines(new_lines)
