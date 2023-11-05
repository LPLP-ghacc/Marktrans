using System.Text;

namespace Marktrans
{
    public class MarkdownTransformer
    {
        private MarkdownParser Parser { get; set; }

        public MarkdownTransformer() 
        {
            Parser = new MarkdownParser();
        }

        public string MarkdownToXaml(string input)
        {
            return Parser.MarkdownXaml(input);
        }
    }

    internal class MarkdownParser
    {
        public string MarkdownXaml(string markdownText)
        {
            var xamlText = new StringBuilder();
            bool isCodeBlock = false;

            using (var reader = new StringReader(markdownText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim() == "```")
                    {
                        isCodeBlock = !isCodeBlock;

                        continue;
                    }

                    if (isCodeBlock)
                    {
                        xamlText.AppendLine("<Paragraph>");
                        xamlText.AppendLine("<Run FontFamily=\"Consolas\" FontSize=\"13\">");
                        xamlText.AppendLine(System.Security.SecurityElement.Escape(line));
                        xamlText.AppendLine("</Run>");
                        xamlText.AppendLine("</Paragraph>");
                    }
                    else if (line.StartsWith("# "))
                    {
                        xamlText.AppendLine($"<TextBlock FontWeight=\"Bold\" FontSize=\"32\">{line.Remove(0, 2)}</TextBlock>");
                    }
                    else
                    {
                        string xamlLine = ParseInlineElements(line);
                        xamlText.AppendLine($"<TextBlock>{xamlLine}</TextBlock>");
                    }
                }
            }

            return xamlText.ToString();
        }

        private string ParseInlineElements(string line)
        {
            var parsedLine = new StringBuilder();
            var tagStack = new Stack<string>();

            for (int i = 0; i < line.Length; i++)
            {
                if (i + 1 < line.Length && line[i] == '[')
                {
                    int endOfLinkText = line.IndexOf(']', i + 1);
                    int startOfLinkUrl = line.IndexOf('(', endOfLinkText) + 1;
                    int endOfLinkUrl = line.IndexOf(')', startOfLinkUrl);

                    if (endOfLinkText != -1 && startOfLinkUrl != -1)
                    {
                        string linkText = line.Substring(i + 1, endOfLinkText - i - 1);
                        string linkUrl = line.Substring(startOfLinkUrl, endOfLinkUrl - startOfLinkUrl);

                        parsedLine.Append($"<Hyperlink NavigateUri=\"{linkUrl}\">{linkText}</Hyperlink>");

                        i = endOfLinkUrl;
                    }
                    else
                    {
                        parsedLine.Append(line[i]);
                    }
                }
                else if (line[i] == '*' && i + 1 < line.Length && line[i + 1] == '*')
                {
                    if (tagStack.Count > 0 && tagStack.Peek() == "bold")
                    {
                        parsedLine.Append("</Run>");
                        tagStack.Pop();
                    }
                    else
                    {
                        parsedLine.Append("<Run FontWeight=\"Bold\">");
                        tagStack.Push("bold");
                    }

                    i++;
                }
                else if (line[i] == '*')
                {
                    if (tagStack.Count > 0 && tagStack.Peek() == "italic")
                    {
                        parsedLine.Append("</Run>");
                        tagStack.Pop();
                    }
                    else
                    {
                        parsedLine.Append("<Run FontStyle=\"Italic\">");
                        tagStack.Push("italic");
                    }
                }
                else
                {
                    parsedLine.Append(line[i]);
                }
            }

            while (tagStack.Count > 0)
            {
                parsedLine.Append("</Run>");
                tagStack.Pop();
            }

            return parsedLine.ToString();
        }
    }
}
