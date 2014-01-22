#region

using System;

#endregion

namespace GridComputingSharedLib
{
    public class GridLog : IGridLog
    {
        public void Info(string info)
        {
            Console.WriteLine("Info: " + info);
        }

        public void Error(string error, Exception ex)
        {
            Console.WriteLine("Error: " + error + ": " + SerializeException(ex));
        }

        public void InfoFormat(string info, params object[] args)
        {
            Console.WriteLine("Info: " + info, args);
        }

        public void Warn(string warning)
        {
            Console.WriteLine("Warn: " + warning);
        }

        public void WarnFormat(string s, params object[] args)
        {
            Console.WriteLine("Warn: " + s, args);
        }

        public void Warn(string warning, Exception ex)
        {
            Console.WriteLine("Warn: " + warning + ". Error: " + SerializeException(ex));
        }

        public static string SerializeException(Exception ex)
        {
            if (ex == null)
                return null;

            var res = string.Format("\nMessage: {0}\n", ex.Message);

            while (ex.InnerException != null)
            {
                res += string.Format("\nInnerMessage: {0}\n", ex.Message);
                ex = ex.InnerException;
            }

            return res;

            //return Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented);
        }
    }
}