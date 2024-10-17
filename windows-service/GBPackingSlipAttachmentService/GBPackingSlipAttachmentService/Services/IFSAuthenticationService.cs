using GBPackingSlipAttachmentService.Utilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace GBPackingSlipAttachmentService.Services
{
    public class IFSAuthenticationService : IIFSAuthenticationService
    {
        private readonly ILogging _logger;
        private readonly IConfiguration _configuration;
        private readonly ICredentialManager _credentialManager;
        private string _cachedToken;
        private DateTime _tokenExpiryTime;

        public IFSAuthenticationService(IConfiguration configuration, ILogging logger)
        {
            _configuration = configuration;
            _credentialManager = new CredentialManager();
            _logger = logger;
        }

        public string GetBearerToken()
        {
            try
            {
                if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiryTime > DateTime.UtcNow)
                {
                    return _cachedToken;
                }

                // Retrieve credentials
                var credentials = _credentialManager.GetCredentials(_configuration["Windows:Target"]);
                var options = new RestClientOptions(_configuration["Ifs:BaseUrl"]);
                options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                var client = new RestClient(options);
                var request = new RestRequest(_configuration["Ifs:TokenUrl"], Method.Post);

                //Remove after fixing bug
                var userName = "ACG_GB";
                var password = "9ncrDAIaN8RXFdxQSfb4V6oWGdk4YLPP";
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type", _configuration["Ifs:GrantType"]);
                request.AddParameter("client_secret", password);
                request.AddParameter("client_id", userName);

                var response = client.Execute(request);
                dynamic data = JObject.Parse(response.Content);

                _cachedToken = data.access_token;
                _tokenExpiryTime = DateTime.UtcNow.AddSeconds((int)data.expires_in);
                _logger.LogInformation("IFSAuthentication - GetBearerToken => Authenticated with IFS");
                return _cachedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError("IFSAuthentication - GetBearerToken => Error occurred", ex);
                throw;
            }
        }
    }
}
