using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace ConsoleApplication1.site.DataStructs
{
    public class RobotSession: IXmlSerializable
    {
        public bool IsFinished { get; set; } = false;
        public List<Page> FoundPages { get; set; } = new List<Page>();
        public Queue<Uri> NotDequeuedUrls { get; set; } = new Queue<Uri>();

        static XmlSerializer pageSerializer = new XmlSerializer(typeof(Page));

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();

            // IsFinished
            reader.MoveToFirstAttribute();
            this.IsFinished = bool.Parse(reader.Value);

            // FoundUrls
            reader.MoveToElement();
            reader.ReadStartElement();

            if (!reader.IsEmptyElement)
            {
                reader.ReadStartElement(); // <FoundPages>
                
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    Page foundPage = (Page)pageSerializer.Deserialize(reader);
                    this.FoundPages.Add(foundPage);

                    /*reader.MoveToContent();
                    reader.MoveToNextAttribute(); // AbsoluteUrl
                    Uri foundUrl = new Uri(reader.Value, UriKind.Absolute);                    

                    reader.MoveToNextAttribute(); // AbsoluteUrl
                    System.Net.HttpStatusCode response = (System.Net.HttpStatusCode) (int.Parse(reader.Value));

                    Page urlResponse = new Page() {
                        Url = foundUrl,
                        StatusCode = response
                    };
                    this.FoundPages.Add(urlResponse);

                    reader.MoveToElement();  // <Url ...
                    reader.Read(); // Переходим к следующему узлу*/
                }
                reader.ReadEndElement(); // </FoundUrls>
            }
            else
                reader.Read(); // съедаем <FoundPages />

            // NotDequeuedUrls

            if (!reader.IsEmptyElement)
            {
                reader.ReadStartElement(); 

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.MoveToContent();
                    reader.MoveToNextAttribute(); // AbsoluteUrl

                    Uri foundUrl = new Uri(reader.Value, UriKind.Absolute);
                    this.NotDequeuedUrls.Enqueue(foundUrl);

                    reader.MoveToElement(); // <Url ...
                    reader.Read(); // Переходим к следующему узлу
                }
                reader.ReadEndElement(); // </NotDequeuedUrls>
            }
            else
                reader.Read(); // съедаем <NotDequeuedUrls />

            reader.ReadEndElement(); // съедаем </RobotSession>
            
        }

        public void WriteXml(XmlWriter writer)
        {
            // IsFinished
            writer.WriteAttributeString("IsFinished", this.IsFinished.ToString());

            // FoundPages
            writer.WriteStartElement("FoundPages");
            foreach (Page foundUrl in this.FoundPages)
            {
                pageSerializer.Serialize(writer, foundUrl);
                /*writer.WriteStartElement("UrlResponse");
                writer.WriteAttributeString("Url", foundUrl.Url.AbsoluteUri);
                writer.WriteAttributeString("StatusCode", ((int)foundUrl.StatusCode).ToString());
                writer.WriteEndElement();*/
            }
            writer.WriteEndElement();

            // NotDequeuedUrls
            writer.WriteStartElement("NotDequeuedUrls");
            foreach (Uri foundUrl in this.NotDequeuedUrls)
            {
                writer.WriteStartElement("Url");
                writer.WriteAttributeString("AbsoluteUrl", foundUrl.AbsoluteUri);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }            
    }
}
