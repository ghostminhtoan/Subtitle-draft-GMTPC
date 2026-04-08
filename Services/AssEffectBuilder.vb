Imports System.Text
Imports System.Text.RegularExpressions
Imports Subtitle_draft_GMTPC.Models

Namespace Services

    Public Class AssEffectBuilder

#Region "Tag String Builders"

        Public Shared Function BuildPosition(x As Double, y As Double) As String
            Return "\pos(" & x & "," & y & ")"
        End Function

        Public Shared Function BuildMove(x1 As Double, y1 As Double, x2 As Double, y2 As Double) As String
            Return "\move(" & x1 & "," & y1 & "," & x2 & "," & y2 & ")"
        End Function

        Public Shared Function BuildMoveTimed(x1 As Double, y1 As Double, x2 As Double, y2 As Double, t1 As Integer, t2 As Integer) As String
            Return "\move(" & x1 & "," & y1 & "," & x2 & "," & y2 & "," & t1 & "," & t2 & ")"
        End Function

        Public Shared Function BuildAlignment(n As Integer) As String
            If n < 1 OrElse n > 9 Then Throw New ArgumentException("Alignment must be 1-9")
            Return "\an" & n
        End Function

        Public Shared Function BuildOrigin(x As Double, y As Double) As String
            Return "\org(" & x & "," & y & ")"
        End Function

        Public Shared Function BuildRotation(axis As String, degrees As Double) As String
            Select Case axis.ToLower()
                Case "z", "frz", "fr"
                    Return "\frz" & degrees
                Case "x", "frx"
                    Return "\frx" & degrees
                Case "y", "fry"
                    Return "\fry" & degrees
                Case Else
                    Throw New ArgumentException("Axis must be x, y, or z")
            End Select
        End Function

        Public Shared Function BuildScale(axis As String, percent As Double) As String
            Select Case axis.ToLower()
                Case "x", "fscx"
                    Return "\fscx" & percent
                Case "y", "fscy"
                    Return "\fscy" & percent
                Case Else
                    Throw New ArgumentException("Axis must be x or y")
            End Select
        End Function

        Public Shared Function BuildShear(axis As String, value As Double) As String
            Select Case axis.ToLower()
                Case "x", "fax"
                    Return "\fax" & value
                Case "y", "fay"
                    Return "\fay" & value
                Case Else
                    Throw New ArgumentException("Axis must be x or y")
            End Select
        End Function

        Public Shared Function BuildFontName(fontName As String) As String
            Return "\fn" & fontName
        End Function

        Public Shared Function BuildFontSize(size As Integer) As String
            If size < 1 Then Throw New ArgumentException("Font size must be positive")
            Return "\fs" & size
        End Function

        Public Shared Function BuildBold(weight As Integer) As String
            If weight <= 0 Then Return "\b0"
            Return "\b" & weight
        End Function

        Public Shared Function BuildItalic(isEnabled As Boolean) As String
            If isEnabled Then Return "\i1"
            Return "\i0"
        End Function

        Public Shared Function BuildUnderline(isEnabled As Boolean) As String
            If isEnabled Then Return "\u1"
            Return "\u0"
        End Function

        Public Shared Function BuildStrikeout(isEnabled As Boolean) As String
            If isEnabled Then Return "\s1"
            Return "\s0"
        End Function

        Public Shared Function BuildBorder(size As Double) As String
            Return "\bord" & size
        End Function

        Public Shared Function BuildShadow(size As Double) As String
            Return "\shad" & size
        End Function

        Public Shared Function BuildBlur(level As Double) As String
            Return "\blur" & level
        End Function

        Public Shared Function BuildEdgeBlur(level As Integer) As String
            Return "\be" & level
        End Function

        Public Shared Function BuildColor(component As String, hexBgr As String) As String
            Dim tagPrefix As String
            Select Case component.ToLower()
                Case "1", "1c", "primary", "c"
                    tagPrefix = "\1c&H"
                Case "2", "2c", "secondary"
                    tagPrefix = "\2c&H"
                Case "3", "3c", "border"
                    tagPrefix = "\3c&H"
                Case "4", "4c", "shadow"
                    tagPrefix = "\4c&H"
                Case Else
                    Throw New ArgumentException("Invalid color component")
            End Select
            Dim cleanHex = hexBgr.Trim().Replace("&H", "").Replace("&", "")
            Return tagPrefix & cleanHex & "&"
        End Function

        Public Shared Function RgbToBgrHex(r As Integer, g As Integer, b As Integer) As String
            Return b.ToString("X2") & g.ToString("X2") & r.ToString("X2")
        End Function

        Public Shared Function BuildAlpha(component As String, hexAlpha As String) As String
            Dim tagPrefix As String
            Select Case component.ToLower()
                Case "alpha", "all"
                    tagPrefix = "\alpha&H"
                Case "1a", "1", "primary"
                    tagPrefix = "\1a&H"
                Case "2a", "2", "secondary"
                    tagPrefix = "\2a&H"
                Case "3a", "3", "border"
                    tagPrefix = "\3a&H"
                Case "4a", "4", "shadow"
                    tagPrefix = "\4a&H"
                Case Else
                    Throw New ArgumentException("Invalid alpha component")
            End Select
            Dim cleanHex = hexAlpha.Trim().Replace("&H", "").Replace("&", "")
            Return tagPrefix & cleanHex & "&"
        End Function

        Public Shared Function BuildFade(inTime As Integer, outTime As Integer) As String
            Return "\fad(" & inTime & "," & outTime & ")"
        End Function

        Public Shared Function BuildKaraoke(type As String, durationCentiSec As Integer) As String
            Select Case type.ToLower()
                Case "k"
                    Return "\k" & durationCentiSec
                Case "kf"
                    Return "\kf" & durationCentiSec
                Case "ko"
                    Return "\ko" & durationCentiSec
                Case Else
                    Throw New ArgumentException("Karaoke type must be k, kf, or ko")
            End Select
        End Function

        Public Shared Function BuildClipRect(x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer) As String
            Return "\clip(" & x1 & "," & y1 & "," & x2 & "," & y2 & ")"
        End Function

        Public Shared Function BuildIClipRect(x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer) As String
            Return "\iclip(" & x1 & "," & y1 & "," & x2 & "," & y2 & ")"
        End Function

