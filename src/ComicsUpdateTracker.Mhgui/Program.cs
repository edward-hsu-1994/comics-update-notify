using StackExchange.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ComicsUpdateTracker.Mhgui
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var dataDir          = GetDataDirectory();
            var trackingListPath = Path.Combine(dataDir, "tracking-list.json");

            if (!File.Exists(trackingListPath))
            {
                File.WriteAllText(trackingListPath, "[]");
            }

            var trackingListText = File.ReadAllText(trackingListPath);
            var trackingList     = System.Text.Json.JsonSerializer.Deserialize<string[]>(trackingListText);

            var comicService      = new ComicbusService();

            var messages = new List<string>();
            foreach (var comicId in trackingList)
            {
                var comic   = await comicService.GetComicById(comicId);
                
                Console.WriteLine($"{comic.Name}");

                var logPath = Path.Combine(GetMhguiLogDirectory(), $"{comicId}.json");

                var allChapters = comic.GetAllChapters().ToArray();
                if (File.Exists(logPath))
                {
                    var logChapters = JsonSerializer.Deserialize<string[]>(File.ReadAllText(logPath));

                    var allChapterIds = allChapters.Select(x => x.Id).ToArray();

                    var newChapters =
                        allChapters.Where(x => allChapterIds.Except(logChapters).Contains(x.Id))
                                   .ToArray();

                    if (newChapters.Any())
                    {
                        var message = $"[{comic.Name}] 有新的章節: \r\n";
                        foreach (var chap in newChapters)
                        {
                            message += $"* {chap.Title}: {chap.Url}";
                        }

                        messages.Add(message);
                        Console.WriteLine(message);
                    }
                }
                else
                {
                    var msg = $"開始追蹤 [{comic.Name}] 的更新";
                    Console.WriteLine(msg);
                    messages.Add(msg);
                }

                File.WriteAllText(
                    logPath,
                    JsonSerializer.Serialize(allChapters.Select(x => x.Id)));
                Console.WriteLine(logPath);
            }

            if (messages.Count > 0)
            {
                await SendLineNotify(string.Join("\r\n\r\n", messages));
            }
        }


        public static string GetDataDirectory()
        {
            var workDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }

            return workDir;
        }

        public static string GetMhguiLogDirectory()
        {
            var workDir = Path.Combine(GetDataDirectory(), "logs");
            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }

            return workDir;
        }

        public static async Task SendLineNotify(string message)
        {
            var formData = new NameValueCollection();
            formData["message"] = "\r\n" + message;
            await Http.Request("https://notify-api.line.me/api/notify")
                      .AddHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("LINE_NOTIFY_TOKEN"))
                      .SendForm(formData)
                      .ExpectHttpSuccess()
                      .PostAsync();
        }
    }
}
