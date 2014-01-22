using System;

namespace GridAgent.Concurrency
{
	/// <summary>
	/// Provides the scope for a locking operation,
	/// controlled by a <see cref="GridMonitor"/>.
	/// </summary>
	public class GridSync
	{
		/// <summary>
		/// Gets or sets the unique name within the <see cref="ScopeTypeName"/>.
		/// This is used in conjunction with the <see cref="ScopeTypeName"/>
		/// to form a unique identifier used by the <see cref="GridMonitor"/>.
		/// </summary>
		/// <value>The name of the GridSync; 
		/// unique within the context of the ScopeTypeName.</value>
		public string LocalName { get; protected set; }

		/// <summary>
		/// Gets or sets the <code>Type</code> name 
		/// for scoping the <see cref="LocalName"/>.
		/// This is used in conjunction with the <see cref="LocalName"/>
		/// to form a unique identifier used by the <see cref="GridMonitor"/>.
		/// </summary>
		/// <value>The context <code>Type</code> name of the GridSync.</value>
		public string ScopeTypeName { get; protected set; }

		/// <summary>
		/// Gets or sets the client id.
		/// </summary>
		/// <value>The client id.</value>
		public Guid ClientId { get; protected set; }
		
		internal GridSync(Guid clientId, Type localType, string name)
		{
			if (localType == null)
			{
				throw new ArgumentNullException("localType");
			}

			if (name == null)
			{
				throw new ArgumentNullException("name");
			}

			ClientId = clientId;
			ScopeTypeName = localType.AssemblyQualifiedName;
			LocalName = name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GridSync"/> class.
		/// </summary>
		/// <param name="scopeTypeName">Name of the scope type. 
		/// <seealso cref="ScopeTypeName"/></param>
		/// <param name="localName">The local name within the context
		/// of the scopeTypeName. <seealso cref="ScopeTypeName"/></param>
		public GridSync(Type scopeTypeName, string localName, TaskRunner taskRunner)
		{
			if (scopeTypeName == null)
			{
				throw new ArgumentNullException("scopeTypeName");
			}

			if (localName == null)
			{
				throw new ArgumentNullException("localName");
			}

			ClientId = taskRunner.ClientId;
			ScopeTypeName = scopeTypeName.AssemblyQualifiedName;
			LocalName = localName;
		}
	}
}
