using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ConsoleApplication1.site
{
    sealed class ReplicaHouse : InsalesShop
    {
        public ReplicaHouse() : base("https://replicahouse.ru") { }

        protected override PageTemplate GetPageTemplate(HtmlDocument doc)
        {            
            var body = doc.DocumentNode.Descendants("body").First();
            string template = body.Attributes["class"].Value;

            switch (template)
            {
                case "template-product": return PageTemplate.Product;
                case "template-collection": return PageTemplate.Collection;
                case "template-page_404": return PageTemplate.Error404;
                default: return PageTemplate.Undetermined;
            }            
        }

        protected override Product ParseProduct(Uri url)
        {
            if (this.URLs.Length == 0)
                throw new Exception("Не сохранено ни одной страницы");

            HtmlDocument doc = this[url];
            Product product = new Product();

            // Задаем Url продукта в любом случае
            product.Url = url.AbsoluteUri;

            // Проверка шаблона на 404 и соответствие шаблону продукта
            PageTemplate template = GetPageTemplate(doc);
            if (template == PageTemplate.Error404)
            {
                product.IsValid = false;
                return product;
            }
            else if (template == PageTemplate.Product)
                product.IsValid = true;
            else
                throw new Exception("Документ не имеет шаблон продукта");

            //
            // Начинаем обработку страницы товара, если страница корректна
            //

            // Заранее зададим нужные узлы
            var divNodes = doc.DocumentNode.Descendants("div");
            var pNodes = doc.DocumentNode.Descendants("p");

            // Title
            string title = doc.DocumentNode.Descendants("h1").Where(
                                h => h.Attributes["itemprop"]?.Value == "name")
                                .First().InnerText;
            product.Title = title;

            // Артикул
            string vendorCode = pNodes.Where(
                p => p.Attributes["class"]?.Value == "product-sku").First().LastChild.InnerText;
            product.VendorCode = vendorCode;

            // Описание
            HtmlNode descriptionNode = doc.GetElementbyId("tab-description-content");
            if (descriptionNode != null) // Если описание задано
                product.FullDescription = descriptionNode.InnerText.Replace("\t", "");

            // Свойства
            var foundPropNodes = divNodes.Where(
                 d => d.Attributes["class"]?.Value == "product-properties");

            if (foundPropNodes.Count() > 0) // Если есть свойства
            {
                var productProperties = foundPropNodes.First().Descendants("p").Where(
                    p => p.FirstChild.Attributes["class"]?.Value != "js-product-property-more product-property-more");

                foreach (HtmlNode propertyNode in productProperties)
                {


                    string innerText = propertyNode.InnerText;
                    string propName = propertyNode.FirstChild.InnerText.Replace(":", "");
                    string propValue = propertyNode.InnerText.Replace(propName, "").Trim();

                    product.Properties.Add(propName, propValue);
                }
            }

            // Короткое описание
            var shortDescNodes = pNodes.Where(
                                p => p.Attributes["class"]?.Value == "product-short-description" &&
                                p.Attributes["itemprop"]?.Value == "description");
            if (shortDescNodes.Count() > 0) // Если есть короткое описание
                product.ShortDescription = shortDescNodes.First().InnerText;

            // Цена и валюта
            var priceDataNodes = divNodes.Where(
                d => d.Attributes["itemprop"]?.Value == "offers").First().Descendants("meta");

            string priceCurrency = priceDataNodes.Where(n => n.Attributes["itemprop"]?.Value == "priceCurrency")
                .Select(n => n.Attributes["content"]).First().Value;
            product.PriceCurrency = priceCurrency;

            string priceStr = priceDataNodes.Where(n => n.Attributes["itemprop"]?.Value == "price")
                .Select(n => n.Attributes["content"]).First().Value;
            product.Price = float.Parse(priceStr, System.Globalization.CultureInfo.InvariantCulture);

            return product;
        }
    }
}
