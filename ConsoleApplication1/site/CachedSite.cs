
using ConsoleApplication1.site.interfaces;
using ConsoleApplication1.site.Utils;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace ConsoleApplication1.site
{
    class CachedSite: IObserver<Robot.RobotEvtArgs>
    {      
        public Uri BaseUrl { get; }
        public Uri[] URLs => urls.ToArray();
        protected bool IsValidPageCache => this.urls.Count > 0;
        RobotSession currentSession;

        string robotSessionPath;
        
        protected string OwnDir { get; }
        HashSet<Uri> urls;
        Robot robot;
        XmlSerializer serializer;
        XmlSerializer sessionSerializer;

        static Regex siteNamePattern = new Regex("(^(www|wap)\\.)|(\\.[a-zA-Z]+$)", RegexOptions.Compiled);
        static Regex accessibleSymbols = new Regex(@"[a-zA-Z._=0-9-]{1}", RegexOptions.Compiled);
        
        public CachedSite(string siteUrl, string parentHolderDir = null)
        {
            Uri createdUri;
            if (Uri.TryCreate(siteUrl, UriKind.Absolute, out createdUri))
                this.BaseUrl = createdUri;
            else throw new Exception($"Невозможно создание абсолютного URL из {siteUrl}");
            
            this.urls = new HashSet<Uri>();
            this.robot = new Robot(this.BaseUrl.AbsoluteUri);

            string siteName = this.GetSiteName(this.BaseUrl);

            if (string.IsNullOrEmpty(parentHolderDir))
                this.OwnDir = $@"{siteName}_pages";
            else
                this.OwnDir = $@"{parentHolderDir}\{siteName}_pages";
            this.robotSessionPath = $@"{this.OwnDir}\Session.xml";
            
            this.InitOwnDir();
            this.serializer = new XmlSerializer(typeof(string[]));
            this.sessionSerializer = new XmlSerializer(typeof(RobotSession));

            if (File.Exists(this.robotSessionPath))
            {
                using (StreamReader sr = File.OpenText(this.robotSessionPath))
                {
                    RobotSession lastRobotSession = (RobotSession)sessionSerializer.Deserialize(sr);

                    if (lastRobotSession.IsFinished)
                        foreach (Uri url in lastRobotSession.FoundUrls)
                            this.urls.Add(url);
                }
            }

            /*if (File.Exists(this.holderFile))
                using (StreamReader sr = File.OpenText(this.holderFile))
                {
                    string[] strUrls = (string[])serializer.Deserialize(sr);
                    foreach (string url in strUrls)
                        this.urls.Add(new Uri(url, UriKind.Absolute));
                }*/

            /*string[] foundUrls = File.ReadAllLines(this.holderFile);                
            foreach (string foundUrl in foundUrls)
                this.urls.Add(new Uri(foundUrl, UriKind.Absolute));*/
        }


        public HtmlDocument this[Uri url]
        {
            get
            {
                if (this.urls.Count == 0)
                    throw new Exception("Не сохранено ни одной страницы.");

                HtmlDocument doc;
                bool success = this.TryGetCachedPage(url, out doc);

                if (success)
                    return doc;
                else
                    throw new FileNotFoundException($"Невозможно получить файл по адресу {UrlToPath(url)}");
            }

            set
            {
                CachePage(url, value, true);
            }
        }

        protected string GetSiteName(Uri url)
        {
            string host = url.Host;
            string siteName = siteNamePattern.Replace(host, "");
            return siteName;
        }

        void InitOwnDir()
        {
            if (!Directory.Exists(this.OwnDir))
                Directory.CreateDirectory(this.OwnDir);

            //if (!File.Exists(this.holderFile))
            //    File.CreateText(this.holderFile).Close(); // TODO: Encode?
        }

        public void UpdatePageCache(bool continueSession = false)
        {
            if (continueSession && this.IsValidPageCache)
                throw new Exception("Невозможно продолжить прошлую сессию, т.к. она завершена.");
            
            this.currentSession = new RobotSession();

            // Если нужно продолжить прошлую сессию, то дополняем текущую сессию данными
            if (continueSession && File.Exists(this.robotSessionPath))
                using (StreamReader sr = File.OpenText(this.robotSessionPath))
                {
                    RobotSession lastSession = (RobotSession)sessionSerializer.Deserialize(sr);
                    
                    foreach (Uri foundUrl in lastSession.FoundUrls)
                        this.currentSession.FoundUrls.Add(foundUrl);

                    foreach (Uri notDequeuedUrl in lastSession.NotDequeuedUrls)
                        robot.UrlQueue.Enqueue(notDequeuedUrl);
                }   
            else
            {
                this.Delete();
                this.InitOwnDir();
            }

            robot.Subscribe(this);
            
            /*EventHandler<Robot.RobotEvtArgs> downloadHandler = null;
            EventHandler<Robot.RobotEvtArgs> cleaner = null;
                                
            downloadHandler = (obj, args) =>
            {
                this.CachePage(args.Url, args.Document, false);

                currentSession.FoundUrls.Add(args.Url);
                currentSession.NotDequeuedUrls.Clear();

                Uri[] notDequeuedUrls = robot.UrlQueue.ToArray();

                foreach (Uri url in notDequeuedUrls)
                    currentSession.NotDequeuedUrls.Enqueue(url);

                File.Delete(this.robotSessionPath);

                using (FileStream fs = File.OpenWrite(this.robotSessionPath))
                    sessionSerializer.Serialize(fs, currentSession);
            };
            robot.OnPageResponse += downloadHandler;

            cleaner = (obj, args) =>
            {
                robot.OnPageResponse -= downloadHandler;
                robot.OnFinish -= cleaner;
            };
            robot.OnFinish += cleaner;*/

            robot.Run();
        }
        
        public virtual void Delete()
        {
            if (Directory.Exists(this.OwnDir))
                Directory.Delete(this.OwnDir, true);

            while (Directory.Exists(this.OwnDir))
                Thread.Sleep(1);

            this.urls.Clear();
        }


        string UrlToPath(Uri url)
        {            
            string dirtyName = $@"{url.Host}{url.PathAndQuery}.html".Replace("/","_");

            StringBuilder builder = new StringBuilder($@"{this.OwnDir}\");

            foreach (char c in dirtyName)
                builder.Append(accessibleSymbols.IsMatch(c.ToString()) ? c : '_');

            return builder.ToString();
        }

        public bool TryGetCachedPage(Uri url, out HtmlDocument doc)
        {           
            string filePath = this.UrlToPath(url);

            if (!File.Exists(filePath))
            {
                doc = null;
                return false;
            }

            string html = File.ReadAllText(filePath);

            doc = new HtmlDocument();
            doc.LoadHtml(html);

            return true;
        }

        void CachePage(Uri url, HtmlDocument doc, bool addToList)
        {         
            // Сохраняем ссылку в текущем экземпляре
            if (addToList)
                this.urls.Add(url);

            // Заносим URL в конец файла со всеми URL
            /*string[] line = { url.AbsoluteUri };
            File.AppendAllLines(this.holderFile, line);*/
            //using (FileStream writer = File.OpenWrite(this.holderFile))
            //    this.serializer.Serialize(writer, this.urls.Select(u => u.AbsoluteUri).ToArray());

            // Сохраняем файл с Html-кодом
            string filePath = this.UrlToPath(url);
            if (File.Exists(filePath)) File.Delete(filePath);

            using (TextWriter writer = File.CreateText(filePath))
                doc.DocumentNode.WriteTo(writer);
        }

        public bool TryGetUrlsAt(Uri url, out TypedUrl[] foundUrls)
        {
            HtmlDocument doc = this[url];
            List<TypedUrl> foundUrlsList;

            if (this.robot.TryParseUrlsAt(doc, out foundUrlsList))
            {
                foundUrls = foundUrlsList.ToArray();
                return true;
            }
            else
            {
                foundUrls = null;
                return false;
            }
        }

        public void OnNext(Robot.RobotEvtArgs args)
        {
            if (args.EvtType != Robot.RobotEvtArgs.RobotEvtType.PageResponse)
                return;

            this.CachePage(args.Url, args.Document, false);

            currentSession.FoundUrls.Add(args.Url);
            currentSession.NotDequeuedUrls.Clear();

            Uri[] notDequeuedUrls = robot.UrlQueue.ToArray();

            foreach (Uri url in notDequeuedUrls)
                currentSession.NotDequeuedUrls.Enqueue(url);

            File.Delete(this.robotSessionPath);

            using (FileStream fs = File.OpenWrite(this.robotSessionPath))
                sessionSerializer.Serialize(fs, currentSession);
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            this.currentSession.IsFinished = true;
            throw new NotImplementedException();
        }

        /*public class Page
        {
            public Uri Url { get; set; }
            public TypedUrl[] ChildUrls { get; set; }

            public Page() { }
        }*/
    }
}