#End Region

#Region "Tag Parsing and Extraction"

        Public Shared Function ExtractTags(text As String) As List(Of String)
            Dim result = New List(Of String)()
            If String.IsNullOrWhiteSpace(text) Then Return result
            Dim matches = Regex.Matches(text, "\{([^}]+)\}")
            For Each m As Match In matches
                result.Add(m.Groups(1).Value.Trim())
            Next
            Return result
        End Function

        Public Shared Function ParseTag(rawTag As String) As AssTag
            If String.IsNullOrWhiteSpace(rawTag) Then Return New AssTag(AssTagType.Unknown, rawTag, "Unknown")
            Dim lower = rawTag.ToLower()
            If lower.StartsWith("\pos(") Then Return New AssTag(AssTagType.Position, rawTag, "Position")
            If lower.StartsWith("\move(") Then Return New AssTag(AssTagType.Move, rawTag, "Move")
            If lower.StartsWith("\an") Then Return New AssTag(AssTagType.Alignment, rawTag, "Alignment")
            If lower.StartsWith("\org(") Then Return New AssTag(AssTagType.Origin, rawTag, "Origin")
            If lower.StartsWith("\frz") OrElse (lower.StartsWith("\fr") AndAlso Not lower.StartsWith("\frx") AndAlso Not lower.StartsWith("\fry")) Then Return New AssTag(AssTagType.RotateZ, rawTag, "Rotate Z")
            If lower.StartsWith("\frx") Then Return New AssTag(AssTagType.RotateX, rawTag, "Rotate X")
            If lower.StartsWith("\fry") Then Return New AssTag(AssTagType.RotateY, rawTag, "Rotate Y")
            If lower.StartsWith("\fscx") Then Return New AssTag(AssTagType.ScaleX, rawTag, "Scale X")
            If lower.StartsWith("\fscy") Then Return New AssTag(AssTagType.ScaleY, rawTag, "Scale Y")
            If lower.StartsWith("\fax") Then Return New AssTag(AssTagType.ShearX, rawTag, "Shear X")
            If lower.StartsWith("\fay") Then Return New AssTag(AssTagType.ShearY, rawTag, "Shear Y")
            If lower.StartsWith("\fn") Then Return New AssTag(AssTagType.FontName, rawTag, "Font Name")
            If lower.StartsWith("\fs") Then Return New AssTag(AssTagType.FontSize, rawTag, "Font Size")
            If lower.StartsWith("\b") Then Return New AssTag(AssTagType.Bold, rawTag, "Bold")
            If lower.StartsWith("\i") Then Return New AssTag(AssTagType.Italic, rawTag, "Italic")
            If lower.StartsWith("\u") Then Return New AssTag(AssTagType.Underline, rawTag, "Underline")
            If lower.StartsWith("\s") Then Return New AssTag(AssTagType.Strikeout, rawTag, "Strikeout")
            If lower.StartsWith("\xbord") Then Return New AssTag(AssTagType.XBorder, rawTag, "X Border")
            If lower.StartsWith("\ybord") Then Return New AssTag(AssTagType.YBorder, rawTag, "Y Border")
            If lower.StartsWith("\bord") Then Return New AssTag(AssTagType.Border, rawTag, "Border")
            If lower.StartsWith("\xshad") Then Return New AssTag(AssTagType.XShadow, rawTag, "X Shadow")
            If lower.StartsWith("\yshad") Then Return New AssTag(AssTagType.YShadow, rawTag, "Y Shadow")
            If lower.StartsWith("\shad") Then Return New AssTag(AssTagType.Shadow, rawTag, "Shadow")
            If lower.StartsWith("\blur") Then Return New AssTag(AssTagType.Blur, rawTag, "Blur")
            If lower.StartsWith("\be") Then Return New AssTag(AssTagType.EdgeBlur, rawTag, "Edge Blur")
            If lower.StartsWith("\1c") OrElse lower = "\c" OrElse lower.StartsWith("\c&") Then Return New AssTag(AssTagType.PrimaryColor, rawTag, "Primary Color")
            If lower.StartsWith("\2c") Then Return New AssTag(AssTagType.SecondaryColor, rawTag, "Secondary Color")
            If lower.StartsWith("\3c") Then Return New AssTag(AssTagType.BorderColor, rawTag, "Border Color")
            If lower.StartsWith("\4c") Then Return New AssTag(AssTagType.ShadowColor, rawTag, "Shadow Color")
            If lower.StartsWith("\alpha") Then Return New AssTag(AssTagType.Alpha, rawTag, "Alpha")
            If lower.StartsWith("\1a") Then Return New AssTag(AssTagType.PrimaryAlpha, rawTag, "Primary Alpha")
            If lower.StartsWith("\2a") Then Return New AssTag(AssTagType.SecondaryAlpha, rawTag, "Secondary Alpha")
            If lower.StartsWith("\3a") Then Return New AssTag(AssTagType.BorderAlpha, rawTag, "Border Alpha")
            If lower.StartsWith("\4a") Then Return New AssTag(AssTagType.ShadowAlpha, rawTag, "Shadow Alpha")
            If lower.StartsWith("\fad(") Then Return New AssTag(AssTagType.Fade, rawTag, "Fade")
            If lower.StartsWith("\fade(") Then Return New AssTag(AssTagType.ComplexFade, rawTag, "Complex Fade")
            If lower.StartsWith("\t(") Then Return New AssTag(AssTagType.Transform, rawTag, "Transform")
            If lower.StartsWith("\kf") Then Return New AssTag(AssTagType.KaraokeKF, rawTag, "Karaoke KF")
            If lower.StartsWith("\ko") Then Return New AssTag(AssTagType.KaraokeKO, rawTag, "Karaoke KO")
            If lower.StartsWith("\k") Then Return New AssTag(AssTagType.KaraokeK, rawTag, "Karaoke K")
            If lower.StartsWith("\clip(") Then Return New AssTag(AssTagType.ClipRect, rawTag, "Clip")
            If lower.StartsWith("\iclip(") Then Return New AssTag(AssTagType.IClipRect, rawTag, "Inverse Clip")
            If lower.StartsWith("\p") Then Return New AssTag(AssTagType.VectorDrawing, rawTag, "Vector Draw")
            Return New AssTag(AssTagType.Unknown, rawTag, "Unknown")
        End Function

