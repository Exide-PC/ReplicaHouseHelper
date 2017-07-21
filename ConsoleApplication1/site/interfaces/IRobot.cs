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
    interface IRobot: IObservable<Robot.RobotEvtArgs>
    {        
        event EventHandler<Robot.RobotEvtArgs> OnPageResponse;
        event EventHandler<Robot.RobotEvtArgs> OnUrlsOnPageFound;
        //event EventHandler<Robot.RobotEvtArgs> OnErrorGettingUrls;
        event EventHandler<Robot.RobotEvtArgs> OnFinish;
                
        Uri BaseUrl { get; }
        bool IsBusy { get; }
        int ThreadCount { get; }
        Queue<Uri> UrlQueue { get; }

        void Run();
        HtmlDocument GetHtmlFrom(Uri url);
        bool TryParseUrlsAt(HtmlDocument doc, out List<TypedUrl> foundUrls);

        Dictionary<Uri, HtmlDocument> GetAllPages();           
    }
}
