using System.Configuration;

namespace GridComputing.Configuration
{
    public class ReferencedElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the unique name of the <see cref="TaskElement"/>.
        /// </summary>
        /// <value>The name of the task.</value>
        /// <example>Mylib</example>/// 
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return ((string)base["name"]);
            }
            set
            {
                base["name"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the unique name of the <see cref="TaskElement"/>.
        /// </summary>
        /// <value>The name of the dll.</value>
        /// <example>mylib.dll</example>/// 
        [ConfigurationProperty("dllName", IsRequired = true, IsKey = true)]
        public string DllName
        {
            get
            {
                return ((string)base["dllName"]);
            }
            set
            {
                base["dllName"] = value;
            }
        }

    }
}