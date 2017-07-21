using ConsoleApplication1.site.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace ConsoleApplication1.site.Utils
{
    public class RobotSession: IXmlSerializable
    {
        public bool IsFinished { get; set; } = false;
        public List<Uri> FoundUrls { get; set; } = new List<Uri>();
        public Queue<Uri> NotDequeuedUrls { get; set; } = new Queue<Uri>();

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
                reader.ReadStartElement(); // <FoundUrls>

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.MoveToContent();
                    reader.MoveToNextAttribute(); // AbsoluteUrl

                    Uri foundUrl = new Uri(reader.Value, UriKind.Absolute);
                    this.FoundUrls.Add(foundUrl);

                    reader.MoveToElement();  // <Url ...
                    reader.Read(); // Переходим к следующему узлу
                }
                reader.ReadEndElement(); // </FoundUrls>
            }
            else
                reader.Read(); // съедаем <FoundUrls />

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

            // FoundUrls
            writer.WriteStartElement("FoundUrls");
            foreach (Uri foundUrl in this.FoundUrls)
            {
                writer.WriteStartElement("Url");
                writer.WriteAttributeString("AbsoluteUrl", foundUrl.AbsoluteUri);
                writer.WriteEndElement();
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
