using System;
using Newtonsoft.Json;

namespace STU.SignalsChecker
{
    /// <summary>
    /// this class is used for JsonConvert.Serialize 
    /// </summary>
    class SignalJsonConverter: JsonConverter
    {

        /// <summary>
        /// override the JsonConverter function writejson
        /// merge the property connect and misc of Signal Class, when serialization 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Signal s = value as Signal;
            if(s != null) 
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Name");
                writer.WriteValue(s.Name);
                writer.WritePropertyName("IO");
                writer.WriteValue(s.IO);
                writer.WritePropertyName("InstanceDef");
                writer.WriteValue(s.InstanceDef);
                writer.WritePropertyName("Width");
                writer.WriteValue(s.Width);
                writer.WritePropertyName("Connection");
                writer.WriteValue(s.Connection);
                writer.WriteEnd();
            }
        }


        /// <summary>
        /// overrider the JsonCoverter function ReadJson
        /// not implement the funciton throw a Exception  
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type type, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("This Converter cannot read Json object");
        }

        /// <summary>
        /// overrider the JsonConverter function CanConvert
        /// only Signal object can uses this converter 
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Signal).Equals(objectType);
        }
    }
}