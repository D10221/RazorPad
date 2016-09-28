using System;
using System.Linq;
using System.Web.Razor.Parser.SyntaxTree;

namespace RazorPad.UI.Editors.Folding
{
    public class RazorCodeSpanParser
    {
        public static string GetBlockName(Block block)
        {
            const string defaultName = "...";
            switch (block.Type)
            {
                case BlockType.Statement:
                    return "{...}";
                case BlockType.Directive:
                    break;
                case BlockType.Functions:
                    return "@funtions";
                case BlockType.Expression:
                    break;
                case BlockType.Helper:
                    return GetHelperBlockName(block);
                case BlockType.Markup:
                    break;
                case BlockType.Section:
                    return GetSectionBlockName(block);
                case BlockType.Template:
                    break;
                case BlockType.Comment:
                    return GetCommentBlockName(block);
                default:
                    return defaultName;
            }
            return defaultName;
        }

        private static string GetCommentBlockName(Block block)
        {
            return "@**@";
        }

        private static string GetSectionBlockName(Block block)
        {
            var sectionName = "";
            //TODO:
            //if (block.Children != null)
            //{
            //    var sectionHeader =
            //        block.Children.FirstOrDefault(c =>
            //        c.GetType().Name.Equals("SectionHeaderSpan")) 
            //        as MetaCodeSpan;
            //    sectionName = sectionHeader != null ? sectionHeader.SectionName : "";
            //}
            return string.Format("section {0}", sectionName);
        }

        private static string GetHelperBlockName(Block block)
        {
            var headerName = "";
            //TODO:
            //var helperHeader = block.Children.FirstOrDefault(c => c.GetType() == typeof (HelperHeaderSpan)) as HelperHeaderSpan;
            //if (helperHeader != null)
            //{
            //    headerName =
            //        helperHeader.Content.Substring(0, helperHeader.Content.IndexOf("(", StringComparison.Ordinal)).Trim();
            //}
            return string.Format("helper {0}", headerName);
        }
    }
}