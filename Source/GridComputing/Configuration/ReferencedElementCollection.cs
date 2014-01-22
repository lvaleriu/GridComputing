using System;
using System.Configuration;

namespace GridComputing.Configuration
{
    /// <summary>
    /// A collection for <see cref="ReferencedElement"/>s.
    /// </summary>
    public class ReferencedElementCollection : ConfigurationElementCollection
    {

        protected override ConfigurationElement CreateNewElement()
        {
            return new ReferencedElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ReferencedElement)element).Name;
        }

        public ReferencedElement this[int index]
        {
            get
            {
                return (ReferencedElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public new ReferencedElement this[string name]
        {
            get
            {
                return (ReferencedElement)BaseGet(name.ToLower());
            }
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

        protected override bool IsElementName(string elementName)
        {
            return elementName == GridConfigSection.TasksElementName;
        }

        public void Add(ReferencedElement referencedElement)
        {
            this[referencedElement.Name] = referencedElement;
        }
    }
}