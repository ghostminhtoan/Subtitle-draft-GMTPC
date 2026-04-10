using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Subtitle_draft_GMTPC
{
    /// <summary>
    /// Quản lý chức năng tìm kiếm trong TextBox và ListBox
    /// </summary>
    public class SearchManager
    {
        private string _searchText = "";
        private int _currentSearchIndex = -1;
        private List<int> _matchPositions = new List<int>();
        private TextBox _activeTextBox;
        private ListBox _activeListBox;

        /// <summary>
        /// Thực hiện tìm kiếm trong TextBox - KHÔNG focus để tránh nhảy cursor
        /// </summary>
        public bool SearchInTextBox(TextBox textBox, string searchText, bool findNext = false)
        {
            if (textBox == null || string.IsNullOrWhiteSpace(searchText))
                return false;

            _activeTextBox = textBox;
            _activeListBox = null;

            // Nếu là tìm kiếm mới (khác text trước đó)
            if (searchText != _searchText)
            {
                _searchText = searchText;
                _currentSearchIndex = -1;
                _matchPositions.Clear();
                FindAllMatches(textBox, searchText);
            }

            // Tìm vị trí tiếp theo
            if (_matchPositions.Count == 0)
                return false;

            if (findNext)
            {
                _currentSearchIndex++;
                if (_currentSearchIndex >= _matchPositions.Count)
                    _currentSearchIndex = 0; // Quay lại đầu
            }
            else
            {
                _currentSearchIndex = 0;
            }

            // Select text tại vị trí tìm thấy - KHÔNG focus
            int pos = _matchPositions[_currentSearchIndex];
            textBox.SelectionStart = pos;
            textBox.SelectionLength = searchText.Length;

            return true;
        }

        /// <summary>
        /// Thực hiện tìm kiếm trong ListBox
        /// </summary>
        public bool SearchInListBox(ListBox listBox, string searchText, bool findNext = false)
        {
            if (listBox == null || string.IsNullOrWhiteSpace(searchText))
                return false;

            _activeListBox = listBox;
            _activeTextBox = null;

            string lowerSearch = searchText.ToLower();
            int startIndex = findNext ? (_currentSearchIndex + 1) : 0;

            // Tìm từ vị trí hiện tại
            for (int i = startIndex; i < listBox.Items.Count; i++)
            {
                var item = listBox.Items[i];
                string itemText = item?.ToString()?.ToLower() ?? "";
                
                // Nếu item là ListBoxItem, lấy Content
                if (item is ListBoxItem listBoxItem && listBoxItem.Content is TextBlock textBlock)
                {
                    itemText = textBlock.Text?.ToLower() ?? "";
                }

                if (itemText.Contains(lowerSearch))
                {
                    _currentSearchIndex = i;
                    _searchText = searchText;
                    listBox.SelectedIndex = i;
                    listBox.ScrollIntoView(item);
                    listBox.Focus();
                    return true;
                }
            }

            // Wrap around - tìm từ đầu
            if (findNext && startIndex > 0)
            {
                for (int i = 0; i < startIndex && i < listBox.Items.Count; i++)
                {
                    var item = listBox.Items[i];
                    string itemText = item?.ToString()?.ToLower() ?? "";
                    
                    if (item is ListBoxItem listBoxItem && listBoxItem.Content is TextBlock textBlock)
                    {
                        itemText = textBlock.Text?.ToLower() ?? "";
                    }

                    if (itemText.Contains(lowerSearch))
                    {
                        _currentSearchIndex = i;
                        _searchText = searchText;
                        listBox.SelectedIndex = i;
                        listBox.ScrollIntoView(item);
                        listBox.Focus();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tìm tất cả vị trí xuất hiện của searchText trong TextBox
        /// </summary>
        private void FindAllMatches(TextBox textBox, string searchText)
        {
            if (textBox == null || string.IsNullOrWhiteSpace(searchText))
                return;

            string content = textBox.Text ?? "";
            string lowerContent = content.ToLower();
            string lowerSearch = searchText.ToLower();

            int startIndex = 0;
            while ((startIndex = lowerContent.IndexOf(lowerSearch, startIndex)) != -1)
            {
                _matchPositions.Add(startIndex);
                startIndex += searchText.Length;
            }
        }

        /// <summary>
        /// Reset trạng thái tìm kiếm
        /// </summary>
        public void Reset()
        {
            _searchText = "";
            _currentSearchIndex = -1;
            _matchPositions.Clear();
            _activeTextBox = null;
            _activeListBox = null;
        }

        /// <summary>
        /// Lấy số lượng kết quả tìm thấy
        /// </summary>
        public int MatchCount => _matchPositions.Count;

        /// <summary>
        /// Lấy vị trí hiện tại
        /// </summary>
        public int CurrentPosition => _currentSearchIndex;
    }
}
