using System;
using System.Collections.Generic;
using System.Text;

namespace ComicsUpdateTracker.Mhgui.Models
{
    public class ChapterCollection
    {
        public string Type { get; set; }
        public IEnumerable<Chapter> Chapters { get; set; }
    }
}
