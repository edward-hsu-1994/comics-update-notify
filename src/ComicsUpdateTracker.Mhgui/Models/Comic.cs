using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;

namespace ComicsUpdateTracker.Mhgui.Models
{
    public class Comic
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public IEnumerable<string> Authors { get; set; }
        public string Year { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public DateTime LastModifyTime { get; set; }
        public Chapter NewestChapter { get; set; }
        public IEnumerable<ChapterCollection> ChapterCollection { get; set; }
        public string Description { get; internal set; }
    }
}
