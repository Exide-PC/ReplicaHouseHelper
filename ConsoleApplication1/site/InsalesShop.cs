using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ConsoleApplication1.site
{
    abstract class InsalesShop: CachedSite
    {
        public enum PageTemplate { Product, Collection, Error404, Undetermined }

        Product[] products = null; // new Product[0];
        public bool HasProducts => this.products != null && this.products.Length > 0;
        public Product[] Products => this.products.ToArray();
        public Uri[] ProductUrls => this.URLs.Where(url => url.PathAndQuery.StartsWith("/product")).ToArray();

        string holderFile; // = "ProductBase.xml";

        public InsalesShop(string url): base(url)
        {
            this.holderFile = $@"{this.OwnDir}\ProductBase.xml";

            if (File.Exists(holderFile))
            {
                var serializer = new XmlSerializer(typeof(Product[]));
                
                using (StreamReader fs = File.OpenText(this.holderFile))
                    this.products = (Product[]) serializer.Deserialize(fs);         
            }  
        }
        
        protected abstract Product ParseProduct(Uri url);
        protected abstract PageTemplate GetPageTemplate(HtmlDocument doc);
        
        public void UpdateProductCache()
        {
            Uri[] productUrls = this.ProductUrls;
            this.products = new Product[productUrls.Length];

            for (int i = 0; i < productUrls.Count(); i++)
            {
                Console.WriteLine(i);

                Uri url = productUrls[i];
                this.products[i] = ParseProduct(url);
            }

            // Сохраняем полученную базу
            var sb = new StringBuilder();
            var serializer = new XmlSerializer(typeof(Product[]));

            using (FileStream fs = File.OpenWrite(this.holderFile))
                serializer.Serialize(fs, this.products);            
        }

        public override void Delete()
        {
            base.Delete();

            this.products = new Product[0];
        }
    }
}
