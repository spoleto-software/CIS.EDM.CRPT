using System.Text.Json;

namespace Spoleto.TrueApi.Edo.Exceptions
{
    /// <summary>
    /// The exception thrown when JsonException occurs.
    /// </summary>
    [Serializable]
    public class JsonParsingException : Exception
    {
        private const string _exceptionMessage = $"The received JSON cannot be deserialized.";

        public JsonParsingException(string json, JsonException originalException)
            : base(_exceptionMessage, originalException)
        {
            Json = json;
        }

        public string Json { get; set; }
    }
}
