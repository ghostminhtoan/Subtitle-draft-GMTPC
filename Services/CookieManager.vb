Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Namespace Services
    ''' <summary>
    ''' Quản lý cookies cho WebView2
    ''' Lưu/Load cookies từ JSON array hoặc Cookie header string
    ''' </summary>
    Public Class CookieManager

        ''' <summary>
        ''' Parse cookies từ JSON array hoặc Cookie header string thành list
        ''' </summary>
        Public Shared Function ParseCookiesToDictionary(rawInput As String) As Dictionary(Of String, String)
            Dim result = New Dictionary(Of String, String)()
            If String.IsNullOrWhiteSpace(rawInput) Then Return result

            Dim trimmed = rawInput.Trim()

            If trimmed.StartsWith("[") AndAlso trimmed.EndsWith("]") Then
                Return ParseJsonCookiesArray(trimmed)
            End If

            Return ParseCookieHeader(trimmed)
        End Function

        ''' <summary>
        ''' Parse JSON cookies array
        ''' </summary>
        Private Shared Function ParseJsonCookiesArray(jsonArray As String) As Dictionary(Of String, String)
            Dim result = New Dictionary(Of String, String)()
            Try
                Dim objPattern = "\{[^{}]*""name""\s*:\s*""([^""]+)""[^{}]*""value""\s*:\s*""([^""]+)""[^{}]*\}"
                Dim objMatches = Regex.Matches(jsonArray, objPattern, RegexOptions.Singleline)

                For Each objMatch As Match In objMatches
                    Dim name = objMatch.Groups(1).Value
                    Dim value = objMatch.Groups(2).Value

                    ' Chỉ lấy cookies quan trọng cho chat.qwen.ai
                    If name = "qwen-theme" OrElse name = "qwen-locale" OrElse name = "x-ap" OrElse
                       name = "_gcl_au" OrElse name = "_bl_uid" OrElse name = "cnaui" OrElse
                       name = "ssxmod_itna" OrElse name = "ssxmod_itna2" OrElse name = "atpsida" OrElse
                       name = "sca" Then
                        Continue For
                    End If

                    result(name) = value
                Next
            Catch
            End Try

            Return result
        End Function

        ''' <summary>
        ''' Parse Cookie header string
        ''' </summary>
        Private Shared Function ParseCookieHeader(cookieHeader As String) As Dictionary(Of String, String)
            Dim result = New Dictionary(Of String, String)()
            If String.IsNullOrWhiteSpace(cookieHeader) Then Return result

            Dim cleaned = cookieHeader.Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbCr, "")
            Dim pairs = cleaned.Split(";"c)

            For Each pair In pairs
                Dim trimmedPair = pair.Trim()
                Dim eqIndex = trimmedPair.IndexOf("="c)
                If eqIndex > 0 Then
                    Dim name = trimmedPair.Substring(0, eqIndex).Trim()
                    Dim value = trimmedPair.Substring(eqIndex + 1).Trim()
                    result(name) = value
                End If
            Next

            Return result
        End Function

        ''' <summary>
        ''' Chuyển dictionary cookies thành Cookie header string
        ''' </summary>
        Public Shared Function ToCookieHeader(cookies As Dictionary(Of String, String)) As String
            Dim sb = New StringBuilder()
            Dim found As Boolean = False

            ' Token trước
            If cookies.ContainsKey("token") Then
                sb.AppendFormat("token={0}", cookies("token"))
                found = True
            End If

            For Each kvp In cookies
                If kvp.Key = "token" Then Continue For
                If found Then sb.Append("; ")
                sb.AppendFormat("{0}={1}", kvp.Key, kvp.Value)
                found = True
            Next

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Tạo JavaScript để inject cookies vào trang
        ''' </summary>
        Public Shared Function GenerateCookieInjectScript(cookies As Dictionary(Of String, String)) As String
            Dim sb = New StringBuilder()
            sb.AppendLine("(function() {")
            sb.AppendLine("  var cookies = {")

            Dim first = True
            For Each kvp In cookies
                If Not first Then sb.AppendLine(",")
                sb.AppendFormat("    ""{0}"": ""{1}""", EscapeJs(kvp.Key), EscapeJs(kvp.Value))
                first = False
            Next

            sb.AppendLine()
            sb.AppendLine("  };")
            sb.AppendLine("  for (var key in cookies) {")
            sb.AppendLine("    document.cookie = key + '=' + cookies[key] + '; path=/; domain=.qwen.ai';")
            sb.AppendLine("    document.cookie = key + '=' + cookies[key] + '; path=/; domain=chat.qwen.ai';")
            sb.AppendLine("  }")
            sb.AppendLine("  return 'Đã inject ' + Object.keys(cookies).length + ' cookies';")
            sb.AppendLine("})();")

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Tạo JavaScript để điền prompt và subtitle vào ô input
        ''' </summary>
        Public Shared Function GenerateInputScript(prompt As String, subtitleText As String) As String
            Dim combinedText = prompt & vbCrLf & vbCrLf & subtitleText
            Dim escapedText = EscapeJs(combinedText)

            Dim sb = New StringBuilder()
            sb.AppendLine("(function() {")
            sb.AppendLine("  var textarea = document.querySelector('textarea');")
            sb.AppendLine("  if (!textarea) return 'Không tìm thấy ô nhập liệu';")
            sb.AppendLine("  textarea.value = '" & escapedText & "';")
            sb.AppendLine("  textarea.dispatchEvent(new Event('input', { bubbles: true }));")
            sb.AppendLine("  textarea.dispatchEvent(new Event('change', { bubbles: true }));")
            sb.AppendLine("  return 'Đã điền " & combinedText.Length & " ký tự vào ô nhập liệu';")
            sb.AppendLine("})();")

            Return sb.ToString()
        End Function

        Private Shared Function EscapeJs(input As String) As String
            Return input.Replace("\", "\\").Replace("'", "\'").Replace(vbCr, "\n").Replace(vbLf, "\n").Replace(vbTab, "    ")
        End Function
    End Class
End Namespace
