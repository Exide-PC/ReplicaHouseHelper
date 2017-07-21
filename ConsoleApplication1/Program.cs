using ConsoleApplication1.site;
using HtmlAgilityPack;
using System;
using System.IO;
using System.Net;

namespace GlobalHotkeyExampleForm
{
    class Program
    {
        public static void Main()
        {
            //Uri url = new Uri("https://www.replicahouse.ru/product/delfin-uzornyi-kulon", UriKind.Absolute);
            //HttpWebResponse response = new Robot("https://replicahouse.ru").GetResponse(url);

            //Robot r = new Robot("https://www.replicahouse.ru/product/delfin-uzornyi-kulon");
            //HtmlDocument doc = r.GetHtmlFrom(new Uri("https://www.replicahouse.ru/product/delfin-uzornyi-kulon", UriKind.Absolute));

            ReplicaHouse rh = new ReplicaHouse();
            rh.UpdatePageCache();

            /*HtmlWeb web = new HtmlWeb();
            Uri url1 = new Uri("https://www.replicahouse.ru/product/delfin-uzornyi-kulon/", UriKind.Absolute);
            Uri url2 = new Uri("https://www.replicahouse.ru/", UriKind.Absolute);

            WebClient wc = new WebClient();
            WebResponse response;
            Stream responseStream;
            try
            {
                var request = WebRequest.Create(url1);
                response = request.GetResponse();
                responseStream = response.GetResponseStream();
                {
                    // Process the stream
                }
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                // handle 404 exceptions
            }
            catch (WebException ex)
            {
                // handle other web exceptions
            }


            try
            {
                // Create a web request for an invalid site. Substitute the "invalid site" strong in the Create call with a invalid name.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url1);

                //myHttpWebRequest.ContentType = "application/soap+xml;";
                //myHttpWebRequest.Method = "POST";
                //myHttpWebRequest.KeepAlive = false;
                myHttpWebRequest.Timeout = System.Threading.Timeout.Infinite;
                //myHttpWebRequest.ReadWriteTimeout = System.Threading.Timeout.Infinite;
                myHttpWebRequest.ProtocolVersion = HttpVersion.Version10;
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                myHttpWebResponse.Close();
            }
            catch (WebException e)
            {
                Console.WriteLine("This program is expected to throw WebException on successful run." +
                                    "\n\nException Message :" + e.Message);
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    Console.WriteLine(e.Response);
                    Console.WriteLine("Status Code : {0}", ((HttpWebResponse)e.Response).StatusCode);
                    Console.WriteLine("Status Description : {0}", ((HttpWebResponse)e.Response).StatusDescription);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }*/

            /*
             * <RobotSession IsFinished="false">
             *      <FoundUrls>
             *          <Url>https://replicahouse.ru</Url>
             *          <Url>https://test.ru</Url>
             *      </FoundUrls>
             *      <NotDequeuedUrls>
             *          <Url>https://replicahouse.ru</Url>
             *          <Url>https://test.ru</Url>
             *      </NotDequeuedUrls>
             * </RobotSession>
             */


            /*InsalesShop shop = new ReplicaHouse();
            //shop.UpdatePageCache();
            //shop.UpdateProducts();

            Product[] products = shop.Products;


            int counter = 0;
            var prods = products.Where(prod => !prod.IsValid);
                                    

            foreach (Product prod in prods)
                Console.WriteLine($"{++counter}. {prod.Url}");*/
        }

    }
}
 