#region

using System;
using System.Collections.Generic;
using System.Reflection;
using GridAgentSharedLib.Clients;

#endregion

namespace GridSharedLibs.ClientServices
{
    internal static class GridServiceKnownTypeCollector
    {
        public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
        {
            /* Add any types to include here. */
            var knownTypes = new List<Type> {typeof (Agent)};
            return knownTypes;
        }
    }
}