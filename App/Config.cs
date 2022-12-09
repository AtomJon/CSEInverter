using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace CSEInverter
{
    [XmlRoot, XmlInclude(typeof(ProductTaskConfiguration))]
    public class Config
    {
        static readonly XmlSerializer serializer = new XmlSerializer(typeof(Config));

        [XmlAttribute]
        public string Name { get; set; }

        [XmlArray("Tasks"), XmlArrayItem("Task")]
        public List<ProductTaskConfiguration> ProductTasks { get; set; }

        [XmlIgnore]
        public Action TaskAdded;

        public Config()
        {
            ProductTasks = new();

            TaskAdded = new(OnTaskAdded);
        }

        public Config(string name)
        {
            ProductTasks = new();

            TaskAdded = new(OnTaskAdded);

            Name = name;
        }

        private void OnTaskAdded() { }

        public IProductTask[] GetTasks()
        {
            return ProductTasks.Select((task) => { return task.Task; }).ToArray();
        }

        public static Config LoadConfigFromStream(Stream configStream)
        {
            if (configStream == null) throw new ArgumentNullException("ConfigStream is null");
            if (configStream.CanRead == false) throw new ArgumentException("ConfigStream.CanRead is false");
            if (configStream.Length < 1) throw new ArgumentException("ConfigStream.Length is less than 1");

            object config = serializer.Deserialize(configStream);

            if (config == null)
            {
                throw new ArgumentException($"Config file cannot be loaded, stream: {configStream}");
            }

            return (Config)config;
        }

        public void SaveToStream(Stream destStream)
        {
            if (destStream == null) throw new ArgumentNullException("ConfigStream is null");
            if (destStream.CanWrite == false) throw new ArgumentException("ConfigStream.CanWrite is false");

            serializer.Serialize(destStream, this);
        }

        public void RearrangeTasks(ProductTaskConfiguration removed, ProductTaskConfiguration target)
        {
            int removedIdx = ProductTasks.IndexOf(removed);
            int targetIdx = ProductTasks.IndexOf(target);

            if (removedIdx < targetIdx)
            {
                ProductTasks.Insert(targetIdx + 1, removed);
                ProductTasks.RemoveAt(removedIdx);
            }
            else
            {
                int remIdx = removedIdx + 1;
                if (ProductTasks.Count + 1 > remIdx)
                {
                    ProductTasks.Insert(targetIdx, removed);
                    ProductTasks.RemoveAt(remIdx);
                }
            }
        }

        public void AddTask(IProductTask task)
        {
            ProductTasks.Add(new ProductTaskConfiguration(task));

            TaskAdded();
        }

        internal void RemoveTask(IProductTask task)
        {
            ProductTaskConfiguration item = FindTaskInProductTasks(task);

            ProductTasks.Remove(item);
        }

        private ProductTaskConfiguration FindTaskInProductTasks(IProductTask task)
        {
            return ProductTasks.Find((x) => x.Task == task);
        }
    }
}