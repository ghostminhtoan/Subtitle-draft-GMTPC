Imports System.Collections.Generic
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Namespace Services
    ''' <summary>
    ''' Service dịch thuật Google Translate đơn giản, ổn định
    ''' </summary>
    Public Class TranslateService
        Private Shared ReadOnly UserAgent As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"

        ''' <summary>
        ''' Dịch text đơn giản
        ''' </summary>
        Public Shared Async Function TranslateTextAsync(text As String, sourceLang As String, targetLang As String) As Task(Of String)
            If String.IsNullOrWhiteSpace(text) Then Return String.Empty

            Try
                Using client = New HttpClient()
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent)
                    client.Timeout = TimeSpan.FromSeconds(30)

                    Dim encodedText = Uri.EscapeDataString(text)
                    Dim url = String.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}",
                        sourceLang, targetLang, encodedText)

                    Dim response = Await client.GetAsync(url)
                    If response.IsSuccessStatusCode Then
                        Dim json = Await response.Content.ReadAsStringAsync()
                        Return ExtractTranslations(json)
                    Else
                        Return String.Format("[HTTP {0}]", response.StatusCode)
                    End If
                End Using
            Catch ex As TaskCanceledException
                Return "[Timeout]"
            Catch ex As Exception
                Return String.Format("[Lỗi: {0}]", ex.Message)
            End Try
        End Function

        ''' <summary>
        ''' Extract bản dịch từ JSON Google Translate
        ''' Response: [[["dịch1","gốc1",...],["dịch2","gốc2",...]],null,"en"]
        ''' Regex match: match 0=dịch1, match 1=gốc1, match 2=dịch2, match 3=gốc2...
        ''' Lấy match chẵn: 0, 2, 4... (translation)
        ''' </summary>
        Private Shared Function ExtractTranslations(json As String) As String
            If String.IsNullOrWhiteSpace(json) Then Return String.Empty

            Try
                Dim sb = New StringBuilder()
                Dim found As Boolean = False

                ' Pattern tìm string đầu tiên sau [" trong JSON
                Dim pattern = "\[""((?:[^""\\]|\\.)*)"""
                Dim matches = Regex.Matches(json, pattern)

                ' Lấy match ở vị trí chẵn: 0, 2, 4... (translation)
                ' Bỏ match lẻ: 1, 3, 5... (original text)
                Dim idx As Integer = 0
                While idx < matches.Count
                    Dim text = matches(idx).Groups(1).Value
                    text = text.Replace("\n", vbLf).Replace("\r", vbCr).Replace("\t", vbTab).Replace("\""", """").Replace("\\", "\")

                    If found Then sb.Append(vbLf)
                    sb.Append(text)
                    found = True

                    idx += 2 ' Nhảy 2: bỏ original, lấy translation tiếp theo
                End While

                If found Then Return sb.ToString()
                Return json.Substring(0, Math.Min(200, json.Length))

            Catch
                Return json
            End Try
        End Function
    End Class
End Namespace
