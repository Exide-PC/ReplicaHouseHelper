
using ConsoleApplication1.site.interfaces;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using ConsoleApplication1.site.DataStructs;

namespace ConsoleApplication1.site.inheritance
{
    public class CachedSite: IObserver<Robot.RobotEvtArgs>
    {      
        public Uri BaseUrl { get; }
        public Uri[] URLs => pages.Select(resp => resp.Url).ToArray();
        public Page[] Pages => this.pages.ToArray();

        protected string OwnDir { get; }
        protected bool IsValidPageCache { get; private set; }

        HashSet<Page> pages;
        Robot robot;
        RobotSession currentSession;
        XmlSerializer sessionSerializer;   
        string robotSessionPath;
        int downloadsUntilSave = 25;
        int downloadCounter = 0;

        static Regex siteNamePattern = new Regex("(^(www|wap)\\.)|(\\.[a-zA-Z]+$)", RegexOptions.Compiled);
        static Regex accessibleSymbols = new Regex(@"[a-zA-Z._=0-9-]{1}", RegexOptions.Compiled);
        
        public CachedSite(string siteUrl, string parentHolderDir = null)
        {
            Uri createdUri;
            if (Uri.TryCreate(siteUrl, UriKind.Absolute, out createdUri))
                this.BaseUrl = createdUri;
            else throw new Exception($"Невозможно создание абсолютного URL из {siteUrl}");
            
            this.pages = new HashSet<Page>();
            this.robot = new Robot(this.BaseUrl.AbsoluteUri);

            string siteName = this.GetSiteName(this.BaseUrl);

            if (string.IsNullOrEmpty(parentHolderDir))
                this.OwnDir = $@"{siteName}_pages";
            else
                this.OwnDir = $@"{parentHolderDir}\{siteName}_pages";
            this.robotSessionPath = $@"{this.OwnDir}\Session.xml";
            
            this.InitOwnDir();
            this.sessionSerializer = new XmlSerializer(typeof(RobotSession));

            if (PreviosSessionExists())
            {
                using (StreamReader sr = File.OpenText(this.robotSessionPath))
                {
                    RobotSession lastRobotSession = (RobotSession)sessionSerializer.Deserialize(sr);
                    this.IsValidPageCache = lastRobotSession.IsFinished;

                    foreach (Page foundPage in lastRobotSession.FoundPages)
                        this.pages.Add(foundPage);                    
                }
            }
            else this.IsValidPageCache = false;

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

        public bool PreviosSessionExists() => File.Exists(this.robotSessionPath);

        public HtmlDocument this[Uri url]
        {
            get
            {
                if (this.pages.Count == 0)
                    throw new InvalidDataException("Не сохранено ни одной страницы.");
                if (!this.IsValidPageCache)
                    throw new InvalidDataException($"Невалидный кеш страниц сайта {this.BaseUrl}");

                HtmlDocument doc;
                bool success = this.TryGetCachedPage(url, out doc);

                if (success)
                    return doc;
                else
                    throw new FileNotFoundException($"Невозможно получить файл по адресу {UrlToPath(url)}");
            }

            //set
            //{
            //    CachePage(url, value, true);
            //}
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
                return;
            
            this.currentSession = new RobotSession();
            this.downloadCounter = 0;

            // Если нужно продолжить прошлую сессию, то дополняем текущую сессию данными
            if (continueSession && File.Exists(this.robotSessionPath))
                using (StreamReader sr = File.OpenText(this.robotSessionPath))
                {
                    RobotSession lastSession = (RobotSession)sessionSerializer.Deserialize(sr);
                    
                    foreach (Page urlResponse in lastSession.FoundPages)
                    {
                        this.currentSession.FoundPages.Add(urlResponse);
                        this.robot.AddSpottedUrl(urlResponse.Url);
                    }                        

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

            this.pages.Clear();
        }

        public HttpStatusCode GetUrlStatusCode(Uri url) =>
            this.pages.Where(resp => resp.Url == url).First().StatusCode;

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

            if (!this.IsValidPageCache || !File.Exists(filePath))
            {
                doc = null;
                return false;
            }

            string html = File.ReadAllText(filePath);

            doc = new HtmlDocument();
            doc.LoadHtml(html);

            return true;
        }

        void CachePage(Uri url, HtmlDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException("doc");

            // Сохраняем ссылку в текущем экземпляре

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
            switch (args.EvtType)
            {
                case Robot.RobotEvtArgs.RobotEvtType.PageResponse:
                    {       
                        Page page = new Page()
                        {
                            Url = args.Url,
                            StatusCode = args.Response.StatusCode
                        };
                        currentSession.FoundPages.Add(page);
                        this.pages.Add(page);

                        if (args.Response.StatusCode != System.Net.HttpStatusCode.OK)
                            return; // TODO: Обработка ссылок на 404            

                        
                        this.CachePage(args.Url, args.Document);                        

                        currentSession.NotDequeuedUrls.Clear();
                        Uri[] notDequeuedUrls = robot.UrlQueue.ToArray();

                        foreach (Uri url in notDequeuedUrls)
                            currentSession.NotDequeuedUrls.Enqueue(url);

                        break;
                    }
                case Robot.RobotEvtArgs.RobotEvtType.UrlsOnPageFound:
                    {
                        Page targetPage = this.pages.First(page => page.Url == args.Url);

                        foreach (TypedUrl urlOnPage in args.UrlsOnPage)
                            targetPage.UrlsOnPage.Add(urlOnPage);

                        // Счётчик увеличивается только при нахождении валидных страниц
                        if (++downloadCounter % downloadsUntilSave == 0)
                            SaveCurrentSession();

                        break;
                    }
            }
        }

        void SaveCurrentSession()
        {
            if (currentSession == null)
                throw new InvalidDataException("Текущая сессия не валидна.");

            Console.WriteLine("Сохраняемся");

            using (FileStream fs = File.OpenWrite(this.robotSessionPath))
                sessionSerializer.Serialize(fs, currentSession);
        }

        public List<Uri> UrlsByStatusCode(HttpStatusCode statusCode)
        {
            if (!this.IsValidPageCache)
                throw new InvalidDataException("Кэш сайта не валиден");

            return this.pages
                .Where(resp => resp.StatusCode == statusCode)
                .Select(resp => resp.Url)
                .ToList();
        }
        
        public HttpStatusCode StatusCodeByUrl(Uri targetUrl)
        {
            if (!this.IsValidPageCache)
                throw new InvalidDataException("Невалидный кеш страниц.");

            return this.pages.Where(urlResponce => urlResponce.Url == targetUrl).First().StatusCode;
        }        

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            this.currentSession.IsFinished = true;
            this.IsValidPageCache = true;
            SaveCurrentSession();
        }      
    }
}
