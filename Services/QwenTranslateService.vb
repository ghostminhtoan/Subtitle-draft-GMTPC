Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Namespace Services
    ''' <summary>
    ''' Service dịch thuật qua chat.qwen.ai sử dụng browser automation
    ''' </summary>
    Public Class QwenTranslateService

        Private Shared ReadOnly UserAgent As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"

        ''' <summary>
        ''' Dịch subtitle qua chat.qwen.ai
        ''' </summary>
        Public Shared Async Function TranslateAsync(subtitleText As String, prompt As String, cookies As String) As Task(Of String)
            If String.IsNullOrWhiteSpace(subtitleText) Then Return "Không có nội dung để dịch."
            If String.IsNullOrWhiteSpace(cookies) Then Return "Vui lòng nhập cookies trình duyệt."

            Try
                Dim cleanCookies As String = ParseCookies(cookies)
                If cleanCookies.StartsWith("[") Then
                    Return cleanCookies ' Parse error
                End If

                Dim fullPrompt As String = prompt & vbCrLf & vbCrLf & subtitleText
                Dim escapedPrompt = EscapeJson(fullPrompt)
                Dim jsonBody = String.Format("{{""model"":""qwen3.6-plus"",""messages"":[{{""role"":""user"",""content"":""{0}""}}],""temperature"":0.3,""max_tokens"":8000,""stream"":false}}", escapedPrompt)

                Dim content = New StringContent(jsonBody, Encoding.UTF8, "application/json")

                Using client As New HttpClient()
                    client.DefaultRequestHeaders.Clear()
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent)
                    client.DefaultRequestHeaders.Add("Accept", "*/*")
                    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9")
                    client.DefaultRequestHeaders.Add("Origin", "https://chat.qwen.ai")
                    client.DefaultRequestHeaders.Add("Referer", "https://chat.qwen.ai/")
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty")
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors")
                    client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin")
                    client.DefaultRequestHeaders.Add("Cookie", cleanCookies)
                    client.Timeout = TimeSpan.FromMinutes(3)

                    ' Thử nhiều endpoint
                    Dim endpoints = {
                        "https://chat.qwen.ai/api/chat/completions",
                        "https://chat.qwen.ai/v1/chat/completions",
                        "https://chat.qwen.ai/api/generate"
                    }

                    For Each endpoint In endpoints
                        Try
                            Dim clonedContent = New StringContent(content.ReadAsStringAsync().Result, Encoding.UTF8, "application/json")
                            Dim response = Await client.PostAsync(endpoint, clonedContent)
                            Dim responseJson = Await response.Content.ReadAsStringAsync()

                            If response.IsSuccessStatusCode Then
                                Dim result = ExtractResponseContent(responseJson)
                                If Not String.IsNullOrEmpty(result) AndAlso Not result.StartsWith("[") Then
                                    Return result
                                End If
                            End If
                        Catch
                            ' Thử endpoint tiếp theo
                        End Try
                    Next

                    Return "[Lỗi] Không thể kết nối đến chat.qwen.ai. Vui lòng kiểm tra cookies hoặc thử lại sau."

                End Using

            Catch ex As TaskCanceledException
                Return "[Timeout] Yêu cầu dịch thuật hết thời gian chờ (3 phút)."
            Catch ex As Exception
                Return String.Format("[Lỗi: {0}]", ex.Message)
            End Try
        End Function

        ''' <summary>
        ''' Parse cookies từ JSON array hoặc Cookie header string
        ''' </summary>
        Private Shared Function ParseCookies(rawInput As String) As String
            If String.IsNullOrWhiteSpace(rawInput) Then Return ""

            Dim trimmed = rawInput.Trim()
            If trimmed.StartsWith("[") AndAlso trimmed.EndsWith("]") Then
                Return ParseJsonCookiesArray(trimmed)
            End If

            Return CleanCookieHeader(trimmed)
        End Function

        ''' <summary>
        ''' Parse JSON cookies array
        ''' </summary>
        Private Shared Function ParseJsonCookiesArray(jsonArray As String) As String
            Try
                Dim cookieDict = New Dictionary(Of String, String)()
                Dim objPattern = "\{[^{}]*""name""\s*:\s*""([^""]+)""[^{}]*""value""\s*:\s*""([^""]+)""[^{}]*\}"
                Dim objMatches = Regex.Matches(jsonArray, objPattern, RegexOptions.Singleline)

                For Each objMatch As Match In objMatches
                    Dim name = objMatch.Groups(1).Value
                    Dim value = objMatch.Groups(2).Value

                    ' Chỉ lấy cookies quan trọng
                    If name = "qwen-theme" OrElse name = "qwen-locale" OrElse name = "x-ap" OrElse
                       name = "_gcl_au" OrElse name = "_bl_uid" OrElse name = "cnaui" OrElse
                       name = "ssxmod_itna" OrElse name = "ssxmod_itna2" OrElse name = "atpsida" OrElse
                       name = "sca" Then
                        Continue For
                    End If

                    cookieDict(name) = value
                Next

                Dim sb = New StringBuilder()
                Dim found As Boolean = False

                If cookieDict.ContainsKey("token") Then
                    sb.AppendFormat("token={0}", cookieDict("token"))
                    found = True
                End If

                For Each kvp In cookieDict
                    If kvp.Key = "token" Then Continue For
                    If found Then sb.Append("; ")
                    sb.AppendFormat("{0}={1}", kvp.Key, kvp.Value)
                    found = True
                Next

                Return sb.ToString()

            Catch ex As Exception
                Return String.Format("[Parse error: {0}]", ex.Message)
            End Try
        End Function

        ''' <summary>
        ''' Làm sạch Cookie header
        ''' </summary>
        Private Shared Function CleanCookieHeader(rawCookies As String) As String
            Dim cleaned = rawCookies
            If cleaned.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase) Then
                cleaned = cleaned.Substring(7).Trim()
            ElseIf cleaned.StartsWith("Cookie: ", StringComparison.OrdinalIgnoreCase) Then
                cleaned = cleaned.Substring(8).Trim()
            End If

            cleaned = cleaned.Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbCr, "")
            cleaned = Regex.Replace(cleaned, "\s+", " ").Trim()
            cleaned = Regex.Replace(cleaned, "\s*;\s*", "; ").Trim()
            cleaned = Regex.Replace(cleaned, ";+\s*", "; ").Trim()

            If cleaned.EndsWith(";") Then
                cleaned = cleaned.TrimEnd(";"c).Trim()
            End If

            Return cleaned
        End Function

        ''' <summary>
        ''' Escape JSON string
        ''' </summary>
        Private Shared Function EscapeJson(input As String) As String
            If String.IsNullOrEmpty(input) Then Return ""
            Return input.Replace("\", "\\").Replace("""", "\""").Replace(vbCr, "\r").Replace(vbLf, "\n").Replace(vbTab, "\t")
        End Function

        ''' <summary>
        ''' Extract response content
        ''' </summary>
        Private Shared Function ExtractResponseContent(json As String) As String
            Try
                Dim pattern = """content""\s*:\s*""((?:[^""\\]|\\.)*)"""
                Dim matches = Regex.Matches(json, pattern)

                If matches.Count > 0 Then
                    Dim lastMatch = matches(matches.Count - 1)
                    Dim content = lastMatch.Groups(1).Value
                    content = content.Replace("\n", vbLf).Replace("\r", vbCr).Replace("\t", vbTab).Replace("\""", """").Replace("\\", "\")
                    Return content
                End If

                Return ""
            Catch
                Return ""
            End Try
        End Function
    End Class
End Namespace
