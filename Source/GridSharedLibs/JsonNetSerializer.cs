#region

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ServiceStack.DesignPatterns.Serialization;

#endregion

namespace GridSharedLibs
{
    public class JsonNetSerializer : ITextSerializer
    {
        #region Implementation of ITextSerializer

        public object DeserializeFromString(string json, Type returnType)
        {
            return JsonConvert.DeserializeObject(json, returnType);
        }

        public T DeserializeFromString<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, new IsoDateTimeConverter());
        }

        public object DeserializeFromStream(Type type, Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return DeserializeFromString(sr.ReadToEnd(), type);
            }
        }

        public string SerializeToString<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                SerializeToStream(obj, ms);
                return new UTF8Encoding(true).GetString(ms.GetBuffer(), 0, (int) ms.Position);
            }
        }

        public T DeserializeFromStream<T>(Stream stream)
        {
            return (T) DeserializeFromStream(typeof (T), stream);
        }

        public void SerializeToStream<T>(T obj, Stream stream)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented, new IsoDateTimeConverter());
            byte[] bytes = new UTF8Encoding().GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
        }

        #endregion
    }
}