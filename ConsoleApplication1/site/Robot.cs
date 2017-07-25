

using ConsoleApplication1.site.DataStructs;
using ConsoleApplication1.site.interfaces;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace ConsoleApplication1.site
{
    public class Robot : IRobot, IObservable<Robot.RobotEvtArgs>
    {
        public enum LinkType { File, Absolute, RelativeToBase, Undetermined }

        public Uri BaseUrl { get; }
        public bool IsBusy { get; private set; }
        public int ThreadCount { get; private set; }
        public Queue<Uri> UrlQueue { get; }
        HashSet<Uri> spottedUrls;
        List<IObserver<RobotEvtArgs>> observers;

        HtmlWeb web;

        public Robot(string baseUrl)
        {
            Uri createdUri;
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out createdUri))
                this.BaseUrl = createdUri;
            else throw new Exception($"Невозможно создание абсолютного URL из {baseUrl}");

            this.web = new HtmlWeb();
            this.UrlQueue = new Queue<Uri>();
            this.spottedUrls = new HashSet<Uri>();
            this.observers = new List<IObserver<RobotEvtArgs>>();
        }

        public event EventHandler<RobotEvtArgs> OnPageResponse;   
        public event EventHandler<RobotEvtArgs> OnUrlsOnPageFound;
        //public event EventHandler<RobotEvtArgs> OnErrorGettingUrls;
        public event EventHandler<RobotEvtArgs> OnFinish;

        public void AddSpottedUrl(Uri url) => this.spottedUrls.Add(url);

        public void Run()
        {
            if (this.IsBusy) throw new RobotException($"Робот уже занят обходом ресурса {this.BaseUrl}");

            this.IsBusy = true; // Переводим его в занятое состояние
                 
            //HashSet<Uri> spottedUrls = new HashSet<Uri>(); // Uri, т.к. быстрее и проще
            int counter = 0;

            // Обрабатываем Url, не догруженные с прошлой сессии
            if (this.UrlQueue.Count == 0)
            {
                this.UrlQueue.Enqueue(this.BaseUrl);
                AddSpottedUrl(this.BaseUrl);
            }
            else
            {
                foreach (Uri enqueuedUrl in this.UrlQueue)
                    AddSpottedUrl(enqueuedUrl);
            }
                
            
            while (UrlQueue.Count > 0)
            {
                Uri nextUrl = UrlQueue.Dequeue();
                //AddSpottedUrl(nextUrl);
                //this.OnUrlDequeued?.Invoke(this, RobotEvtArgs.UrlDequeued(nextUrl.Url, nextUrl.UrlType));

                // new implementation
                HttpWebResponse responce = this.GetResponse(nextUrl);
                
                //foreach (IObserver<RobotEvtArgs> obs in this.observers)
                //    obs.OnNext(RobotEvtArgs.OnPageResponse(nextUrl, responce));

                Console.WriteLine($"{++counter}. Качаем {nextUrl}");

                HtmlDocument doc;
                bool htmlReceived = TryGetHtml(responce, out doc);

                foreach (IObserver<RobotEvtArgs> obs in this.observers)
                        obs.OnNext(RobotEvtArgs.OnPageResponse(nextUrl, responce, doc));

                if (!htmlReceived) continue; // Если страница не получена - переходим к следующей

                //Console.WriteLine($"{++counter}. Качаем {nextUrl}");
                //HtmlDocument doc = GetHtmlFrom(nextUrl);                                   

                //this.OnPageResponse?.Invoke(
                //    this,
                //    RobotEvtArgs.OnPageResponse(nextUrl, responce, doc)
                //    );
                
                Console.WriteLine($"Осталось: {UrlQueue.Count}");


                List<TypedUrl> foundUrls;
                if (!this.TryParseUrlsAt(doc, out foundUrls))
                {
                    //this.OnErrorGettingUrls?.Invoke(this, RobotEvtArgs.ErrorGettingUrls(nextUrl);
                    continue; // Если ни одного Url не найдено - переходим к следующей странице
                }
                else
                    //this.OnUrlsOnPageFound?.Invoke(
                    //    this, 
                    //    RobotEvtArgs.UrlsOnPageFound(
                    //        url: nextUrl, 
                    //        foundUrls: foundUrls.ToArray()));
                    foreach (IObserver<RobotEvtArgs> obs in observers)
                        obs.OnNext(
                            RobotEvtArgs.UrlsOnPageFound(
                                url: nextUrl,
                                foundUrls: foundUrls.ToArray()));

                // Старый LinkFoundHandler linkHandler = (obj, args) => { ... }
                foreach (TypedUrl foundUri in foundUrls)
                {
                    if (foundUri.UrlType == LinkType.RelativeToBase)
                        if (!spottedUrls.Contains(foundUri.Url)) //&& !queue.Any(uri => uri.Url == foundUri.Url))
                        {
                            UrlQueue.Enqueue(foundUri.Url);
                            AddSpottedUrl(foundUri.Url);
                            //Console.WriteLine($"{UrlQueue.Count}. {foundUri.Url}");
                        }
                }
            }

            // Метод, вызываемый в конце обхода страниц
            foreach (IObserver<RobotEvtArgs> sub in observers)
                sub.OnCompleted();
            

            this.observers.Clear();
            this.spottedUrls.Clear();
            //this.OnFinish?.Invoke(this, RobotEvtArgs.Finished());

            this.IsBusy = false; // Освобождаем робота
        }

        public bool TryParseUrlsAt(HtmlDocument doc, out List<TypedUrl> foundUrls)
        {
            /*if (doc == null)
            {
                foundUrls = null;
                return false;
            }*/

            HtmlNodeCollection hrefColl = doc.DocumentNode.SelectNodes("//a[@href]");

            if (hrefColl == null)
            {
                foundUrls = null;
                return false;
            }

            foundUrls = new List<TypedUrl>();

            foreach (HtmlNode node in hrefColl)
            {
                string href = node.Attributes["href"].Value;

                // Пытаемся сначала создать абсолютную ссылку
                Uri createdAbsoluteUri;
                bool successAbsolute = Uri.TryCreate(href, UriKind.Absolute, out createdAbsoluteUri);

                if (successAbsolute)
                {
                    // Проверяем, что это файл
                    if (!createdAbsoluteUri.ToString().Contains(" ") && // TODO: stop-symbols
                        (createdAbsoluteUri.IsFile || Path.GetExtension(createdAbsoluteUri.ToString()) != string.Empty))
                    {
                        // Обработка файла, использующего схему file://
                        foundUrls.Add(new TypedUrl(createdAbsoluteUri, LinkType.File));
                    }
                    else
                    {
                        // Обработка найденных абсолютных внешних ссылок
                        foundUrls.Add(new TypedUrl(createdAbsoluteUri, LinkType.Absolute));
                        continue; // Больше нечего делать
                    }
                }


                // Пытаемся создать относительную ссылку
                Uri createdRelativeUri;
                bool successRelative = Uri.TryCreate(href, UriKind.Relative, out createdRelativeUri);

                // Если создана нормальная относительная ссылка
                if (successRelative)
                {
                    Uri createdAbsoluteFromRelative;
                    // Пытаемся создать новую ссылку с которой будем считывать последующие
                    if (Uri.TryCreate(this.BaseUrl, createdRelativeUri, out createdAbsoluteFromRelative))
                    {
                        // Возможно мы получили другую ссылку на файл
                        // Анализириуем абсолютную ссылку во избежание ошибок с пробелами
                        if (Path.GetExtension(createdAbsoluteFromRelative.AbsoluteUri.ToString()) != string.Empty)
                        {
                            foundUrls.Add(new TypedUrl(createdAbsoluteFromRelative, LinkType.File));
                            // Обработка файла, не использующего схему file://
                        }
                        else
                        {
                            // Иначе мы нашли очередную ссылку для сайтмапа
                            foundUrls.Add(new TypedUrl(createdAbsoluteFromRelative, LinkType.RelativeToBase));
                        }
                    }
                    else throw new Exception($"Невозможно создать URI из {this.BaseUrl} и {createdRelativeUri}");
                }
            }

            return true;
        }

        [Obsolete("Используйте GetResponse", true)]
        public HtmlDocument GetHtmlFrom(Uri url)
        {
            HtmlDocument doc = web.Load(url.AbsoluteUri);
            return doc;               
        }

        public HttpWebResponse GetResponse(Uri url)
        {
            try
            {
                // Create a web request for an invalid site. Substitute the "invalid site" strong in the Create call with a invalid name.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.ProtocolVersion = HttpVersion.Version10;
                return (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (WebException e)
            {
                return (HttpWebResponse) e.Response;
            }
        }

        bool TryGetHtml(HttpWebResponse response, out HtmlDocument doc)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string html = reader.ReadToEnd();
                doc = new HtmlDocument();
                doc.LoadHtml(html);
                return true;
            }
            else
            {
                doc = null;
                return false;
            }
        }

        [Obsolete("Не эффективно по памяти", false)]
        public Dictionary<Uri, HtmlDocument> GetAllPages()
        {
            if (this.IsBusy) throw new RobotException($"Робот уже занят обходом ресурса {this.BaseUrl}");

            Dictionary<Uri, HtmlDocument> siteMap = new Dictionary<Uri, HtmlDocument>();

            // Функция, которая будет вызываться при скачивании очередной страницы
            EventHandler<RobotEvtArgs> evtHandler = (obj, args) => siteMap.Add(args.Url, args.Document);
            // Добавляем эту функцию к событию
            this.OnPageResponse += evtHandler;
            // Метод для очистки событий
            EventHandler<RobotEvtArgs> cleaner = null;

            // При окончании работы робота удаляем функции из событий
            cleaner = (_, __) =>
            {
                this.OnPageResponse -= evtHandler;
                this.OnFinish -= cleaner;
            };

            // Вешаем "чистильщика" на событие
            this.OnFinish += cleaner;
                            
            this.Run();
            while (this.IsBusy) Thread.Sleep(1000);

            return siteMap;
        }

        public IDisposable Subscribe(IObserver<RobotEvtArgs> observer)
        {
            this.observers.Add(observer);

            return new Unsubscriber<RobotEvtArgs>(this.observers, observer);
        }        

        public class RobotEvtArgs : EventArgs
        {
            public enum RobotEvtType { PageResponse, PageDownloaded, UrlsOnPageFound, Finish }//, UrlDequeued, ErrorGettingUrls }

            public RobotEvtType EvtType { get; private set; }
            public Uri Url { get; private set; } = null;
            //public LinkType Type { get; private set; } = LinkType.Undetermined;
            public HttpWebResponse Response { get; private set; } = null;
            public TypedUrl[] UrlsOnPage { get; private set; } = null;
            public HtmlDocument Document { get; private set; } = null;
            
            public static RobotEvtArgs UrlsOnPageFound(Uri url, TypedUrl[] foundUrls)
            {
                RobotEvtArgs args = new Robot.RobotEvtArgs();

                args.EvtType = RobotEvtType.UrlsOnPageFound;
                args.Url = url;
                args.UrlsOnPage = foundUrls;

                return args;
            }

            public static RobotEvtArgs OnPageResponse(Uri foundUri, HttpWebResponse response, HtmlDocument doc)
            {
                RobotEvtArgs args = new Robot.RobotEvtArgs();

                args.EvtType = RobotEvtType.PageResponse;
                args.Url = foundUri;
                args.Response = response;
                args.Document = doc;

                return args;
            }

            /*public static RobotEvtArgs OnPageDownloaded(Uri url, HtmlDocument doc, HttpWebResponse response)
            {
                RobotEvtArgs args = new Robot.RobotEvtArgs();

                args.EvtType = RobotEvtType.PageDownloaded;
                args.Url = url;
                args.Document = doc;
                args.Response = response;

                return args;
            }*/

            public static RobotEvtArgs Finished()
            {
                RobotEvtArgs args = new Robot.RobotEvtArgs();

                args.EvtType = RobotEvtType.Finish;

                return args;
            }

            /*public static RobotEvtArgs UrlDequeued(Uri dequeuedUrl, LinkType type)
            {
                RobotEvtArgs args = new Robot.RobotEvtArgs();

                args.EvtType = RobotEvtType.UrlDequeued;
                args.Url = dequeuedUrl;
                args.Type = type;

                return args;
            }

            public static RobotEvtArgs ErrorGettingUrls(Uri url)
            {
                RobotEvtArgs args = new Robot.RobotEvtArgs();

                args.EvtType = RobotEvtType.ErrorGettingUrls;
                args.Url = url;
                args.Type = LinkType.Undetermined; // TODO: File?

                return args;
            }*/
        }

        class RobotException : Exception
        {
            string msg;

            public RobotException(string msg)
            {
                this.msg = msg;
            }

            public override string Message
            {
                get
                {
                    return msg;
                }
            }
        }
        
        internal class Unsubscriber<RobotEvtArgs> : IDisposable
        {
            private List<IObserver<RobotEvtArgs>> observers;
            private IObserver<RobotEvtArgs> observer;

            internal Unsubscriber(List<IObserver<RobotEvtArgs>> observers, IObserver<RobotEvtArgs> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (observers.Contains(observer))
                    observers.Remove(observer);
            }
        }
    }
}
