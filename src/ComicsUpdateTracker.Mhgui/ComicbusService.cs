using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using ComicsUpdateTracker.Mhgui.Models;
using HtmlAgilityPack;
using ScrapySharp.Extensions;

namespace ComicsUpdateTracker.Mhgui
{
    public class ComicbusService
    {
        public const string WELL_KNOWN_HOST = "https://www.comicbus.com/";

        public static Regex CHAPTER_ID_REGEX = new Regex(@"cview\(\'(?<id>.+).html\',\d+,\d+\);");
        public static Regex COMIC_NAME = new Regex(@"addhistory\(\""\d+\"",\""(?<name>.+)\""\);");
        private readonly string _host;
        private readonly HttpClient _http;

        public ComicbusService(string host = WELL_KNOWN_HOST)
        {
            _host = host;
            _http = new HttpClient()
            {
                BaseAddress = new Uri(host)
            };
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task<Comic> GetComicById(string id)
        {
            var htmlBytes = await _http.GetByteArrayAsync($"html/{id}.html");
            var html = Encoding.GetEncoding("Big5").GetString(htmlBytes);

            // From String
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var comic = new Comic();

            comic.Name = COMIC_NAME.Match(html).Groups["name"].Value;
            comic.Tags = new string[]{doc.DocumentNode.CssSelect("a").First(x => x.InnerText.Trim().EndsWith("系列")).InnerText};
            comic.Year = null;
            comic.Status = doc.DocumentNode.CssSelect("a[href='#Comic']").First().LastChild.InnerText.Trim();

            var authorText = doc.DocumentNode.CssSelect("img[src='../images/bl.gif']").ToArray()[1]
                                .ParentNode.NextSibling.NextSibling.InnerText.Trim();
            authorText    = HttpUtility.HtmlDecode(authorText);
            comic.Authors = authorText.Split(",");
            
            var description = doc.DocumentNode.CssSelect("table[style='padding:10px; border-top:1px dotted #cccccc']").First().InnerText;
            comic.Description = HttpUtility.HtmlDecode(description).Trim();


            var dateTimeText = doc.DocumentNode.CssSelect("img[src='../images/bl.gif']").ToArray()[4]
                                .ParentNode.NextSibling.NextSibling.InnerText.Trim();
            comic.LastModifyTime = DateTime.Parse(dateTimeText.Trim());


            var chapterEles = doc.DocumentNode.CssSelect("#rp_tb_comic_0 td")
                                 .Where(x=>x.InnerHtml.Contains("cview("))
                                 .Select(x=>x.CssSelect("a").First());
            List<Chapter> chapters = new List<Chapter>();
            foreach (var chapterEle in chapterEles)
            {
                var chapter = new Chapter()
                {
                    Id = CHAPTER_ID_REGEX.Match(chapterEle.OuterHtml).Groups["id"].Value.Trim(),
                    Title = chapterEle.InnerText.Trim()
                };
                var ids = chapter.Id.Split("-");
                chapter.Url = $"https://comic.aya.click/online/best_{ids[0]}.html?ch={ids[1]}";
                if (chapters.Any(x => x.Id == chapter.Id))
                {
                    continue;
                }
                chapters.Add(chapter);
            }

            comic.ChapterCollection = new ChapterCollection[]
            {
                new ChapterCollection()
                {
                    Type     = "Default",
                    Chapters = chapters
                }
            };

            comic.NewestChapter = comic.GetAllChapters()?.LastOrDefault();

            return comic;
        }
    }
}
