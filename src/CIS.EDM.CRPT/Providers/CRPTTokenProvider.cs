using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CIS.Cryptography;
using CIS.EDM.CRPT.Models;
using CIS.EDM.Extensions;
using CIS.EDM.Helpers;

namespace CIS.EDM.CRPT.Providers
{
    public class CRPTTokenProvider : ICRPTTokenProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CRPTTokenProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        async Task<TokenModel> ICRPTTokenProvider.GetTokenAsync(CRPTOption settings)
        {
            using var client = _httpClientFactory.CreateClient();
            client.ConfigureHttpClient();

            var authKey = await GetAuthKey(client, settings).ConfigureAwait(false);

            var uri = new Uri(new Uri(settings.ServiceUrl), "/api/v1/session");
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                requestMessage.ConfigureRequestMessage();

                var data = HttpHelper.ConvertToBase64(authKey.Data);
                authKey.Data = CryptographyHelper.SignBase64Data(data, thumbprint: settings.CertificateThumbprint);

                var authKeyJson = HttpHelper.ToJson(authKey);
                requestMessage.Content = new StringContent(authKeyJson, DefaultSettings.Encoding, DefaultSettings.ContentType);
                using (var responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false))
                {
                    var result = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        TokenModel tokenModel = null;
                        try
                        {
                            tokenModel = HttpHelper.FromJson<TokenModel>(result);
                        }
                        catch (Exception)
                        {
                            throw new Exception($"Ошибка формирования токена доступа\n{result}");
                        }

                        return tokenModel;
                    }
                    else if (!string.IsNullOrEmpty(result))
                    {
                        ErrorModel errorModel = null;
                        try
                        {
                            errorModel = HttpHelper.FromJson<ErrorModel>(result);
                        }
                        catch (Exception e)
                        {
                            errorModel = new ErrorModel { Errors = new List<ErrorInfo> { new ErrorInfo { ErrorMessage = result } } };
                        }

                        throw new Exception(errorModel.ToString());
                    }
                    else
                    {
                        throw new Exception(responseMessage.ReasonPhrase);
                    }
                }
            }
        }

        private async Task<AuthKeyModel> GetAuthKey(HttpClient client, CRPTOption settings)
        {
            var uri = new Uri(new Uri(settings.ServiceUrl), "/api/v1/session");
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                requestMessage.ConfigureRequestMessage();

                using (var responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false))
                {
                    var result = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        AuthKeyModel tokenModel = null;
                        try
                        {
                            tokenModel = HttpHelper.FromJson<AuthKeyModel>(result);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Ошибка формирования токена доступа\n{result}");
                        }

                        return tokenModel;
                    }
                    else if (!string.IsNullOrEmpty(result))
                    {
                        throw new Exception(result);
                    }
                    else
                    {
                        throw new Exception(responseMessage.ReasonPhrase);
                    }
                }
            }
        }
    }
}
