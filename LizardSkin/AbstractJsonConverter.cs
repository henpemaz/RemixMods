using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

// http://www.tomdupont.net/2014/04/deserialize-abstract-classes-with.html
public abstract class AbstractJsonConverter<T> : JsonConverter
{
    protected abstract T Create(Type objectType, JObject jObject);
    public override bool CanConvert(Type objectType)
    {
        return typeof(T).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        T target = Create(objectType, jObject);
        serializer.Populate(jObject.CreateReader(), target);
        return target;
    }

    public override bool CanWrite { get { return false; } } // not handled
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class UnityColorConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(UnityEngine.Color).IsAssignableFrom(objectType);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return Menu.MenuColorEffect.HexToColor((string)reader.Value);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, Menu.MenuColorEffect.ColorToHex((UnityEngine.Color)value));
    }
}