#End Region

#Region "Tag Application"

        Public Shared Function ApplyTagToLine(line As String, newRawTag As String) As String
            Dim tagType = GetTagTypeFromString(newRawTag)
            line = RemoveTagByType(line, tagType)
            Dim wrapTag = "{" & newRawTag & "}"
            Return InsertTag(line, wrapTag)
        End Function

        Private Shared Function InsertTag(line As String, wrapTag As String) As String
            Dim dialogueMatch = Regex.Match(line, "^Dialogue:\s*\d+,[^,]+,[^,]+,[^,]+,[^,]*,[^,]*,[^,]*,[^,]*,[^,]*,(.*)$", RegexOptions.IgnoreCase)
            If dialogueMatch.Success Then
                Dim textStartIndex = dialogueMatch.Groups(1).Index
                Return line.Insert(textStartIndex, wrapTag & " ")
            Else
                Return wrapTag & " " & line
            End If
        End Function

        Public Shared Function ApplyTagsToAllLines(content As String, tags As IEnumerable(Of String)) As String
            Dim lineArr = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.None)
            Dim sb = New StringBuilder()
            For i As Integer = 0 To lineArr.Length - 1
                Dim line = lineArr(i)
                If String.IsNullOrWhiteSpace(line) Then
                    sb.AppendLine(line)
                    Continue For
                End If
                For Each tag In tags
                    line = ApplyTagToLine(line, tag)
                Next
                sb.AppendLine(line)
            Next
            Return sb.ToString().TrimEnd()
        End Function

        Public Shared Function RemoveAllTags(line As String) As String
            Return Regex.Replace(line, "\{[^}]*\} *", "")
        End Function

        Public Shared Function RemoveTagByType(line As String, tagType As AssTagType) As String
            Dim tags = ExtractTags(line)
            If tags.Count = 0 Then Return line
            For Each tag In tags
                Dim parsed = ParseTag(tag)
                If parsed.TagType = tagType Then
                    Dim wrapTag = "{" & tag & "}"
                    Dim escaped = Regex.Escape(wrapTag)
                    line = Regex.Replace(line, escaped, "")
                    line = Regex.Replace(line, escaped & " ", "")
                End If
            Next
            line = Regex.Replace(line, "\{\s*\} *", "")
            Return line
        End Function

        Public Shared Function RemoveAllTagsFromAllLines(content As String) As String
            Return Regex.Replace(content, "\{[^}]*\} *", "")
        End Function

