using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Subtitle_draft_GMTPC.Services
{
    internal static class GitHubMarkdownHtmlRenderer
    {
        private static readonly Regex HeadingRegex = new Regex(@"^(#{1,6})\s+(.*)$", RegexOptions.Compiled);
        private static readonly Regex OrderedListRegex = new Regex(@"^\s*\d+\.\s+(.*)$", RegexOptions.Compiled);
        private static readonly Regex UnorderedListRegex = new Regex(@"^\s*[-*+]\s+(.*)$", RegexOptions.Compiled);
        private static readonly Regex TableSeparatorRegex = new Regex(@"^\s*\|?[\s:\-]+\|[\|\s:\-]*$", RegexOptions.Compiled);
        private static readonly Regex ImageRegex = new Regex(@"!\[(.*?)\]\(([^)\s]+(?:\s+""[^""]*"")?)\)", RegexOptions.Compiled);
        private static readonly Regex LinkRegex = new Regex(@"\[(.*?)\]\(([^)\s]+(?:\s+""[^""]*"")?)\)", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new Regex(@"(\*\*|__)(.+?)\1", RegexOptions.Compiled);
        private static readonly Regex InlineCodeRegex = new Regex(@"`([^`]+)`", RegexOptions.Compiled);

        public static string RenderDocument(string markdown, string sourceUrl, string title)
        {
            var context = GitHubPathContext.FromBlobUrl(sourceUrl);
            var bodyHtml = RenderBlocks((markdown ?? string.Empty).Replace("\r\n", "\n").Split('\n'), context);

            return @"<!DOCTYPE html>
<html>
<head>
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
<meta charset=""utf-8"" />
<title>" + WebUtility.HtmlEncode(title) + @"</title>
<style>
body {
    margin: 0;
    padding: 24px 28px 56px;
    background: #16181d;
    color: #e6edf3;
    font-family: ""Segoe UI"", Arial, sans-serif;
    line-height: 1.7;
    font-size: 15px;
}
h1, h2, h3, h4, h5, h6 {
    color: #ffffff;
    margin: 1.4em 0 0.6em;
    line-height: 1.25;
}
h1 { font-size: 2em; border-bottom: 1px solid #30363d; padding-bottom: 0.3em; }
h2 { font-size: 1.55em; border-bottom: 1px solid #30363d; padding-bottom: 0.25em; }
h3 { font-size: 1.25em; }
p, ul, ol, table, blockquote, pre { margin: 0 0 16px; }
ul, ol { padding-left: 28px; }
li { margin: 4px 0; }
blockquote {
    margin-left: 0;
    padding: 0 16px;
    color: #adb8c5;
    border-left: 4px solid #3b82f6;
    background: #1b2130;
}
code {
    background: #24292f;
    color: #f0f6fc;
    border-radius: 4px;
    padding: 2px 5px;
    font-family: Consolas, ""Courier New"", monospace;
    font-size: 0.95em;
}
pre {
    background: #0d1117;
    border: 1px solid #30363d;
    border-radius: 8px;
    padding: 14px 16px;
    overflow: auto;
}
pre code {
    background: transparent;
    padding: 0;
    border-radius: 0;
}
table {
    width: 100%;
    border-collapse: collapse;
    display: block;
    overflow-x: auto;
}
th, td {
    border: 1px solid #3a4048;
    padding: 10px 12px;
    text-align: left;
    vertical-align: top;
}
th {
    background: #202833;
    color: #ffffff;
}
tr:nth-child(even) td {
    background: #1b1f25;
}
hr {
    border: 0;
    border-top: 1px solid #30363d;
    margin: 24px 0;
}
a {
    color: #58a6ff;
    text-decoration: none;
}
a:hover {
    text-decoration: underline;
}
img {
    max-width: 100%;
    height: auto;
    border-radius: 8px;
    border: 1px solid #30363d;
    margin: 8px 0 16px;
    background: #0d1117;
}
.page {
    max-width: 1280px;
    margin: 0 auto;
}
.tutorial-search-hit {
    background: #5a4300;
    color: #fff7d6;
    border-radius: 3px;
    padding: 0 1px;
}
.tutorial-search-hit.tutorial-search-current {
    background: #f7d047;
    color: #111111;
    box-shadow: 0 0 0 1px #ffd76a;
}
</style>
</head>
<body>
<div class=""page"" id=""page-content"">" + bodyHtml + @"</div>
<script>" + BuildSearchScript(title) + @"</script>
</body>
</html>";
        }

        private static string BuildSearchScript(string title)
        {
            return @"
(function () {
    var state = {
        title: '" + JavaScriptEncode(title) + @"',
        query: '',
        matchCase: false,
        wholeWord: false,
        useRegex: false,
        currentIndex: -1,
        originalHtml: null
    };

    function getContent() {
        return document.getElementById('page-content');
    }

    function ensureOriginalHtml() {
        var content = getContent();
        if (content && state.originalHtml === null) {
            state.originalHtml = content.innerHTML;
        }
    }

    function resetContent() {
        var content = getContent();
        if (!content) {
            return;
        }

        ensureOriginalHtml();
        content.innerHTML = state.originalHtml;
    }

    function buildResult(type, total, current, message) {
        if (type === 'error') {
            return 'error|' + message;
        }

        return 'ok|' + total + '|' + current + '|' + state.title;
    }

    function getMatches() {
        return document.querySelectorAll('span.tutorial-search-hit');
    }

    function clearCurrentMatch() {
        var matches = getMatches();
        for (var i = 0; i < matches.length; i++) {
            matches[i].className = 'tutorial-search-hit';
        }
    }

    function activateCurrentMatch() {
        var matches = getMatches();
        clearCurrentMatch();

        if (!matches.length || state.currentIndex < 0 || state.currentIndex >= matches.length) {
            return buildResult('ok', matches.length, 0, '');
        }

        var current = matches[state.currentIndex];
        current.className = 'tutorial-search-hit tutorial-search-current';

        if (current.getBoundingClientRect && window.scrollTo) {
            var rect = current.getBoundingClientRect();
            var targetTop = rect.top + window.pageYOffset - ((window.innerHeight || document.documentElement.clientHeight) / 2) + (rect.height / 2);
            if (targetTop < 0) {
                targetTop = 0;
            }

            window.scrollTo(0, targetTop);
        } else if (current.scrollIntoView) {
            current.scrollIntoView(true);
        }

        return buildResult('ok', matches.length, state.currentIndex + 1, '');
    }

    function escapeRegex(text) {
        return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    function buildRegex(query, matchCase, wholeWord, useRegex) {
        var pattern = useRegex ? query : escapeRegex(query);
        if (wholeWord) {
            pattern = '\\b(?:' + pattern + ')\\b';
        }

        return new RegExp(pattern, matchCase ? 'g' : 'gi');
    }

    function shouldSkipNode(node) {
        if (!node || !node.parentNode || !node.nodeValue) {
            return true;
        }

        var tagName = node.parentNode.tagName;
        if (!tagName) {
            return false;
        }

        tagName = tagName.toUpperCase();
        return tagName === 'SCRIPT' || tagName === 'STYLE' || tagName === 'TEXTAREA';
    }

    function highlightTextNode(node, regex) {
        var text = node.nodeValue;
        var localRegex = new RegExp(regex.source, regex.ignoreCase ? 'gi' : 'g');
        var match;
        var lastIndex = 0;
        var hasMatch = false;
        var fragment = document.createDocumentFragment();

        while ((match = localRegex.exec(text)) !== null) {
            hasMatch = true;

            if (match.index > lastIndex) {
                fragment.appendChild(document.createTextNode(text.substring(lastIndex, match.index)));
            }

            var mark = document.createElement('span');
            mark.className = 'tutorial-search-hit';
            mark.appendChild(document.createTextNode(match[0]));
            fragment.appendChild(mark);

            lastIndex = match.index + match[0].length;

            if (match[0].length === 0) {
                localRegex.lastIndex++;
            }
        }

        if (!hasMatch) {
            return false;
        }

        if (lastIndex < text.length) {
            fragment.appendChild(document.createTextNode(text.substring(lastIndex)));
        }

        node.parentNode.replaceChild(fragment, node);
        return true;
    }

    function highlightMatches(regex) {
        var content = getContent();
        if (!content) {
            return 0;
        }

        var walker = document.createTreeWalker(content, NodeFilter.SHOW_TEXT, null, false);
        var nodes = [];
        var currentNode;

        while ((currentNode = walker.nextNode())) {
            if (!shouldSkipNode(currentNode) && /\S/.test(currentNode.nodeValue)) {
                nodes.push(currentNode);
            }
        }

        for (var i = 0; i < nodes.length; i++) {
            highlightTextNode(nodes[i], regex);
        }

        return getMatches().length;
    }

    window.tutorialSearchApply = function (query, matchCase, wholeWord, useRegex, resetIndex) {
        ensureOriginalHtml();
        resetContent();

        state.query = query || '';
        state.matchCase = !!matchCase;
        state.wholeWord = !!wholeWord;
        state.useRegex = !!useRegex;

        if (!state.query) {
            state.currentIndex = -1;
            return buildResult('ok', 0, 0, '');
        }

        var regex;
        try {
            regex = buildRegex(state.query, state.matchCase, state.wholeWord, state.useRegex);
        } catch (error) {
            state.currentIndex = -1;
            return buildResult('error', 0, 0, error.message || 'Invalid regex');
        }

        var total = highlightMatches(regex);
        if (!total) {
            state.currentIndex = -1;
            return buildResult('ok', 0, 0, '');
        }

        if (resetIndex || state.currentIndex < 0 || state.currentIndex >= total) {
            state.currentIndex = 0;
        }

        return activateCurrentMatch();
    };

    window.tutorialSearchNext = function () {
        var matches = getMatches();
        if (!matches.length) {
            return buildResult('ok', 0, 0, '');
        }

        state.currentIndex = (state.currentIndex + 1) % matches.length;
        return activateCurrentMatch();
    };

    window.tutorialSearchPrev = function () {
        var matches = getMatches();
        if (!matches.length) {
            return buildResult('ok', 0, 0, '');
        }

        state.currentIndex = (state.currentIndex - 1 + matches.length) % matches.length;
        return activateCurrentMatch();
    };

    window.tutorialSearchClear = function () {
        ensureOriginalHtml();
        resetContent();
        state.query = '';
        state.currentIndex = -1;
        return buildResult('ok', 0, 0, '');
    };
})();
";
        }

        private static string JavaScriptEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }

        private static string RenderBlocks(IReadOnlyList<string> lines, GitHubPathContext context)
        {
            var html = new StringBuilder();
            var index = 0;

            while (index < lines.Count)
            {
                var line = lines[index];
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    index++;
                    continue;
                }

                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    index++;
                    var codeLines = new List<string>();
                    while (index < lines.Count && !lines[index].Trim().StartsWith("```", StringComparison.Ordinal))
                    {
                        codeLines.Add(lines[index]);
                        index++;
                    }

                    if (index < lines.Count)
                    {
                        index++;
                    }

                    html.Append("<pre><code>")
                        .Append(WebUtility.HtmlEncode(string.Join("\n", codeLines)))
                        .Append("</code></pre>");
                    continue;
                }

                var headingMatch = HeadingRegex.Match(trimmed);
                if (headingMatch.Success)
                {
                    var level = headingMatch.Groups[1].Value.Length;
                    html.Append("<h")
                        .Append(level)
                        .Append(">")
                        .Append(RenderInline(headingMatch.Groups[2].Value.Trim(), context))
                        .Append("</h")
                        .Append(level)
                        .Append(">");
                    index++;
                    continue;
                }

                if (trimmed == "---" || trimmed == "***")
                {
                    html.Append("<hr />");
                    index++;
                    continue;
                }

                if (IsTableStart(lines, index))
                {
                    html.Append(RenderTable(lines, ref index, context));
                    continue;
                }

                if (trimmed.StartsWith(">", StringComparison.Ordinal))
                {
                    html.Append(RenderBlockquote(lines, ref index, context));
                    continue;
                }

                if (UnorderedListRegex.IsMatch(trimmed) || OrderedListRegex.IsMatch(trimmed))
                {
                    html.Append(RenderList(lines, ref index, context));
                    continue;
                }

                html.Append(RenderParagraph(lines, ref index, context));
            }

            return html.ToString();
        }

        private static bool IsTableStart(IReadOnlyList<string> lines, int index)
        {
            if (index + 1 >= lines.Count)
            {
                return false;
            }

            var header = lines[index].Trim();
            var separator = lines[index + 1].Trim();
            return header.Contains("|") && TableSeparatorRegex.IsMatch(separator);
        }

        private static string RenderTable(IReadOnlyList<string> lines, ref int index, GitHubPathContext context)
        {
            var html = new StringBuilder();
            var headerCells = SplitTableRow(lines[index]);
            index += 2;

            html.Append("<table><thead><tr>");
            foreach (var cell in headerCells)
            {
                html.Append("<th>")
                    .Append(RenderInline(cell, context))
                    .Append("</th>");
            }
            html.Append("</tr></thead><tbody>");

            while (index < lines.Count)
            {
                var row = lines[index].Trim();
                if (string.IsNullOrWhiteSpace(row) || !row.Contains("|"))
                {
                    break;
                }

                var cells = SplitTableRow(lines[index]);
                html.Append("<tr>");
                foreach (var cell in cells)
                {
                    html.Append("<td>")
                        .Append(RenderInline(cell, context))
                        .Append("</td>");
                }
                html.Append("</tr>");
                index++;
            }

            html.Append("</tbody></table>");
            return html.ToString();
        }

        private static string[] SplitTableRow(string line)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("|", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1);
            }

            if (trimmed.EndsWith("|", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            return trimmed.Split('|').Select(cell => cell.Trim()).ToArray();
        }

        private static string RenderBlockquote(IReadOnlyList<string> lines, ref int index, GitHubPathContext context)
        {
            var quoteLines = new List<string>();
            while (index < lines.Count)
            {
                var trimmed = lines[index].Trim();
                if (!trimmed.StartsWith(">", StringComparison.Ordinal))
                {
                    break;
                }

                quoteLines.Add(trimmed.TrimStart('>').TrimStart());
                index++;
            }

            return "<blockquote>" + RenderBlocks(quoteLines, context) + "</blockquote>";
        }

        private static string RenderList(IReadOnlyList<string> lines, ref int index, GitHubPathContext context)
        {
            var ordered = OrderedListRegex.IsMatch(lines[index].Trim());
            var tag = ordered ? "ol" : "ul";
            var html = new StringBuilder().Append("<").Append(tag).Append(">");

            while (index < lines.Count)
            {
                var trimmed = lines[index].Trim();
                Match match = ordered ? OrderedListRegex.Match(trimmed) : UnorderedListRegex.Match(trimmed);
                if (!match.Success)
                {
                    break;
                }

                html.Append("<li>")
                    .Append(RenderInline(match.Groups[1].Value.Trim(), context))
                    .Append("</li>");
                index++;
            }

            html.Append("</").Append(tag).Append(">");
            return html.ToString();
        }

        private static string RenderParagraph(IReadOnlyList<string> lines, ref int index, GitHubPathContext context)
        {
            var paragraphLines = new List<string>();

            while (index < lines.Count)
            {
                var trimmed = lines[index].Trim();
                if (string.IsNullOrWhiteSpace(trimmed) ||
                    trimmed.StartsWith("```", StringComparison.Ordinal) ||
                    HeadingRegex.IsMatch(trimmed) ||
                    trimmed.StartsWith(">", StringComparison.Ordinal) ||
                    UnorderedListRegex.IsMatch(trimmed) ||
                    OrderedListRegex.IsMatch(trimmed) ||
                    trimmed == "---" ||
                    trimmed == "***" ||
                    IsTableStart(lines, index))
                {
                    break;
                }

                paragraphLines.Add(trimmed);
                index++;
            }

            return "<p>" + RenderInline(string.Join(" ", paragraphLines), context) + "</p>";
        }

        private static string RenderInline(string text, GitHubPathContext context)
        {
            var encoded = WebUtility.HtmlEncode(text ?? string.Empty);

            encoded = ImageRegex.Replace(encoded, match =>
            {
                var alt = WebUtility.HtmlEncode(WebUtility.HtmlDecode(match.Groups[1].Value));
                var url = context.ResolveUrl(WebUtility.HtmlDecode(match.Groups[2].Value), true);
                return "<img src=\"" + WebUtility.HtmlEncode(url) + "\" alt=\"" + alt + "\" />";
            });

            encoded = LinkRegex.Replace(encoded, match =>
            {
                var label = RenderInlineSimple(WebUtility.HtmlDecode(match.Groups[1].Value));
                var url = context.ResolveUrl(WebUtility.HtmlDecode(match.Groups[2].Value), false);
                return "<a href=\"" + WebUtility.HtmlEncode(url) + "\">" + label + "</a>";
            });

            encoded = BoldRegex.Replace(encoded, "<strong>$2</strong>");
            encoded = InlineCodeRegex.Replace(encoded, "<code>$1</code>");

            return encoded;
        }

        private static string RenderInlineSimple(string text)
        {
            var encoded = WebUtility.HtmlEncode(text ?? string.Empty);
            encoded = BoldRegex.Replace(encoded, "<strong>$2</strong>");
            encoded = InlineCodeRegex.Replace(encoded, "<code>$1</code>");
            return encoded;
        }

        private sealed class GitHubPathContext
        {
            private readonly Uri _rawBaseUri;
            private readonly Uri _blobBaseUri;

            private GitHubPathContext(Uri rawBaseUri, Uri blobBaseUri)
            {
                _rawBaseUri = rawBaseUri;
                _blobBaseUri = blobBaseUri;
            }

            public static GitHubPathContext FromBlobUrl(string blobUrl)
            {
                if (string.IsNullOrWhiteSpace(blobUrl))
                {
                    return new GitHubPathContext(new Uri("https://raw.githubusercontent.com/"), new Uri("https://github.com/"));
                }

                var sourceUri = new Uri(blobUrl);
                var segments = sourceUri.AbsolutePath.Trim('/').Split('/');
                if (segments.Length < 4 || !string.Equals(segments[2], "blob", StringComparison.OrdinalIgnoreCase))
                {
                    return new GitHubPathContext(new Uri("https://raw.githubusercontent.com/"), sourceUri);
                }

                var owner = segments[0];
                var repo = segments[1];
                var branch = segments[3];
                var directory = string.Join("/", segments.Skip(4).Take(Math.Max(segments.Length - 5, 0)));
                var rawBase = BuildBaseUri("https://raw.githubusercontent.com/", owner, repo, branch, directory);
                var blobBase = BuildBaseUri("https://github.com/", owner, repo, "blob/" + branch, directory);

                return new GitHubPathContext(new Uri(rawBase), new Uri(blobBase));
            }

            public string ResolveUrl(string target, bool imageMode)
            {
                if (string.IsNullOrWhiteSpace(target))
                {
                    return string.Empty;
                }

                target = target.Trim();
                var quoteIndex = target.IndexOf(" \"", StringComparison.Ordinal);
                if (quoteIndex >= 0)
                {
                    target = target.Substring(0, quoteIndex);
                }

                if (Uri.TryCreate(target, UriKind.Absolute, out var absoluteUri))
                {
                    return absoluteUri.AbsoluteUri;
                }

                if (target.StartsWith("#", StringComparison.Ordinal))
                {
                    return target;
                }

                if (target.StartsWith("/", StringComparison.Ordinal))
                {
                    return "https://github.com" + target;
                }

                var baseUri = imageMode ? _rawBaseUri : _blobBaseUri;
                return new Uri(baseUri, target).AbsoluteUri;
            }

            private static string BuildBaseUri(string root, params string[] parts)
            {
                var builder = new StringBuilder(root.TrimEnd('/'));
                foreach (var part in parts.Where(part => !string.IsNullOrWhiteSpace(part)))
                {
                    builder.Append("/");
                    builder.Append(Uri.EscapeUriString(part.Trim('/')));
                }
                builder.Append("/");
                return builder.ToString();
            }
        }
    }
}
