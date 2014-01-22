#region

using System;
using GridComputingSharedLib;

#endregion

namespace PrimeFinder_Master
{
    //[Serializable]
    public class PrimesRange : BaseUserData
    {
        public long LowerLimit { get; set; }
        public long UpperLimit { get; set; }

        public int[] Test { get; set; }
    }
}