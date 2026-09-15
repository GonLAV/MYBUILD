using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bolt.Automation.Common.Utils
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerSettings _serializerSettings = new()
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static TValue? Deserialize<TValue>(string json, JsonSerializerSettings? serializerSettings = null)
        {
            return JsonConvert.DeserializeObject<TValue>(json, serializerSettings ?? _serializerSettings);
        }

        public static object? Deserialize(string json, Type returnType, JsonSerializerSettings? serializerSettings = null)
        {
            return JsonConvert.DeserializeObject(json, returnType, serializerSettings ?? _serializerSettings);
        }

        public static string Serialize<TValue>(TValue value, JsonSerializerSettings? serializerSettings = null)
        {
            return JsonConvert.SerializeObject(value, serializerSettings ?? _serializerSettings);
        }

        public static TValue? Clone<TValue>(TValue value)
        {
            return Deserialize<TValue>(Serialize(value));
        }

        private static void FlattenObjectInternal(JToken obj, Dictionary<string, object?> dict, string? parent, bool supportArrayKeyType)
        {
            foreach (var prop in obj.Cast<JProperty>())
            {
                var propKey = parent == null ? prop.Name : $"{parent}.{prop.Name}";

                if (prop.Value is JObject)
                {
                    FlattenObjectInternal(prop.Value, dict, propKey, supportArrayKeyType);
                }
                else if (prop.Value is JArray)
                {
                    if (prop.Value is JArray { Count: > 0 } jarr)
                    {
                        if (jarr[0] is JValue) // Primitive Array
                        {
                            var arr = jarr.Select(j => j.Value<string>()).ToArray();
                            //dict.Add(string.Format("{0}[]", propKey), arr);
                            dict.Add(supportArrayKeyType ? $"{propKey}[]" : propKey, arr);
                        }
                        else if (jarr[0] is JObject) // Complex Array
                        {
                            string uid = "9480f348-9447-41ea-9b44-16aad46b4f1{0}";
                            for (var i = 0; i < jarr.Count; i++)
                            {
                                var id = ((JObject)jarr[i]).GetValue("__id__");
                                // temp solution!!
                                //id = new JValue(Guid.NewGuid().ToString());
                                id ??= string.Format(uid, i + 6);

                                var arrKey = $"{propKey}[?(@__id__=='{id}')]";
                                FlattenObjectInternal(jarr[i], dict, arrKey, supportArrayKeyType);
                            }
                        }
                    }
                    else // Empty Array
                    {
                        dict.Add(propKey, null);
                    }
                }
                else
                {
                    var jval = prop.Value as JValue;
                    dict.Add(propKey, jval?.Value);
                }
            }
        }

        public static Dictionary<string, object?> FlattenObject(JObject obj, bool supportArrayKeyType = false)
        {
            var dict = new Dictionary<string, object?>();
            var isEmptyRoot = obj.First == obj.Last && obj.First is JProperty & (obj.First as JProperty).Value.Equals(new JValue(""));

            if (!isEmptyRoot)
            {
                FlattenObjectInternal(obj, dict, null, supportArrayKeyType);
            }

            return dict;
        }

        public static JObject? UnflattenObj(Dictionary<string, object> dict)
        {
            var jobj = new JObject();

            UnflattenObjInternal(jobj, dict, null);

            return jobj;
        }

        private static void UnflattenObjInternal(JObject? jobj, Dictionary<string, object> dict, string? prefix)
        {
            foreach (var kvp in dict)
            {
                string newKey = string.IsNullOrEmpty(prefix) ? kvp.Key : kvp.Key.Replace(prefix + ".", string.Empty);

                var split = newKey.Split('.'); // PD.Drivers[id=23423].DriverType[]
                JToken? curr = jobj;

                for (var i = 0; i < split.Length - 1; i++)
                {
                    var arrayStartIdx = split[i].IndexOf('[');

                    if (arrayStartIdx > -1)
                    {
                        // Complex Array
                        var propName = split[i][..arrayStartIdx];
                        var id = Guid.Parse(Regex.Match(split[i], "[a-fA-F0-9]{8}-([a-fA-F0-9]{4}-){3}[a-fA-F0-9]{12}").Value);


                        if (curr?[propName] is not JArray jarr)
                        {
                            jarr = [];
                            (curr as JObject)?.Add(propName, jarr);
                        }

                        var val = new JValue(id);
                        curr = jarr.Cast<JObject>()?.FirstOrDefault(o => o.GetValue("__id__").Equals(val));

                        if (curr == null)
                        {
                            curr = new JObject();
                            (curr as JObject)?.Add("__id__", val);
                            jarr?.Add(curr);
                        }
                    }
                    else
                    {
                        if (curr?[split[i]] == null)
                        {
                            (curr as JObject)?.Add(split[i], new JObject());
                        }

                        curr = curr[split[i]];
                    }

                    newKey = newKey.Replace(split[i] + ".", string.Empty);
                }

                var currObject = curr as JObject;

                var primitiveArrStartIdx = newKey.IndexOf("[]", StringComparison.Ordinal);
                if (primitiveArrStartIdx > -1) // Primitive Array
                {
                    var propName = newKey[..primitiveArrStartIdx];
                    currObject?.Add(propName, JArray.FromObject(kvp.Value));
                    //var strValue = kvp.Value as string;
                    //currObject.Add(propName, new JArray(strValue.Split(',')));
                }
                else // regular property
                {
                    var currVal = currObject?.GetValue(newKey);

                    if (currVal != null) continue;
                    {
                        var realVal = Convert.ChangeType(kvp.Value, kvp.Value.GetType());
                        JToken item = new JValue(realVal);
                        (curr as JObject)?.Add(newKey, item);
                    }
                }
            }
        }
    }
}
