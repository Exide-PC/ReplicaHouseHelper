using ConsoleApplication1.site.DataStructs;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ConsoleApplication1.site.inheritance
{
    abstract class InsalesShop: CachedSite
    {
        public enum PageTemplate { Product, Collection, Error404, Undetermined }

        Product[] products = new Product[0];
        public bool HasProducts => this.products.Length > 0;
        public Product[] Products => this.products.ToArray();
        public Uri[] ProductUrls => this.URLs.Where(url => url.PathAndQuery.StartsWith("/product")).ToArray();

        string holderFile; // = "ProductBase.xml";

        public InsalesShop(string url): base(url)
        {
            this.holderFile = $@"{this.OwnDir}\ProductBase.xml";

            if (this.IsValidPageCache && File.Exists(holderFile))
            {
                var serializer = new XmlSerializer(typeof(Product[]));
                
                using (StreamReader fs = File.OpenText(this.holderFile))
                    this.products = (Product[]) serializer.Deserialize(fs);         
            }  
        }
        
        protected abstract Product ParseProduct(HtmlDocument productDoc);

        public bool IsProductCacheUpdatePossible => this.IsValidPageCache;

        public void UpdateProductCache()
        {
            if (!this.IsProductCacheUpdatePossible)
                throw new InvalidDataException("Обновление продуктов невозможно по причине невалидности кеша страниц.");

            Uri[] productUrls = this.ProductUrls
                .Where(url => this.StatusCodeByUrl(url) == System.Net.HttpStatusCode.OK)
                .ToArray();

            this.products = new Product[productUrls.Length];

            for (int i = 0; i < productUrls.Count(); i++)
            {
                Console.WriteLine(i);

                Uri url = productUrls[i];
                HtmlDocument productDoc = this[url];

                this.products[i] = ParseProduct(productDoc);
                this.products[i].Url = url.AbsoluteUri; // Не забываем задать URL продукта
            }

            // Сохраняем полученную базу
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
