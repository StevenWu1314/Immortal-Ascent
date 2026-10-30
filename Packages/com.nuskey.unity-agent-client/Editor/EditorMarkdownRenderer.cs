using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityAgentClient
{
    internal static class EditorMarkdownRenderer
    {
        private enum BlockType
        {
            Paragraph,
            Heading1,
            Heading2,
            Heading3,
            Heading4,
            Heading5,
            Heading6,
            CodeBlock,
            ListItem,
            OrderedListItem,
            BlockQuote,
            HorizontalRule
        }

        private class MarkdownBlock
        {
            public BlockType Type { get; set; }
            public string Content { get; set; }
            public string Language { get; set; } // For code blocks
            public int IndentLevel { get; set; } // For lists
        }

        private class InlineStyle
        {
            public bool IsBold { get; set; }
            public bool IsItalic { get; set; }
            public bool IsCode { get; set; }
            public bool IsStrikethrough { get; set; }
        }

        private static GUIStyle _headingStyle1;
        private static GUIStyle _headingStyle2;
        private static GUIStyle _headingStyle3;
        private static GUIStyle _headingStyle4;
        private static GUIStyle _paragraphStyle;
        private static GUIStyle _codeBlockStyle;
        private static GUIStyle _inlineCodeStyle;
        private static GUIStyle _boldStyle;
        private static GUIStyle _italicStyle;
        private static GUIStyle _boldItalicStyle;
        private static GUIStyle _listStyle;
        private static GUIStyle _quoteStyle;

        public static GUIStyle CodeBlockStyle
        {
            get
            {
                _codeBlockStyle ??= new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(0, 0, 4, 4),
                    fontSize = 11,
                    font = GUI.skin.font
                };
                return _codeBlockStyle;
            }
        }

        private static void InitializeStyles()
        {
            if (_headingStyle1 != null) return;

            var defaultFont = GUI.skin.font;

            // Heading 1
            _headingStyle1 = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                wordWrap = true,
                margin = new RectOffset(0, 0, 10, 10),
                font = defaultFont
            };

            // Heading 2
            _headingStyle2 = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                wordWrap = true,
                margin = new RectOffset(0, 0, 8, 8),
                font = defaultFont
            };

            // Heading 3
            _headingStyle3 = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                wordWrap = true,
                margin = new RectOffset(0, 0, 6, 6),
                font = defaultFont
            };

            // Heading 4
            _headingStyle4 = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                wordWrap = true,
                margin = new RectOffset(0, 0, 4, 4),
                font = defaultFont
            };

            // Paragraph
            _paragraphStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                margin = new RectOffset(0, 0, 4, 4),
                font = defaultFont
            };

            // Code block
            _codeBlockStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 4, 4),
                fontSize = 11,
                font = defaultFont
            };

            // Inline code
            _inlineCodeStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                padding = new RectOffset(3, 3, 0, 0),
                normal = { background = MakeBackgroundTexture(new Color(0.2f, 0.2f, 0.2f, 0.3f)) },
                font = defaultFont
            };

            // Bold
            _boldStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                wordWrap = true,
                richText = true,
                margin = new RectOffset(0, 0, 4, 4),
                font = defaultFont
            };

            // Italic
            _italicStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                fontStyle = FontStyle.Italic,
                margin = new RectOffset(0, 0, 4, 4),
                font = defaultFont
            };

            // Bold + Italic
            _boldItalicStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                wordWrap = true,
                richText = true,
                fontStyle = FontStyle.BoldAndItalic,
                margin = new RectOffset(0, 0, 4, 4),
                font = defaultFont
            };

            // List
            _listStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                margin = new RectOffset(10, 0, 2, 2),
                font = defaultFont
            };

            // Quote
            _quoteStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(10, 0, 4, 4),
                normal = { background = MakeBackgroundTexture(new Color(0.3f, 0.3f, 0.3f, 0.3f)) },
                font = defaultFont
            };
        }

        private static Texture2D MakeBackgroundTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Render markdown text using IMGUI
        /// </summary>
        /// <param name="markdown">Markdown text to render</param>
        public static void Render(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return;
            }

            InitializeStyles();

            var blocks = ParseMarkdown(markdown);

            foreach (var block in blocks)
            {
                RenderBlock(block);
            }
        }

        private static List<MarkdownBlock> ParseMarkdown(string markdown)
        {
            var blocks = new List<MarkdownBlock>();
            var lines = markdown.Split('\n');

            int i = 0;
            while (i < lines.Length)
            {
                var line = lines[i];

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                // Code block (```)
                if (line.TrimStart().StartsWith("```"))
                {
                    var codeBlock = ParseCodeBlock(lines, ref i);
                    if (codeBlock != null)
                    {
                        blocks.Add(codeBlock);
                        continue;
                    }
                }

                // Horizontal rule
                if (Regex.IsMatch(line.Trim(), @"^(\*\*\*+|---+|___+)$"))
                {
                    blocks.Add(new MarkdownBlock { Type = BlockType.HorizontalRule });
                    i++;
                    continue;
                }

                // Headings
                var headingMatch = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
                if (headingMatch.Success)
                {
                    var level = headingMatch.Groups[1].Length;
                    var content = headingMatch.Groups[2].Value;
                    blocks.Add(new MarkdownBlock
                    {
                        Type = level switch
                        {
                            1 => BlockType.Heading1,
                            2 => BlockType.Heading2,
                            3 => BlockType.Heading3,
                            4 => BlockType.Heading4,
                            5 => BlockType.Heading5,
                            _ => BlockType.Heading6
                        },
                        Content = content
                    });
                    i++;
                    continue;
                }

                // Ordered list
                var orderedListMatch = Regex.Match(line, @"^(\s*)(\d+)\.\s+(.+)$");
                if (orderedListMatch.Success)
                {
                    var indent = orderedListMatch.Groups[1].Length;
                    var content = orderedListMatch.Groups[3].Value;
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.OrderedListItem,
                        Content = content,
                        IndentLevel = indent / 2
                    });
                    i++;
                    continue;
                }

                // Unordered list
                var listMatch = Regex.Match(line, @"^(\s*)([-*+])\s+(.+)$");
                if (listMatch.Success)
                {
                    var indent = listMatch.Groups[1].Length;
                    var content = listMatch.Groups[3].Value;
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.ListItem,
                        Content = content,
                        IndentLevel = indent / 2
                    });
                    i++;
                    continue;
                }

                // Block quote
                if (line.TrimStart().StartsWith(">"))
                {
                    var content = line.TrimStart().Substring(1).TrimStart();
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.BlockQuote,
                        Content = content
                    });
                    i++;
                    continue;
                }

                // Regular paragraph
                var paragraph = ParseParagraph(lines, ref i);
                if (!string.IsNullOrWhiteSpace(paragraph))
                {
                    blocks.Add(new MarkdownBlock
                    {
                        Type = BlockType.Paragraph,
                        Content = paragraph
                    });
                }
            }

            return blocks;
        }

        private static MarkdownBlock ParseCodeBlock(string[] lines, ref int index)
        {
            var startLine = lines[index];
            var languageMatch = Regex.Match(startLine.TrimStart(), @"```(\w*)");
            var language = languageMatch.Groups[1].Value;

            index++; // Move past the opening ```

            var codeLines = new List<string>();
            while (index < lines.Length)
            {
                if (lines[index].TrimStart().StartsWith("```"))
                {
                    index++; // Move past the closing ```
                    break;
                }
                codeLines.Add(lines[index]);
                index++;
            }

            return new MarkdownBlock
            {
                Type = BlockType.CodeBlock,
                Content = string.Join("\n", codeLines),
                Language = language
            };
        }

        private static string ParseParagraph(string[] lines, ref int index)
        {
            var paragraphLines = new List<string>();

            while (index < lines.Length)
            {
                var line = lines[index];

                // Stop at empty line
                if (string.IsNullOrWhiteSpace(line))
                {
                    index++;
                    break;
                }

                // Stop at special syntax
                if (line.TrimStart().StartsWith("#") ||
                    line.TrimStart().StartsWith("```") ||
                    line.TrimStart().StartsWith(">") ||
                    Regex.IsMatch(line.Trim(), @"^[-*+]\s") ||
                    Regex.IsMatch(line.Trim(), @"^\d+\.\s") ||
                    Regex.IsMatch(line.Trim(), @"^(\*\*\*+|---+|___+)$"))
                {
                    break;
                }

                paragraphLines.Add(line);
                index++;
            }

            return string.Join(" ", paragraphLines);
        }

        private static void RenderBlock(MarkdownBlock block)
        {
            switch (block.Type)
            {
                case BlockType.Heading1:
                    EditorGUILayout.LabelField(ProcessInlineMarkdown(block.Content), _headingStyle1);
                    break;

                case BlockType.Heading2:
                    EditorGUILayout.LabelField(ProcessInlineMarkdown(block.Content), _headingStyle2);
                    break;

                case BlockType.Heading3:
                    EditorGUILayout.LabelField(ProcessInlineMarkdown(block.Content), _headingStyle3);
                    break;

                case BlockType.Heading4:
                case BlockType.Heading5:
                case BlockType.Heading6:
                    EditorGUILayout.LabelField(ProcessInlineMarkdown(block.Content), _headingStyle4);
                    break;

                case BlockType.CodeBlock:
                    EditorGUILayout.TextArea(block.Content, _codeBlockStyle);
                    break;

                case BlockType.ListItem:
                    var listIndent = block.IndentLevel * 15;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(listIndent);
                    EditorGUILayout.LabelField("• " + ProcessInlineMarkdown(block.Content), _listStyle);
                    EditorGUILayout.EndHorizontal();
                    break;

                case BlockType.OrderedListItem:
                    var orderedIndent = block.IndentLevel * 15;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(orderedIndent);
                    EditorGUILayout.LabelField("1. " + ProcessInlineMarkdown(block.Content), _listStyle);
                    EditorGUILayout.EndHorizontal();
                    break;

                case BlockType.BlockQuote:
                    EditorGUILayout.LabelField(ProcessInlineMarkdown(block.Content), _quoteStyle);
                    break;

                case BlockType.HorizontalRule:
                    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                    break;

                case BlockType.Paragraph:
                    EditorGUILayout.LabelField(ProcessInlineMarkdown(block.Content), _paragraphStyle);
                    break;
            }
        }

        private static string ProcessInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Process inline code first (to avoid processing markdown inside code)
            text = Regex.Replace(text, @"`([^`]+)`", match =>
            {
                var code = match.Groups[1].Value;
                return $"<color=#D4D4D4><b>{EscapeRichText(code)}</b></color>";
            });

            // Bold and Italic (***text*** or ___text___)
            text = Regex.Replace(text, @"(\*\*\*|___)(.+?)\1", "<b><i>$2</i></b>");

            // Bold (**text** or __text__)
            text = Regex.Replace(text, @"(\*\*|__)(.+?)\1", "<b>$2</b>");

            // Italic (*text* or _text_)
            text = Regex.Replace(text, @"(\*|_)(.+?)\1", "<i>$2</i>");

            // Strikethrough (~~text~~)
            text = Regex.Replace(text, @"~~(.+?)~~", "<s>$1</s>");

            // Links [text](url) - just show the text
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");

            return text;
        }

        private static string EscapeRichText(string text)
        {
            return text.Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}