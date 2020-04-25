using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace HttpClientBestPractices
{
    public class HttpClientWrapper
    {
        /// <summary>
        /// Use static to reduce the waste of sockets by reusing them.
        /// </summary>
        private static readonly HttpClient _httpClient;

        static HttpClientWrapper()
        {
            // See https://docs.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler
            var socketHandler = new SocketsHttpHandler();
            // How long connection will be opened. By default is -1 which means that connection wouldn't ever be closed.
            // Setting it to 0 will will close the connection after each request.
            socketHandler.PooledConnectionLifetime = TimeSpan.FromSeconds(60);
            // How long a connection can be idle in the pool to be considered reusable.
            // Idle means no data transfer
            socketHandler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(100);

            _httpClient = new HttpClient(socketHandler);
        }

        // User HttpCompletionOption enum to increa
        public async Task<IEnumerable<T>> Get<T>(Uri uri)
        {
            using var response = await _httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead
            );

            response.EnsureSuccessStatusCode();

            return await JsonSerializer.DeserializeAsync<IEnumerable<T>>(
                response.Content.ReadAsStreamAsync().Result
            );
        }

        public async Task Post<T>(Uri uri, T data) =>
            await _httpClient.PostAsync(uri,
                new StringContent(
                    JsonSerializer.Serialize(data),
                    Encoding.UTF8,
                    "application/json"
                ));

        public async Task Put<T>(Uri uri, T data) =>
            await _httpClient.PutAsync(uri, new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json"
            ));

        public async Task SendAsync<T>(Uri uri, HttpMethod httpMethod, T data)
        {
            var httpRequestMessage = new HttpRequestMessage(httpMethod, uri);

            var test = JsonSerializer.Serialize(data);
            httpRequestMessage.Content = new StringContent(test);

            await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
        }
    }
}