using System.Text.Json;


namespace ClientLibrary.Helpers
{
    public static class Serializations
    {
        public static string SerializeObj<T>(T modelObjects) => JsonSerializer.Serialize(modelObjects);
        public static T DeserializeJsonString<T>(string jsonString) => JsonSerializer.Deserialize<T>(jsonString);

        // public static IList<T> DeserializeJSonStringList<T>(string jsonString) => JsonSerializer.Deserialize<IList>;
    }
}
