using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EF_OrigCatalog_Nissan_Test
{
    public class PageDocument : IDisposable
    {
        HtmlParser htmlParser;
        string pageSting;
        IHtmlDocument document;

        public PageDocument(string content)
        {
            htmlParser = new HtmlParser();
            pageSting = content;
            document = htmlParser.Parse(content);
        }

        public void Dispose()
        {
            pageSting = null;
            htmlParser = null;
            if (document != null)
                document.Dispose();
        }

        public IHtmlCollection<IElement> Select_All(string pattern)
        {
            return document.QuerySelectorAll(pattern);

        }
        public IElement Select_Single(string pattern)
        {
            return document.QuerySelector(pattern);

        }
        public static IEnumerable<string> GetJSLinkParams(IHtmlAnchorElement anchor)
        {
            List<string> js_params = new List<string>();
            var matches = Regex.Matches(anchor.Href, @"'([^']|\\')*'");
            foreach (var item in matches)
            {
                js_params.Add(item.ToString().Replace("'", string.Empty));
            }
            return js_params;
        }
    }

}
