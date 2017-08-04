﻿using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Common.Logging;

namespace Snowflake.Data.Core
{
    public class RestRequestImpl : IRestRequest
    {
        private static ILog logger = LogManager.GetLogger<RestRequestImpl>();

        private static readonly RestRequestImpl instance = new RestRequestImpl();

        private static MediaTypeHeaderValue applicationJson = new MediaTypeHeaderValue("applicaion/json");

        private static MediaTypeWithQualityHeaderValue applicationSnowflake = new MediaTypeWithQualityHeaderValue("application/snowflake");

        private const string SF_AUTHORIZATION_HEADER = "Authorization";

        private const string SSE_C_ALGORITHM = "x-amz-server-side-encryption-customer-algorithm";

        private const string SSE_C_KEY = "x-amz-server-side-encryption-customer-key";

        private const string SSE_C_AES = "AES256";

        private RestRequestImpl()
        {
        }
        
        static internal RestRequestImpl Instance
        {
            get { return instance; }
        }

        public JObject post(SFRestRequest postRequest)
        {
            var json = JsonConvert.SerializeObject(postRequest.jsonBody);
            HttpContent httpContent = new StringContent(json);
            CancellationTokenSource cancellationTokenSource = 
                new CancellationTokenSource(postRequest.timeout);

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, postRequest.uri);

            message.Content = httpContent;
            message.Content.Headers.ContentType = applicationJson;
            message.Headers.Add(SF_AUTHORIZATION_HEADER, postRequest.authorizationToken);
            message.Headers.Accept.Add(applicationSnowflake);

            var responseContent = sendRequest(message, cancellationTokenSource.Token).Content;

            var jsonString = responseContent.ReadAsStringAsync();
            jsonString.Wait();

            return JObject.Parse(jsonString.Result);
        }

        public HttpResponseMessage get(S3DownloadRequest getRequest)
        {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, getRequest.uri);
            message.Headers.Add(SSE_C_ALGORITHM, SSE_C_AES);
            message.Headers.Add(SSE_C_KEY, getRequest.qrmk);

            CancellationTokenSource cancellationTokenSource = 
                new CancellationTokenSource(getRequest.timeout);

            return sendRequest(message, cancellationTokenSource.Token);
        }

        public JObject get(SFRestRequest getRequest)
        {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, getRequest.uri);
            message.Headers.Add(SF_AUTHORIZATION_HEADER, getRequest.authorizationToken);
            message.Headers.Accept.Add(applicationSnowflake);

            CancellationTokenSource cancellationTokenSource = 
                new CancellationTokenSource(getRequest.timeout);

            var responseContent = sendRequest(message, cancellationTokenSource.Token).Content;

            var jsonString = responseContent.ReadAsStringAsync();
            jsonString.Wait();

            return JObject.Parse(jsonString.Result);
        }

        private HttpResponseMessage sendRequest(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            try
            {
                var response = HttpUtil.getHttpClient().SendAsync(requestMessage, cancellationToken)
                    .Result.EnsureSuccessStatusCode();

                return response;
            }
            catch(AggregateException e)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new SFException(SFError.REQUEST_TIMEOUT);
                }
                else
                {
                    throw e;
                }
            }
        }
    }

}
