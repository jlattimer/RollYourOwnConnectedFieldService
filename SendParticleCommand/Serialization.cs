using System.IO;
using System.Runtime.Serialization.Json;

namespace SendParticleCommand
{
    public class Serialization
    {
        public static string SerializeObject<T>(object o)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, o);
                stream.Position = 0;
                StreamReader reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                return json;
            }
        }

        public static T DeserializeObject<T>(string json)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(json);
                writer.Flush();
                stream.Position = 0;
                T o = (T)serializer.ReadObject(stream);

                return o;
            }
        }
    }
}