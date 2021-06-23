using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ComicsUpdateTracker.Mhgui.Models;
using HtmlAgilityPack;
using ScrapySharp.Extensions;

namespace ComicsUpdateTracker.Mhgui
{
    public class MhguiService
    {
        public const string WELL_KNOWN_HOST = "https://www.mhgui.com/";

        public static Regex CHAPTER_ID_REGEX = new Regex(@"(?<id>\d+).html");

        private readonly string _host;
        private readonly HttpClient _http;

        public MhguiService(string host = WELL_KNOWN_HOST)
        {
            _host = host;
            _http = new HttpClient()
            {
                BaseAddress = new Uri(host)
            };
        }

        public async Task<Comic> GetComicById(string id)
        {
            var html = await _http.GetStringAsync($"comic/{id}/");

            // From String
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var comic = new Comic()
            {
                Name = doc.DocumentNode.CssSelect("h1").First().InnerText
            };

            comic.Year     = doc.DocumentNode.CssSelect(".detail-list>li>span>a").First().InnerText;
            comic.Location = doc.DocumentNode.CssSelect(".detail-list>li>span>a").Skip(1).First().InnerText;

            var detail = doc.DocumentNode.CssSelect(".detail-list>li>span>strong");


            var          authorElement = detail.First(x => x.InnerText == "漫画作者：").NextSibling;
            List<string> authors       = new List<string>();
            while (authorElement != null &&
                   authorElement.InnerText.Trim() != ",")
            {
                authors.Add(authorElement.InnerText);
                authorElement = authorElement?.NextSibling?.NextSibling;
            }

            comic.Authors = authors;


            var          tagElement = detail.First(x => x.InnerText == "漫画剧情：").NextSibling;
            List<string> tags       = new List<string>();
            while (tagElement != null &&
                   tagElement.InnerText.Trim() != ",")
            {
                tags.Add(tagElement.InnerText);
                tagElement = tagElement?.NextSibling?.NextSibling;
            }

            comic.Tags = tags;


            comic.Alias  = detail.First(x => x.InnerText == "漫画别名：").NextSibling.InnerText;
            comic.Status = detail.First(x => x.InnerText == "漫画状态：").NextSibling.InnerText;
            comic.LastModifyTime =
                DateTime.Parse(doc.DocumentNode.CssSelect(".status>span>.red").Skip(1).First().InnerText);
            comic.Description = doc.DocumentNode.CssSelect("#intro-cut").First().InnerText;

            #region newest

            var newestChapter = doc.DocumentNode.CssSelect(".status>span>a").FirstOrDefault();
            if (newestChapter != null)
            {
                comic.NewestChapter = new Chapter()
                {
                    Title = newestChapter.InnerText,
                    Url   = _host + newestChapter.GetAttributeValue("href")
                };
            }

            #endregion

            var chapterTypes = doc.DocumentNode.CssSelect("h4");
            var collections  = new List<ChapterCollection>();

            foreach (var chapterType in chapterTypes)
            {
                var chapterTypeEle = chapterType;
                while (chapterTypeEle.NextSibling != null &&
                       !chapterTypes.Contains(chapterTypeEle.NextSibling))
                {
                    chapterTypeEle = chapterTypeEle.NextSibling;
                    if (chapterTypeEle.HasClass("chapter-list"))
                    {
                        break;
                    }
                }

                if (!chapterTypeEle.HasClass("chapter-list"))
                {
                    continue;
                }
                
                List<Chapter> chapters = new List<Chapter>();
                foreach (var chapterEle in chapterTypeEle.CssSelect("li>a"))
                {
                    chapters.Add(new Chapter()
                    { 
                        Title = chapterEle.GetAttributeValue("title"),
                        Url = _host + chapterEle.GetAttributeValue("href")
                    });
                }

                collections.Add(new ChapterCollection()
                {
                    Type = chapterType.InnerText,
                    Chapters = chapters.OrderBy(x=>x.Id).ToArray()
                });
            }

            comic.ChapterCollection = collections;

            return comic;
        }
    }
}
