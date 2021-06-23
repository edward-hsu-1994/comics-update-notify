using System;
using System.Collections.Generic;
using System.Text;

namespace ComicsUpdateTracker.Mhgui.Models
{
    public class Chapter
    {
        public string Id
        {
            get
            {
                if (Url == null) return null;
                try
                {
                    return MhguiService.CHAPTER_ID_REGEX.Match(Url).Groups["id"].Value;
                }
                catch
                {
                    return null;
                }
            }
        }
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
