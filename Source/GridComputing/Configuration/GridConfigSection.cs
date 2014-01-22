using System.Configuration;

namespace GridComputing.Configuration
{
    /// <summary>
    /// The configuration for grid module.
    /// </summary>
    internal class GridConfigSection : ConfigurationSection
    {
        /// <summary>
        /// The name of the Tasks element in the configuration.
        /// <example>&lt;Tasks/&gt;</example>
        /// </summary>
        public const string TasksElementName = "Tasks";

        private const string debugAttributeName = "debug";

        [ConfigurationProperty(debugAttributeName, DefaultValue = false,
            IsRequired = false)]
        public bool Debug
        {
            get { return (bool) base[debugAttributeName]; }
            set { base[debugAttributeName] = value; }
        }

        /// <summary>
        /// Gets the <see cref="TaskElement"/>s that the <see cref="ITask"/>s
        /// are initialised from.
        /// </summary>
        /// <value>The task elements.</value>
        [ConfigurationProperty(TasksElementName)]
        public TaskElementCollection TaskElements
        {
            get { return (TaskElementCollection) base[TasksElementName]; }
        }
    }
}