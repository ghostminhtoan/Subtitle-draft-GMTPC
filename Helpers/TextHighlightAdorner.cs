using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Subtitle_draft_GMTPC.Helpers
{
    /// <summary>
    /// Adorner tùy chỉnh để vẽ dải màu bôi đen (Highlight) trên WPF TextBox
    /// Giúp hiển thị highlight đồng thời trên nhiều TextBox độc lập với Caret/Focus của Win32.
    /// </summary>
    public class TextHighlightAdorner : Adorner
    {
        private static readonly Brush HighlightFillBrush = new SolidColorBrush(Color.FromArgb(190, 255, 215, 0)); // #FFD700 Gold/Yellow siêu sáng rực rỡ trên nền tối
        private static readonly Pen HighlightBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1.5); // Viền trắng sáng tương phản cao

        static TextHighlightAdorner()
        {
            HighlightFillBrush.Freeze();
            HighlightBorderPen.Freeze();
        }

        private readonly TextBox _textBox;
        private int _startCharIndex;
        private int _length;

        public TextHighlightAdorner(TextBox textBox, int startCharIndex, int length) : base(textBox)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _startCharIndex = Math.Max(0, startCharIndex);
            _length = Math.Max(0, length);
            IsHitTestVisible = false;
        }

        public void UpdateHighlight(int startCharIndex, int length)
        {
            _startCharIndex = Math.Max(0, startCharIndex);
            _length = Math.Max(0, length);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (_length <= 0 || string.IsNullOrEmpty(_textBox.Text))
                return;

            int textLen = _textBox.Text.Length;
            int start = Math.Min(_startCharIndex, textLen);
            int end = Math.Min(_startCharIndex + _length, textLen);

            if (start >= end)
                return;

            // Đảm bảo clip theo khung nhìn của TextBox
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, _textBox.ActualWidth, _textBox.ActualHeight)));

            try
            {
                int startLine = _textBox.GetLineIndexFromCharacterIndex(start);
                int endLine = _textBox.GetLineIndexFromCharacterIndex(Math.Max(start, end - 1));

                if (startLine < 0 || endLine < 0) return;

                for (int line = startLine; line <= endLine; line++)
                {
                    int lineCharStart = _textBox.GetCharacterIndexFromLineIndex(line);
                    int lineLength = _textBox.GetLineLength(line);
                    int lineCharEnd = lineCharStart + lineLength;

                    int segStart = Math.Max(start, lineCharStart);
                    int segEnd = Math.Min(end, lineCharEnd);

                    if (segStart >= segEnd)
                        continue;

                    // Lấy tọa độ mép trái của ký tự đầu tiên
                    Rect rStart = _textBox.GetRectFromCharacterIndex(segStart);
                    
                    // Lấy tọa độ mép phải của ký tự cuối cùng trong đoạn highlight
                    Rect rEndChar = _textBox.GetRectFromCharacterIndex(Math.Max(segStart, segEnd - 1));

                    double top = rStart.Top;
                    double height = Math.Max(rStart.Height, 18);
                    double left = rStart.Left;
                    double right = Math.Max(rEndChar.Right, left + 10);
                    double width = Math.Max(12, right - left);

                    Rect drawRect = new Rect(left, top, width, height);
                    drawingContext.DrawRectangle(HighlightFillBrush, HighlightBorderPen, drawRect);
                }
            }
            catch
            {
                // Bỏ qua lỗi vẽ nếu layout TextBox chưa render xong
            }
            finally
            {
                drawingContext.Pop();
            }
        }

        /// <summary>
        /// Áp dụng highlight trực quan lên TextBox thông qua AdornerLayer
        /// </summary>
        public static void SetHighlight(TextBox textBox, int start, int length)
        {
            if (textBox == null) return;

            var layer = AdornerLayer.GetAdornerLayer(textBox);
            if (layer == null) return;

            var adorners = layer.GetAdorners(textBox);
            TextHighlightAdorner existing = null;
            if (adorners != null)
            {
                foreach (var ad in adorners)
                {
                    if (ad is TextHighlightAdorner tha)
                    {
                        existing = tha;
                        break;
                    }
                }
            }

            if (length <= 0)
            {
                if (existing != null)
                {
                    layer.Remove(existing);
                }
                return;
            }

            if (existing != null)
            {
                existing.UpdateHighlight(start, length);
            }
            else
            {
                var newAdorner = new TextHighlightAdorner(textBox, start, length);
                layer.Add(newAdorner);
            }
        }

        /// <summary>
        /// Xóa bỏ highlight adorner trên TextBox
        /// </summary>
        public static void ClearHighlight(TextBox textBox)
        {
            if (textBox == null) return;

            var layer = AdornerLayer.GetAdornerLayer(textBox);
            if (layer == null) return;

            var adorners = layer.GetAdorners(textBox);
            if (adorners != null)
            {
                foreach (var ad in adorners)
                {
                    if (ad is TextHighlightAdorner tha)
                    {
                        layer.Remove(tha);
                    }
                }
            }
        }
    }
}
