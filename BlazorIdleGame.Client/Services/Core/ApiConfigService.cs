using Microsoft.Extensions.Configuration;

namespace BlazorIdleGame.Client.Services.Core
{
    public interface IApiConfigService
    {
        string ApiBaseUrl { get; }
        void UpdateApiBaseUrl(string newUrl);
    }

    public class ApiConfigService : IApiConfigService
    {
        private readonly IConfiguration _configuration;
        private string _apiBaseUrl;

        public ApiConfigService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001/";
        }

        public string ApiBaseUrl => _apiBaseUrl;

        public void UpdateApiBaseUrl(string newUrl)
        {
            if (!string.IsNullOrWhiteSpace(newUrl))
            {
                _apiBaseUrl = newUrl.EndsWith('/') ? newUrl : newUrl + "/";
            }
        }
    }
}