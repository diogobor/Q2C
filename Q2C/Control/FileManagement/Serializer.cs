using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace Q2C.Control.FileManagement
{
    public static class Serializer
    {
        private class MyContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select(p => base.CreateProperty(p, memberSerialization))
                            .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                       .Select(f => base.CreateProperty(f, memberSerialization)))
                            .ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });
                return props;
            }

        }

        public static string ToJSON(object o, bool serializeFields = true, bool indented = true)
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Formatting = indented ? Formatting.Indented : Formatting.None
            };

            if (serializeFields == true)
                jsonSettings.ContractResolver = new MyContractResolver();

            string r = JsonConvert.SerializeObject(o, jsonSettings);

            return r;
        }

        public static T FromJson<T>(string jsonString, bool serializeFields = true)
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Formatting = Formatting.Indented,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };

            if (serializeFields == true)
                jsonSettings.ContractResolver = new MyContractResolver();

            return JsonConvert.DeserializeObject<T>(jsonString, jsonSettings);
        }

        public static List<T> FromJson<T>(string jsonPath, int batchSize)
        {
            List<T> r = new List<T>();

            using (StreamReader streamReader = new StreamReader(jsonPath))
            using (JsonTextReader reader = new JsonTextReader(streamReader))
            {
                int ctt = 0;
                reader.SupportMultipleContent = true;

                var serializer = new JsonSerializer();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        T c = serializer.Deserialize<T>(reader);
                        ctt++;
                        r.Add(c);
                        if (ctt == batchSize)
                            break;
                    }
                }
            }

            return r;
        }

        /// <summary>
        /// Method responsible for serializing xreas
        /// </summary>
        /// <param name="xreas">"42.681_0.71#42.741_0.5#42.761_0.58" RT vs Xrea</param>
        /// <returns>base64string</returns>
        public static string SerializeXreas(string xreas)
        {
            var strToJson = ToJSON(xreas, false);
            byte[] byte_array = Encoding.UTF8.GetBytes(strToJson);
            string stringBase64 = Convert.ToBase64String(byte_array);
            return stringBase64;
        }
        /// <summary>
        /// Method responsible for deserializing xreas
        /// </summary>
        /// <param name="xreasSerialized">base64string</param>
        /// <returns>"42.681_0.71#42.741_0.5#42.761_0.58" RT vs Xrea</returns>
        public static string DeserializeXreas(string xreasSerialized)
        {
            string xreas = xreasSerialized;
            try
            {
                byte[] byte_array = Convert.FromBase64String(xreasSerialized);
                string byte_array_str = Encoding.UTF8.GetString(byte_array);
                xreas = FromJson<string>(byte_array_str);
            }
            catch (Exception) { }

            return xreas;
        }
    }
}
