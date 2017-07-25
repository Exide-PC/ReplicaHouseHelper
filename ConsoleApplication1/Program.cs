using ConsoleApplication1.site;
using ConsoleApplication1.site.DataStructs;
using ConsoleApplication1.site.inheritance;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace GlobalHotkeyExampleForm
{
    class Program
    {
        public static void Main()
        {
            Uri url = new Uri("https://www.replicahouse.ru/", UriKind.Absolute);
            ReplicaHouse rh = new ReplicaHouse();
            //rh.UpdatePageCache(true);
            rh.UpdateProductCache();
                       
        }

    }
}
 