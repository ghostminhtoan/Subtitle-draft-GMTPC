namespace Subtitle_draft_GMTPC.Models
{
    // Enum cho các loại ASS override tags
    public enum AssTagType
    {
        Unknown,
        // Position and Movement
        Position,
        Move,
        Alignment,
        Origin,
        // Transform and Rotation
        RotateZ,
        RotateX,
        RotateY,
        ScaleX,
        ScaleY,
        ShearX,
        ShearY,
        // Font and Style
        FontName,
        FontSize,
        Bold,
        Italic,
        Underline,
        Strikeout,
        // Border and Shadow
        Border,
        XBorder,
        YBorder,
        Shadow,
        XShadow,
        YShadow,
        Blur,
        EdgeBlur,
        // Color and Alpha
        PrimaryColor,
        SecondaryColor,
        BorderColor,
        ShadowColor,
        Alpha,
        PrimaryAlpha,
        SecondaryAlpha,
        BorderAlpha,
        ShadowAlpha,
        // Fade and Animation
        Fade,
        ComplexFade,
        Transform,
        // Karaoke
        KaraokeK,
        KaraokeKF,
        KaraokeKO,
        // Clip and Draw
        ClipRect,
        IClipRect,
        ClipVector,
        VectorDrawing
    }

    // Đại diện cho một ASS override tag
    public class AssTag
    {
        public AssTagType TagType { get; set; }
        public string RawTag { get; set; }
        public string DisplayName { get; set; }

        public AssTag()
        {
        }

        public AssTag(AssTagType tagType, string rawTag, string displayName)
        {
            TagType = tagType;
            RawTag = rawTag;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return $"{DisplayName}: {RawTag}";
        }
    }

    // Group các tags theo chức năng
    public class AssTagGroup
    {
        public string GroupName { get; set; }
        public string GroupIcon { get; set; }
        public System.Collections.Generic.List<AssTag> Tags { get; set; } = new System.Collections.Generic.List<AssTag>();

        public AssTagGroup(string groupName, string groupIcon)
        {
            GroupName = groupName;
            GroupIcon = groupIcon;
        }
    }

    // Preset effect - tổ hợp tags cấu hình sẵn
    public class EffectPreset
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public System.Collections.Generic.List<AssTag> Tags { get; set; } = new System.Collections.Generic.List<AssTag>();

        public EffectPreset(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    // Thông tin effect đang áp dụng cho một line
    public class LineEffectInfo
    {
        public int LineIndex { get; set; }
        public string LineText { get; set; }
        public System.Collections.Generic.List<AssTag> AppliedTags { get; set; } = new System.Collections.Generic.List<AssTag>();

        public LineEffectInfo()
        {
        }

        public LineEffectInfo(int lineIndex, string lineText)
        {
            LineIndex = lineIndex;
            LineText = lineText;
        }
    }
}
