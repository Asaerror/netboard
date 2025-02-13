//#define forUser
using System.Text.RegularExpressions;
using System.Collections;
using System.Text.Json;

namespace Netboard_tools
{
    internal class Program
    {
        const string siteUrl = "https://netboard.wikidot.com/";
        const string module = "ajax-module-connector.php";
        const string hub = "cn:main";
        const string space = "                                    ";
        const string cat = "blog";
        static HttpClient http = new(new HttpClientHandler() { UseCookies = false});
        static void Main(string[] args)
        {
            Console.Title = $"Netboard后端小工具 浮云制作~";
            Console.WriteLine("请不要在程序运行时对控制台进行缩放，不然会报错(懒得修bug)\n按任意键继续");
#if forUser
            Console.ReadKey();
#endif
            int max = GetLatesPage(hub,cat);
            Console.WriteLine($"最新地址为{cat}:{max}");
            Console.Clear();
            int min = 0;
            http.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36 Edg/133.0.0.0");
            http.DefaultRequestHeaders.Add("Cookie", "wikidot_token7=123456");
            User fc = new User(7508990);
            var s = new Slider(max,min,Console.BufferWidth-20,' ','/',max);
            var position =Console.GetCursorPosition();
            for (int i = max;i>min;i--)
            {
                s.Render(i,false);
                Console.WriteLine($"\n------------\n");
                AddUVFromPage(i, cat);
                Console.Clear();
                Console.SetCursorPosition(position.Left, position.Top);
            }
            string uv = JsonSerializer.Serialize(User.all);
            string path = System.AppDomain.CurrentDomain.BaseDirectory + "/生成";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(path +"/user_UV.json",uv);

        }


        public class User
        {
            readonly int id;
            public List<int> pages { get; set; }
            public static Dictionary<int, User> all = new();
            public User(int id)
            {
                this.id = id;
                pages = new List<int>();
                all.Add(id, this);
            }
            public static User GetUser(int id)
            {
                if (!all.Keys.Contains(id))
                {
                    new User(id);
                }
                return all[id];
            }
            public static bool AddPage(int id, int page)
            {
                User user = GetUser(id);
                if (!user.pages.Contains(page)) { user.pages.Add(page); return true; } else { return false; }
            }
        }
        public class Slider
        {
            (int Left, int Top) position;
            readonly int max,min, size;
            int range =>  max - min;
            int current;
            readonly char empty,block;
            public Slider(int min,int max,int size,char empty,char block,int current =0)
            {
                this.max = min;
                this.min = max;
                this.size = size-2;
                this.empty = empty;
                this.block = block;
                this.current = current;
                position = Console.GetCursorPosition();
                Render(current,false);
            }
            public void Render(int c,bool t= true)
            {
                var pos = Console.GetCursorPosition();
                current = c;
                int p = (max - current) * size / range;
                Console.SetCursorPosition(position.Left, position.Top);
                Console.Write($"[{new String(block,p) + new String(empty, (size-p))}]{max - current}/{range}");
                if(t) Console.SetCursorPosition(pos.Left, pos.Top);

            }
        }
        static async Task<List<int>> GetUV(int pageId)
            {
            if (pageId <= 0) return new();
            try
            {

                Console.WriteLine("获取uv用户中...");
                var v = new Dictionary<string, string>() { { "pageId", pageId.ToString() }, { "moduleName", "pagerate/WhoRatedPageModule" }, { "wikidot_token7", "123456" } };
                var httpContent = new FormUrlEncodedContent(v);
                string uvList = http.PostAsync("https://netboard.wikidot.com/ajax-module-connector.php", httpContent).Result.Content.ReadAsStringAsync().Result;
                string pattern = @"userid=(?<userid>\d+)&";
                var uv = new List<int>();
                foreach (Match match in Regex.Matches(uvList, pattern))
                {
                    uv.Add(int.Parse(match.Groups["userid"].Value));
                }
                Console.WriteLine("获取成功!");
                uv.print();
                return await Task.FromResult(uv);
            }catch (System.Net.Http.HttpRequestException)
            {
                Console.WriteLine("页面不存在");
            }
            catch(Exception e){
                Console.WriteLine($"其它报错\n{e}\n---------\n等待发送其它请求");
                Thread.Sleep(Random.Shared.Next(1000,10000));
                return GetUV(pageId).Result;
            }
            return new List<int>();
        }
        
        static void GetPageID(int page ,out int pageId,out string pageTitle ,string catagory)
        {
            try{
                //获取页面
                string pageurl = siteUrl + catagory+":" + page;
                Console.WriteLine($"获取页面信息中... 地址: {catagory + page}");
                string pageContent = http.GetStringAsync(pageurl).Result;
                string pattern = @"<title>\s*(?<pagetitle>[^<]+?)\s*- Netboard\s*<\/title>[\s\S]*?WIKIREQUEST\.info\.pageId = (?<pageid>\d+)";
                //获取页面id和标题
                pageId = -1;
                pageTitle = "null";
                foreach (Match match in Regex.Matches(pageContent, pattern))
                {
                    pageId = int.Parse(match.Groups["pageid"].Value);
                    pageTitle = match.Groups["pagetitle"].Value;
                }
                
                Console.WriteLine($"获取页面信息完成! \n页面ID:{pageId.ToString()} 标题:\"{pageTitle}\"");
            }catch (System.AggregateException)
            {
                Console.WriteLine("页面不存在");
                pageId = -1;
                pageTitle = "null";
                return;
            } catch(Exception e)
            {
                Console.WriteLine($"页面ID获取报错\n{e}\n是否重新发送请求(Y/N)");
#if forUser
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    GetPageID(page, out pageId, out pageTitle, catagory);
                }
                else
                {
                    pageId = -1;
                    pageTitle = "null";
                }
#else
                GetPageID(page, out pageId, out pageTitle, catagory);
#endif

            }
        }
        static void AddUVFromPage(int page, string catagory)
        {
            foreach (var i in GetUV(page,catagory).Result){
                User.AddPage(i,page);
            }
        }

        static int GetLatesPage(string url,string catogory)
        {
            try
            {
                //获取页面
                string pageurl = siteUrl + url;
                Console.WriteLine($"获取页面信息中... 地址: {url}");
                string pageContent = http.GetStringAsync(pageurl).Result;
                string pattern = @"<a href=""/"+catogory+@":(?<pageid>\d+)"">.*?</a>";
                //获取页面id和标题
                int page = -1;
                foreach (Match match in Regex.Matches(pageContent, pattern))
                {
                    int l = int.Parse(match.Groups["pageid"].Value);
                    page = (l>page)?l:page;
                }
                Console.WriteLine($"获取页面信息完成! \n页面ID:{page}");
                return page;
            }catch (Exception e)
            {
                Console.WriteLine($"页面错误，请检测网络后联系开发人员");
                Console.WriteLine(e.ToString());
                return -1;
            }
        }
        #region 重载
        static async Task<List<int>> GetUV(int page, string catagory)
        {
            int pageId;
            GetPageID(page, out pageId,catagory);
            return await GetUV(pageId);
        }
        static void GetPageID(int page, out int pageId, string catagory)
        {
            GetPageID(page, out pageId, out _, catagory);
        }
        #endregion
    }
    public static class Extension
    {
        public static void print(this IEnumerable list)
        {
            Console.Write("[");
            foreach (var i in list)
            {
                Console.Write(i + ",");
            }
            Console.WriteLine("]");
        }
    }
}
