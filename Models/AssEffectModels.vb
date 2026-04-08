Namespace Models

    ' Enum choc cac loai ASS override tags
    Public Enum AssTagType
        Unknown
        ' Position and Movement
        Position
        Move
        Alignment
        Origin
        ' Transform and Rotation
        RotateZ
        RotateX
        RotateY
        ScaleX
        ScaleY
        ShearX
        ShearY
        ' Font and Style
        FontName
        FontSize
        Bold
        Italic
        Underline
        Strikeout
        ' Border and Shadow
        Border
        XBorder
        YBorder
        Shadow
        XShadow
        YShadow
        Blur
        EdgeBlur
        ' Color and Alpha
        PrimaryColor
        SecondaryColor
        BorderColor
        ShadowColor
        Alpha
        PrimaryAlpha
        SecondaryAlpha
        BorderAlpha
        ShadowAlpha
        ' Fade and Animation
        Fade
        ComplexFade
        Transform
        ' Karaoke
        KaraokeK
        KaraokeKF
        KaraokeKO
        ' Clip and Draw
        ClipRect
        IClipRect
        ClipVector
        VectorDrawing
    End Enum

    ' Dai dien cho mot ASS override tag
    Public Class AssTag
        Public Property TagType As AssTagType
        Public Property RawTag As String
        Public Property DisplayName As String

        Public Sub New()
        End Sub

        Public Sub New(tagType As AssTagType, rawTag As String, displayName As String)
            Me.TagType = tagType
            Me.RawTag = rawTag
            Me.DisplayName = displayName
        End Sub

        Public Overrides Function ToString() As String
            Return String.Format("{0}: {q}", DisplayName, RawTag)
        End Function
    End Class

    ' Group cac tags theo chuc nang
    Public Class AssTagGroup
        Public Property GroupName As String
        Public Property GroupIcon As String
        Public Property Tags As New List(Of AssTag)()

        Public Sub New(groupName As String, groupIcon As String)
            Me.GroupName = groupName
            Me.GroupIcon = groupIcon
        End Sub
    End Class

    ' Preset effect - to hop tags da cau hinh san
    Public Class EffectPreset
        Public Property Name As String
        Public Property Description As String
        Public Property Tags As New List(Of AssTag)()

        Public Sub New(name As String, description As String)
            Me.Name = name
            Me.Description = description
        End Sub
    End Class

    ' Thong tin effect dang ap dung chomot mot line
    Public Class LineEffectInfo
        Public Property LineIndex As Integer
        Public Property LineText As String
        Public Property AppliedTags As New List(Of AssTag)()

        Public Sub New()
        End Sub

        Public Sub New(lineIndex As Integer, lineText As String)
            Me.LineIndex = lineIndex
            Me.LineText = lineText
        End Sub
    End Class

End Namespace