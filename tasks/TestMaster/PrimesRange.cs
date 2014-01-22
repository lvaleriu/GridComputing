#region

using System;
using GridComputingSharedLib;

#endregion

namespace TestMaster
{
    //[Serializable]
    public class PrimesRange : BaseUserData
    {
        public long LowerLimit { get; set; }
        public long UpperLimit { get; set; }
    }
}