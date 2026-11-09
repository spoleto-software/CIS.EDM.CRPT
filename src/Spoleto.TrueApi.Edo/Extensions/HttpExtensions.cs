namespace Spoleto.TrueApi.Edo.Extensions
{
    internal static class HttpExtensions
    {
        public static void ConfigureHttpClient(this HttpClient client)
        {
            client.Timeout = new TimeSpan(0, 0, 5, 0);
        }
    }
}
