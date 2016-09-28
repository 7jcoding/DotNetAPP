using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace DotNetAPP.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            var options = new RedisCacheOptions
            {
                Configuration = "192.168.1.18:6379,password=fanhuan",
                InstanceName = "DotNetCore:"
            };
            var cache = new RedisCache(options);
            var cacheKey = "redis-test";
            var cacheValue = Encoding.UTF8.GetBytes("Hello, World!");
            cache.SetAsync(cacheKey, cacheValue, new DistributedCacheEntryOptions() { AbsoluteExpiration = DateTime.Now.AddSeconds(60) });
            //cache.Set(cacheKey, cacheValue);
            ViewBag.Redis = cache.GetString(cacheKey);
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Tools()
        {
            ViewData["Message"] = "Tools page.";            
            if (Request.Method == "POST")
            {
                var result = string.Empty;
                var url = Request.Form["url"].ToString().ToLower();
                if (string.IsNullOrWhiteSpace(Request.Form["url"]) || (!url.Contains("http://") && !url.Contains("https://")))
                {
                    result = "您输入的链接不是有效的商品链接...";
                }
                else
                {
                    HtmlWeb htmlWeb = new HtmlWeb();
                    HtmlDocument htmlDoc = htmlWeb.LoadFromWebAsync(url, Encoding.UTF8).Result;
                    var htmlCode = htmlDoc.DocumentNode.DescendantsAndSelf("html").First().InnerHtml;
                    var reg = new Regex("//dsc.taobaocdn.com/[^\']+", RegexOptions.IgnoreCase);
                    Match m = reg.Match(htmlCode);
                    if (m.Success)
                    {
                        url = "http:" + m.Value;
                        htmlCode = WebRequest(url);
                        reg = new Regex("src=\"([^\"]+)", RegexOptions.IgnoreCase);
                        MatchCollection mc = reg.Matches(htmlCode);
                        foreach (Match _m in mc)
                        {
                            result += string.Format("<img src=\"{0}\"><br/>", _m.Groups[1].Value);
                        }
                    }
                    else
                    {
                        result = htmlCode;
                    }
                }
                ViewBag.Result = result;
            }
            return View(ViewBag);
        }

        public IActionResult Error()
        {
            return View();
        }

        public string WebRequest(string url, string method = "GET", string data = null, string encoding = "UTF-8")
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = method == "GET" ? method : "POST";
            if (method == "POST" && !string.IsNullOrEmpty(data))
            {
                request.ContentType = "application/x-www-form-urlencoded";
                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStreamAsync().Result))
                {
                    streamWriter.Write(data);
                }
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponseAsync().Result;
            using (Stream stream = response.GetResponseStream())
            {
                bool hasGzipCompress = response.Headers["Content-Encoding"] != null && response.Headers["Content-Encoding"].ToLower() == "gzip";
                GZipStream gzip = hasGzipCompress ? new GZipStream(stream, System.IO.Compression.CompressionMode.Decompress) : null;
                using (StreamReader streamReader = new StreamReader(hasGzipCompress ? gzip : stream, Encoding.GetEncoding(encoding)))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
