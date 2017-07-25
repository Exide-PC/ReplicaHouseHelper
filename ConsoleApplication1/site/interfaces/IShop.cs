using ConsoleApplication1.site;
using ConsoleApplication1.site.DataStructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1.site.interfaces
{
    interface IShop
    {
        Product[] Products { get; }
        Uri[] ProductUrls { get; }
        bool HasProducts { get; }
    }
}
