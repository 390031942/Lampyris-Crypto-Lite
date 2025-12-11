namespace Lampyris.Crypto.Server.Lite;

using System.Text;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Lampyris.CSharp.Common;

/// <summary>
/// 币安公告网页HTML文本提取器 - 从网页HTML中定位特定部分并提取格式化文本，得到公告内容
/// </summary>
public class BinanceAnnouncementHtmlTextExtractor
{
    /// <summary>
    /// 外部调用，提取得到公告内容
    /// </summary>
    public static string? Extract(string html)
    {
        try
        {
            // 使用XPath定位
            string xpath = "//div[contains(@class, 'richtext-container')]";
            return ExtractTextWithLineBreaks(html, xpath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"提取币安公告 发生错误: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// 从HTML中提取指定部分的文本（带换行格式）
    /// </summary>
    /// <param name="html">完整的HTML文档</param>
    /// <param name="xpath">用于定位目标元素的XPath表达式</param>
    /// <returns>格式化后的文本内容</returns>
    private static string ExtractTextWithLineBreaks(string html, string xpath)
    {
        if (string.IsNullOrEmpty(html))
            throw new ArgumentException("HTML内容不能为空");

        if (string.IsNullOrEmpty(xpath))
            throw new ArgumentException("XPath不能为空");

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // 定位目标元素
        var targetNode = htmlDoc.DocumentNode.SelectSingleNode(xpath);
        if (targetNode == null)
        {
            throw new InvalidOperationException($"未找到匹配XPath '{xpath}' 的元素");
        }

        // 提取文本并保留格式
        return ExtractTextFromNode(targetNode);
    }

    /// <summary>
    /// 使用CSS选择器定位元素并提取文本
    /// </summary>
    /// <param name="html">完整的HTML文档</param>
    /// <param name="cssSelector">CSS选择器</param>
    /// <returns>格式化后的文本内容</returns>
    private static string ExtractTextWithLineBreaksByCss(string html, string cssSelector)
    {
        if (string.IsNullOrEmpty(html))
            throw new ArgumentException("HTML内容不能为空");

        if (string.IsNullOrEmpty(cssSelector))
            throw new ArgumentException("CSS选择器不能为空");

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // 使用HtmlAgilityPack.CssSelectors扩展（需要安装对应NuGet包）
        var targetNode = htmlDoc.DocumentNode.QuerySelector(cssSelector);
        if (targetNode == null)
        {
            throw new InvalidOperationException($"未找到匹配CSS选择器 '{cssSelector}' 的元素");
        }

        return ExtractTextFromNode(targetNode);
    }

    /// <summary>
    /// 从HTML节点中提取格式化文本
    /// </summary>
    private static string ExtractTextFromNode(HtmlNode node)
    {
        var sb = new StringBuilder();
        TraverseNodes(node, sb);
        return sb.ToString().Trim();
    }

    /// <summary>
    /// 递归遍历节点，提取文本并处理格式
    /// </summary>
    private static void TraverseNodes(HtmlNode node, StringBuilder sb)
    {
        foreach (var childNode in node.ChildNodes)
        {
            switch (childNode.NodeType)
            {
                case HtmlNodeType.Text:
                    // 处理文本节点
                    ProcessTextNode(childNode, sb);
                    break;

                case HtmlNodeType.Element:
                    // 处理元素节点
                    ProcessElementNode(childNode, sb);
                    break;
            }
        }
    }

    /// <summary>
    /// 处理文本节点
    /// </summary>
    private static void ProcessTextNode(HtmlNode node, StringBuilder sb)
    {
        var text = node.InnerText.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            // 只在文本非空时添加，避免多余空格
            if (sb.Length > 0 && !char.IsWhiteSpace(sb[sb.Length - 1]))
            {
                sb.Append(' ');
            }
            sb.Append(text);
        }
    }

    /// <summary>
    /// 处理元素节点
    /// </summary>
    private static void ProcessElementNode(HtmlNode node, StringBuilder sb)
    {
        // 在块级元素前添加换行
        if (IsBlockElement(node.Name) && sb.Length > 0)
        {
            EnsureNewLine(sb);
        }

        // 特殊处理换行标签
        if (node.Name == "br")
        {
            sb.Append(Environment.NewLine);
            return;
        }

        // 递归处理子节点
        TraverseNodes(node, sb);

        // 在块级元素后添加换行
        if (IsBlockElement(node.Name))
        {
            EnsureNewLine(sb);
        }
    }

    /// <summary>
    /// 检查是否为块级元素
    /// </summary>
    private static bool IsBlockElement(string tagName)
    {
        var blockElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "div", "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li", "section", "article", "header",
            "footer", "nav", "aside", "main", "figure", "figcaption",
            "table", "tr", "td", "th", "form", "fieldset", "legend",
            "blockquote", "pre", "hr", "address", "dl", "dt", "dd"
        };

        return blockElements.Contains(tagName);
    }

    /// <summary>
    /// 确保字符串以换行结尾
    /// </summary>
    private static void EnsureNewLine(StringBuilder sb)
    {
        if (sb.Length == 0) return;

        var newLine = Environment.NewLine;
        var newLineLength = newLine.Length;

        // 检查是否已经以换行结尾
        if (sb.Length >= newLineLength)
        {
            var endsWithNewLine = true;
            for (int i = 0; i < newLineLength; i++)
            {
                if (sb[sb.Length - newLineLength + i] != newLine[i])
                {
                    endsWithNewLine = false;
                    break;
                }
            }

            if (endsWithNewLine) return;
        }

        sb.Append(newLine);
    }

    /// <summary>
    /// 从HTML中提取纯文本（不保留格式）
    /// </summary>
    public static string ExtractPlainText(string html, string xpath)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var targetNode = htmlDoc.DocumentNode.SelectSingleNode(xpath);
        if (targetNode == null)
        {
            throw new InvalidOperationException($"未找到匹配XPath '{xpath}' 的元素");
        }

        return targetNode.InnerText.Trim();
    }
}