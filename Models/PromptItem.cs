namespace Subtitle_draft_GMTPC.Models
{
    /// <summary>
    /// Lưu thông tin một prompt với ID cố định
    /// </summary>
    public class PromptItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }

        public PromptItem()
        {
        }

        public PromptItem(int id, string name, string content)
        {
            Id = id;
            Name = name;
            Content = content;
        }

        /// <summary>
        /// Trả về tên hiển thị dạng "STT. Tên"
        /// </summary>
        public string DisplayName => $"{Id}. {Name}";
    }
}
