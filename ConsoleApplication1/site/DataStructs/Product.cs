using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ConsoleApplication1.site.DataStructs
{
    public class Product: IEquatable<Product> //: IXmlSerializable
    {          
        public static readonly string DEFAULT_NO_DATA = "NULL";
        
        public string Url = DEFAULT_NO_DATA;
        public string Title = DEFAULT_NO_DATA;
        public string VendorCode = DEFAULT_NO_DATA;
        public float Price = 0;
        public string PriceCurrency = DEFAULT_NO_DATA;
        public PropertiesDictionary Properties = new PropertiesDictionary();
        public string ShortDescription = DEFAULT_NO_DATA;
        public string FullDescription = DEFAULT_NO_DATA;
        //public string FullDescriptionHtml = DEFAULT_NO_DATA;

        public bool Equals(Product other)
        {
            return this.Url == other.Url;
        }
        
        public class PropertiesDictionary : Dictionary<string, string>, IXmlSerializable
        {
            public XmlSchema GetSchema() => null;

            public void ReadXml(XmlReader reader)
            {
                reader.MoveToContent(); // Пропускаем комментарии и переходим к контенту
                bool wasEmpty = reader.IsEmptyElement; // Нет внутренних узлов, но аттрибуты допустимы
                reader.Read(); // Аналог необходимого в конце ReadEndElement() на случай если у нас пустой узел
                if (wasEmpty) return;

                while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
                {
                    reader.MoveToContent();

                    reader.MoveToFirstAttribute();
                    string key = reader.Value;
                    
                    reader.MoveToNextAttribute();
                    string value = reader.Value;

                    this.Add(key, value);

                    reader.MoveToElement();
                    reader.Read();
                }
                reader.ReadEndElement(); // У нас гарантированно есть закрывающий элемент </Properties>
            }

            public void WriteXml(XmlWriter writer)
            {
                foreach (KeyValuePair<string, string> pair in this)
                {
                    writer.WriteStartElement("Pair");

                    writer.WriteAttributeString("Key", pair.Key);
                    writer.WriteAttributeString("Value", pair.Value);
                    
                    writer.WriteEndElement();
                }
            }
        }
    }
}
