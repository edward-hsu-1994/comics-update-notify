using System;
using System.Threading.Tasks;

namespace ComicsUpdateTracker.Mhgui
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var mhs = new MhguiService();
            var comic = await mhs.GetComicById("23270");

        }
    }
}
