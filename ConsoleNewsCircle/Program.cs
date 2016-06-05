using cn.bmob.api;
using cn.bmob.io;
using cn.bmob.json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ConsoleNewsCircle
{
    class Program
    {
        const string TABLE_NAME = "NewsList";
        const int UPDATE_PAGE = 10;

        static string[] newsTypeSet = new string[]
            { "ACGN", "FOOD", "TECH", "FINA", "SPOR", "LIFE" };
        static string[] requestTypeSet = new string[]
            {"comic","food","tech","finance","sports","society"};

        static int[] uploadCounter = new int[6];
        static int[] errorCounter = new int[6];
        static BmobWindows Bmob = new BmobWindows();

        static void Main(string[] args)
        {
            // 初始化Bmob
            Bmob.initialize("6eb0ac4e1ad9afe743725acc74950d8a", "4342d28839efc8967ddd8ddf92b1d0ef");
            for (int i = 0; i < newsTypeSet.Length; i++)
            {
                // 查找云端最新条目 
                BmobQuery query = new BmobQuery();
                query.WhereEqualTo("newsType", newsTypeSet[i]);
                query.OrderByDescending("newsSerial");
                query.Select("newsUrl");
                query.Limit(1);
                var futureQ = Bmob.FindTaskAsync<NewsList>(TABLE_NAME, query);
                // 截取最新条目的 newsUrl
                string urlJSON = JsonAdapter.JSON.ToJsonString(futureQ.Result);
                string newsUrl = "";
                if (urlJSON.Length > 20)
                {
                    var start = "\":\"";
                    var end = "\"";
                    int indexS = urlJSON.IndexOf(start) + start.Length;
                    int indexE = urlJSON.LastIndexOf(end);
                    newsUrl = urlJSON.Substring(indexS, indexE - indexS);
                    Console.WriteLine(newsUrl);
                }

                Console.WriteLine("\n\n|-----------------【" + newsTypeSet[i] + "】-----------------|\n\n");

                // 利用API拉去新闻加入 mList
                List<NewsList> mList = new List<NewsList>();
                for (int j = 1; j <= UPDATE_PAGE; j++)
                {
                    string url = "http://apis.baidu.com/3023/news/channel";
                    string param = "id=" + requestTypeSet[i] + "&page=" + j;
                    string result = request(url, param);

                    JObject resultObject = JObject.Parse(result);
                    IList<JToken> results = resultObject["data"]["article"].Children().ToList();
                    foreach (var item in results)
                    {
                        NewsList _game = JsonConvert.DeserializeObject<NewsList>(item.ToString());

                        var allHtml = GetWebClient(_game.url);
                        var start = "<div id=\"main\"";
                        var end = "<script>process()";
                        int indexS = allHtml.IndexOf(start);
                        int indexE = allHtml.LastIndexOf(end);

                        _game.newsContent = allHtml.Substring(indexS, indexE - indexS)
                            .Replace("src=\"http://img.3023.com/s.gif\" data-", "");
                        _game.newsType = newsTypeSet[i];
                        _game.newsTime = getNewsTime(_game.time);
                        Console.WriteLine("title: {0}\nurl: {1}\nimg: {2}\nauthor: {3}\ntime: {4}",
                        _game.title, _game.url, _game.img, _game.author, _game.newsTime);

                        mList.Add(_game);
                    }
                }       
                        
                Console.WriteLine("\n\n|---------------截取后【" + newsTypeSet[i] + "】---------------|\n\n");
              
                // 截取非重复项
                int cutOfIndex = -1;
                mList.Reverse();
                for (int j = 0; j < mList.Count; j++)
                {
                    if (newsUrl.Equals(mList[j].url))
                    {
                        cutOfIndex = j;
                        break;
                    }
                }
                if (cutOfIndex != -1)
                {
                    // 存在重复项，需要切割
                    mList.RemoveRange(0, cutOfIndex + 1);
                }
                uploadCounter[i] = mList.Count;
                // 上传至云端
                foreach (var item in mList)
                {
                    try
                    {
                        var future = Bmob.CreateTaskAsync(item);
                        Console.WriteLine(future.Result + "\n");
                    }
                    catch (AggregateException e)
                    {
                        // 需要打印错误日志
                        errorCounter[i]++;
                    }
                }

            }
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "进度报告: \n");
            for (int i = 0; i < newsTypeSet.Length; i++)
            {
                Console.WriteLine("类型: {0} 更新条目: {1} 失败条目: {2}",
                    newsTypeSet[i], uploadCounter[i], errorCounter[i]);
            }
            Console.ReadLine();
        }

        // 新闻API的请求方法
        public static string request(string url, string param)
        {
            string strURL = url + '?' + param;
            System.Net.HttpWebRequest request;
            request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
            request.Method = "GET";
            // 添加header
            request.Headers.Add("apikey", "ce4a2788706720ba907955a176b63e54");
            System.Net.HttpWebResponse response;
            response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.Stream s;
            s = response.GetResponseStream();
            string StrDate = "";
            string strValue = "";
            StreamReader Reader = new StreamReader(s, Encoding.UTF8);
            while ((StrDate = Reader.ReadLine()) != null)
            {
                strValue += StrDate + "\r\n";
            }
            return strValue;
        }

        // 获取URL所对应的HTML代码
        private static string GetWebClient(string url)
        {
            string strHTML = "";
            WebClient myWebClient = new WebClient();
            Stream myStream = myWebClient.OpenRead(url);
            StreamReader sr = new StreamReader(myStream, System.Text.Encoding.GetEncoding("utf-8"));
            try
            {
                strHTML = sr.ReadToEnd();
            }
            catch (IOException e)
            {
                myStream.Close();
                return "";
            }
            myStream.Close();
            return strHTML;
        }

        // 根据Unix时间戳转换为自定义的时间格式
        private static string getNewsTime(long timeStamp)
        {
            //设置计算起始时间
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            //使用这些时间间隔构造一个TimeSpan对象。
            TimeSpan toNow = new TimeSpan(lTime);
            //把TimeSpan对象，加到当前时区的1970起始时间上。
            return dtStart.Add(toNow).ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
