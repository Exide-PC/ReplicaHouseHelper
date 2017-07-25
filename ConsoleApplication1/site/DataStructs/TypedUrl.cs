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
    public class TypedUrl: IXmlSerializable
    {
        public Uri Url { get; set; }
        public Robot.LinkType UrlType { get; set; }

        public TypedUrl() { }

        public TypedUrl(Uri foundUri, Robot.LinkType type)
        {
            this.Url = foundUri;
            this.UrlType = type;
        }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToFirstAttribute();
            this.Url = new Uri(reader.Value, UriKind.Absolute);

            reader.MoveToNextAttribute();
            this.UrlType = (Robot.LinkType) Enum.Parse(typeof(Robot.LinkType), reader.Value);

            //reader.MoveToElement();

            reader.Read();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("AbsoluteUrl", this.Url.AbsoluteUri);
            writer.WriteAttributeString("LinkType", this.UrlType.ToString());
        }
    }
}
