using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1.site.Utils
{
    public class TypedUrl
    {
        public Uri Url { get; set; }
        public Robot.LinkType UrlType { get; set; }

        public TypedUrl() { }

        public TypedUrl(Uri foundUri, Robot.LinkType type)
        {
            this.Url = foundUri;
            this.UrlType = type;
        }
    }
}
