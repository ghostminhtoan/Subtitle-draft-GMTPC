f = r'r:\HDD R\ZC SYMLINK\USERS\source\repos\ghostminhtoan\Subtitle draft GMTPC\MainWindowTabEffect.vb'
c = open(f, 'r', encoding='utf-8').read()

old_handler = '''    Private Sub BtnColorPicker_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim dlg As New System.Windows.Forms.ColorDialog()
            dlg.FullOpen = True
            If dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
                Dim hex = dlg.Color.B.ToString("X2") & dlg.Color.G.ToString("X2") & dlg.Color.R.ToString("X2")
                Dim tag = "{\\1c&H" & hex & "&}"
                Dim content = TxtEffectInput.Text
                If String.IsNullOrWhiteSpace(content) Then
                    TxtEffectInput.Text = tag & " "
                    TxtEffectOutput.Text = TxtEffectInput.Text
                Else
                    Dim selStart = TxtEffectInput.SelectionStart
                    TxtEffectInput.Text = TxtEffectInput.Text.Insert(selStart, tag & " ")
                    TxtEffectOutput.Text = TxtEffectInput.Text
                End If
                ShowToastEffect("Da them tag mau: " & tag)
            End If
        Catch ex As Exception
            System.Windows.MessageBox.Show("Loi: " & ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
        End Try
    End Sub'''

new_handler = '''    Private Sub BtnColorPicker_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim dlg As New System.Windows.Forms.ColorDialog()
            dlg.FullOpen = True
            If dlg.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
                Dim hex = dlg.Color.B.ToString("X2") & dlg.Color.G.ToString("X2") & dlg.Color.R.ToString("X2")
                Dim hexBox = EffectConfigFields.Children.OfType(Of StackPanel)().
                    SelectMany(Function(sp) sp.Children.OfType(Of TextBox)()).
                    FirstOrDefault(Function(t) t.Tag IsNot Nothing AndAlso t.Tag.ToString().ToLower() = "hex")
                If hexBox IsNot Nothing Then
                    hexBox.Text = hex
                    ShowToastEffect("Da dien hex: " & hex)
                Else
                    ShowToastEffect("Mo 1 effect mau truoc (vi du: \\\\1c)")
                End If
            End If
        Catch ex As Exception
            System.Windows.MessageBox.Show("Loi: " & ex.Message, "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
        End Try
    End Sub'''

if old_handler in c:
    c = c.replace(old_handler, new_handler)
    open(f, 'w', encoding='utf-8').write(c)
    print('Replaced handler successfully')
else:
    print('Old handler not found, searching...')
    # Find the current handler
    idx = c.find('Private Sub BtnColorPicker_Click')
    if idx >= 0:
        print(f'Found at position {idx}')
        print(c[idx:idx+200])
    else:
        print('Handler not found at all')
