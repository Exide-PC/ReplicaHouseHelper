using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ConsoleApplication1.site.DataStructs;

namespace ConsoleApplication1.site.inheritance
{
    sealed class ReplicaHouse : InsalesShop
    {
        public ReplicaHouse() : base("https://replicahouse.ru") { }

        /*protected override PageTemplate GetPageTemplate(HtmlDocument doc)
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
        }*/

        protected override Product ParseProduct(HtmlDocument productDoc)
        {
            if (this.URLs.Length == 0)
                throw new Exception("Не сохранено ни одной страницы");
            
            Product product = new Product();                        
            
            // Заранее получим все часто используемые узлы
            var divNodes = productDoc.DocumentNode.Descendants("div");
            var pNodes = productDoc.DocumentNode.Descendants("p");

            // Title
            string title = productDoc.DocumentNode.Descendants("h1").Where(
                                h => h.Attributes["itemprop"]?.Value == "name")
                                .First().InnerText;
            product.Title = title;

            // Артикул
            string vendorCode = pNodes.Where(
                p => p.Attributes["class"]?.Value == "product-sku").First().LastChild.InnerText;
            product.VendorCode = vendorCode;

            // Описание
            HtmlNode descriptionNode = productDoc.GetElementbyId("tab-description-content");
            if (descriptionNode != null) // Если описание задано
            {
                product.FullDescription = descriptionNode.InnerText.Replace("\t", "");
                //product.FullDescriptionHtml = descriptionNode.InnerHtml;
            }                

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
                    string propValue = propertyNode.InnerText.Replace(propName, "").Replace(":","").Trim();

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
