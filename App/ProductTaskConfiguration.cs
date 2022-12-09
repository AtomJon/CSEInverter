using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CSEInverter
{
    public sealed class ProductTaskConfiguration : IXmlSerializable
    {
        public ProductTaskConfiguration() { }
        public ProductTaskConfiguration(IProductTask t) { this.Task = t; }

        public IProductTask Task { get; set; }
        public string Name { get { return Task.GetDescription(); } }

        public void WriteXml(XmlWriter writer)
        {
            if (Task == null)
            {
                writer.WriteAttributeString("type", "null");
                return;
            }
            Type type = this.Task.GetType();
            XmlSerializer serializer = new XmlSerializer(type);
            writer.WriteAttributeString("type", type.AssemblyQualifiedName);
            serializer.Serialize(writer, this.Task);
        }

        public void ReadXml(XmlReader reader)
        {
            if (!reader.HasAttributes)
                throw new FormatException("expected a type attribute!");
            string type = reader.GetAttribute("type");

            reader.Read(); // consume the value
            if (type == "null")
                return;// leave T at default value
            XmlSerializer serializer = new XmlSerializer(Type.GetType(type));
            this.Task = (IProductTask)serializer.Deserialize(reader);
            reader.ReadEndElement();
        }

        public XmlSchema GetSchema() { return (null); }
    }
}