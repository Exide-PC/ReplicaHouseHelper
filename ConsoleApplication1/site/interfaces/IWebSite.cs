using ConsoleApplication1.site;
using ConsoleApplication1.site.Utils;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1.site.interfaces
{
    interface IWebSite
    {
        Uri BaseUrl { get; }            
        void SavePage(Uri url, HtmlDocument doc);        
        HtmlDocument this[Uri url] { get; set; }
        bool TryGetUrlsAt(Uri url, out TypedUrl[] foundUrls);        
    }
}