#End Region

#Region "Internal Helpers"

        Private Shared Function GetTagTypeFromString(rawTag As String) As AssTagType
            Return ParseTag(rawTag.Trim()).TagType
        End Function

#End Region

#Region "Preset Effects"

        Public Shared Function GetPresetEffects() As List(Of EffectPreset)
            Dim presets = New List(Of EffectPreset)()
            Dim fadeIn = New EffectPreset("Fade In", "Mo dan xuat hien")
            fadeIn.Tags.Add(New AssTag(AssTagType.Fade, "\fad(300,200)", "Fade"))
            presets.Add(fadeIn)
            Dim fadeOut = New EffectPreset("Fade Out", "Mo dan bien mat")
            fadeOut.Tags.Add(New AssTag(AssTagType.Fade, "\fad(200,500)", "Fade"))
            presets.Add(fadeOut)
            Dim glow = New EffectPreset("Glow", "Vien sang bao quanh chu")
            glow.Tags.Add(New AssTag(AssTagType.Border, "\bord2", "Border"))
            glow.Tags.Add(New AssTag(AssTagType.Blur, "\blur1", "Blur"))
            presets.Add(glow)
            Dim bigText = New EffectPreset("Big Text", "Chu to dam")
            bigText.Tags.Add(New AssTag(AssTagType.FontSize, "\fs48", "Font Size"))
            bigText.Tags.Add(New AssTag(AssTagType.Bold, "\b1", "Bold"))
            presets.Add(bigText)
            Dim centerTop = New EffectPreset("Center Top", "Can giua tren cung")
            centerTop.Tags.Add(New AssTag(AssTagType.Alignment, "\an8", "Alignment"))
            presets.Add(centerTop)
            Dim centerScreen = New EffectPreset("Center Screen", "Can chinh giua man hinh")
            centerScreen.Tags.Add(New AssTag(AssTagType.Alignment, "\an5", "Alignment"))
            presets.Add(centerScreen)
            Dim karaokeSweep = New EffectPreset("Karaoke Sweep", "Quet mau karaoke")
            karaokeSweep.Tags.Add(New AssTag(AssTagType.PrimaryColor, "\1c&H0000FF&", "Primary Color"))
            karaokeSweep.Tags.Add(New AssTag(AssTagType.SecondaryColor, "\2c&HFFFF00&", "Secondary Color"))
            presets.Add(karaokeSweep)
            Return presets
        End Function

#End Region

#Region "Validation"

        Public Shared Function ValidateTag(tagType As AssTagType, params As Dictionary(Of String, Object)) As Tuple(Of Boolean, String)
            Select Case tagType
                Case AssTagType.Position
                    If Not params.ContainsKey("x") OrElse Not params.ContainsKey("y") Then Return Tuple.Create(False, "Missing x,y parameters")
                    Return Tuple.Create(True, "")
                Case AssTagType.Alignment
                    If Not params.ContainsKey("n") Then Return Tuple.Create(False, "Missing alignment value (1-9)")
                    Dim n = Convert.ToInt32(params("n"))
                    If n < 1 OrElse n > 9 Then Return Tuple.Create(False, "Alignment must be 1-9")
                    Return Tuple.Create(True, "")
                Case AssTagType.FontSize
                    If Not params.ContainsKey("size") Then Return Tuple.Create(False, "Missing font size")
                    Dim size = Convert.ToInt32(params("size"))
                    If size < 1 Then Return Tuple.Create(False, "Font size must be positive")
                    Return Tuple.Create(True, "")
                Case AssTagType.PrimaryColor, AssTagType.SecondaryColor, AssTagType.BorderColor, AssTagType.ShadowColor
                    If Not params.ContainsKey("hex") Then Return Tuple.Create(False, "Missing hex color value")
                    Dim hex = params("hex").ToString()
                    If Not Regex.IsMatch(hex, "^[0-9A-Fa-f]{6}$") Then Return Tuple.Create(False, "Invalid hex color (need 6 hex digits: BBGGRR)")
                    Return Tuple.Create(True, "")
                Case Else
                    Return Tuple.Create(True, "")
            End Select
        End Function

#End Region

    End Class

End Namespace
