using GBPackingSlipAttachmentService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GBPackingSlipAttachmentService.Utilities
{
    public class RestHelper
    {
        public readonly IIFSAuthenticationService _authenticationService;
        public readonly ILogging _logger;
        public readonly IConfiguration _configuration;
        public readonly HttpClient _httpClient;

        public RestHelper(IConfiguration configuration, ILogging logger)
        {
            _configuration = configuration;
            _logger = logger;
            _authenticationService = new IFSAuthenticationService(_configuration, _logger);
            _httpClient = new HttpClient();
        }

        public Uri CreateRequestUri(string serviceUri)
        {
            var uriBuilder = new UriBuilder(new Uri($"{_configuration["Ifs:BaseUrl"]}/{_configuration["Ifs:ResourceUrl"]}/{serviceUri}"));
            return uriBuilder.Uri;
        }

        public void addHeaders()
        {
            var accessToken = _authenticationService.GetBearerToken();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json;odata.metadata=full;IEEE754Compatible=true");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        }

        public async Task<dynamic> PostAsync(Uri requestUri, string jsonBody)
        {
            try
            {
                addHeaders();
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUri.ToString(), content);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<dynamic>(jsonResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RestHelper - PostAsync => {requestUri}", ex);
                throw;
            }
        }

        public async Task<dynamic> GetAsync(Uri requestUri)
        {
            try
            {
                addHeaders();
                var response =  await _httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<dynamic>(jsonResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RestHelper - GetAsync => {requestUri}", ex);
                throw;
            }
        }

        public async Task<dynamic> PatchAsync(Uri requestUri, string etag, string itemId, string filePath) 
        {
            try
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
                    {
                        Content = new StreamContent(fileStream)
                    };

                    var accessToken = _authenticationService.GetBearerToken();
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Headers.Add("X-XSRF-TOKEN", "your-xsrf-token");
                    request.Headers.Add("If-Match", etag); // ETag required for the patch request
                    request.Headers.Add("X-IFS-Content-Disposition", "filename=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(filePath))));

                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"RestHelper - PatchAsync => Packing slip attached to receipt id: {Path.GetFileName(filePath)}");
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                }

            }
            catch (Exception ex) 
            {
                _logger.LogError($"RestHelper - PatchAsync => Failed uploading file {Path.GetFileName(filePath)}", ex);
                throw;
            }
        }
    }

}
