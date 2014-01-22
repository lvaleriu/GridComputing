using System;
using System.Configuration;

namespace GridComputing.Configuration
{
    /// <summary>
    /// A collection for <see cref="TaskElement"/>s.
    /// </summary>
    internal class TaskElementCollection : ConfigurationElementCollection
    {
        public TaskElement this[int index]
        {
            get { return (TaskElement) BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public new TaskElement this[string name]
        {
            get { return (TaskElement) BaseGet(name.ToLower()); }
            set
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }
                string nameLower = name.ToLower();
                if (BaseGet(nameLower) != null)
                {
                    BaseRemove(nameLower);
                }
                BaseAdd(value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new TaskElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TaskElement) element).Name;
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName == GridConfigSection.TasksElementName;
        }
    }
}