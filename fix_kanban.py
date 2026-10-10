import os
import re

# Update KanbanColumn.xaml
with open('AgileTaskManager.Desktop/KanbanColumn.xaml', 'r', encoding='utf-8-sig') as f:
    xaml = f.read()

# Replace the TextBox for txtTitle with Grid containing both TextBlock and TextBox
old_title_xaml = '''<TextBox x:Name="txtTitle" Text="{Binding ColumnName, Mode=TwoWay, UpdateSourceTrigger=LostFocus}" LostFocus="TxtTitle_LostFocus" KeyDown="TxtTitle_KeyDown" FontSize="15" FontWeight="Bold" Foreground="#0F172A" Background="Transparent" BorderThickness="0" TextWrapping="Wrap"/>'''
new_title_xaml = '''                    <Grid>
                        <TextBlock Text="{Binding ColumnName}" FontSize="15" FontWeight="Bold" Foreground="#0F172A" TextWrapping="Wrap">
                            <TextBlock.Style>
                                <Style TargetType="TextBlock">
                                    <Setter Property="Visibility" Value="Visible"/>
                                    <Style.Triggers>
                                        <DataTrigger Binding="{Binding IsEditingTitle}" Value="True">
                                            <Setter Property="Visibility" Value="Collapsed"/>
                                        </DataTrigger>
                                    </Style.Triggers>
                                </Style>
                            </TextBlock.Style>
                        </TextBlock>
                        <TextBox x:Name="txtTitle" Text="{Binding ColumnName, Mode=TwoWay, UpdateSourceTrigger=LostFocus}" LostFocus="TxtTitle_LostFocus" KeyDown="TxtTitle_KeyDown" IsVisibleChanged="TxtTitle_IsVisibleChanged" FontSize="15" FontWeight="Bold" Foreground="#0F172A" Background="White" BorderThickness="1" BorderBrush="#3B82F6" TextWrapping="Wrap" Padding="2">
                            <TextBox.Style>
                                <Style TargetType="TextBox">
                                    <Setter Property="Visibility" Value="Collapsed"/>
                                    <Style.Triggers>
                                        <DataTrigger Binding="{Binding IsEditingTitle}" Value="True">
                                            <Setter Property="Visibility" Value="Visible"/>
                                        </DataTrigger>
                                    </Style.Triggers>
                                </Style>
                            </TextBox.Style>
                        </TextBox>
                    </Grid>'''

xaml = xaml.replace(old_title_xaml, new_title_xaml)

# Add Edit button next to Delete button
old_btn = '''<Button Content="?" Width="20" Height="20" Background="Transparent" BorderThickness="0" Foreground="#94A3B8"
                            FontSize="12" FontWeight="Bold" Cursor="Hand" Click="DeleteColumn_Click"/>'''
new_btn = '''<Button Content="?" Width="20" Height="20" Background="Transparent" BorderThickness="0" Foreground="#94A3B8"
                            FontSize="14" FontWeight="Bold" Cursor="Hand" Command="{Binding EditColumnTitleCommand}" Margin="0,0,5,0" ToolTip="S?a tên"/>
                    <Button Content="?" Width="20" Height="20" Background="Transparent" BorderThickness="0" Foreground="#94A3B8"
                            FontSize="12" FontWeight="Bold" Cursor="Hand" Click="DeleteColumn_Click" ToolTip="Xóa"/>'''

xaml = xaml.replace(old_btn, new_btn)

with open('AgileTaskManager.Desktop/KanbanColumn.xaml', 'w', encoding='utf-8-sig') as f:
    f.write(xaml)

# Update KanbanColumn.xaml.cs
with open('AgileTaskManager.Desktop/KanbanColumn.xaml.cs', 'r', encoding='utf-8-sig') as f:
    cs = f.read()

# Add IsEditingTitle = false to TxtTitle_LostFocus
cs = cs.replace('var updatePayload = new', 'vm.IsEditingTitle = false;\n                var updatePayload = new')

# Add TxtTitle_IsVisibleChanged
is_vis_handler = '''        private void TxtTitle_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.Visibility == Visibility.Visible)
            {
                tb.Focus();
                tb.SelectAll();
            }
        }
'''
cs = cs.replace('private void TxtTitle_KeyDown', is_vis_handler + '\n        private void TxtTitle_KeyDown')

with open('AgileTaskManager.Desktop/KanbanColumn.xaml.cs', 'w', encoding='utf-8-sig') as f:
    f.write(cs)

