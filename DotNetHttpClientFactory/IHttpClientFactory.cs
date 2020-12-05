using System.Net.Http;

namespace DotNetHttpClientFactory
{    
    public interface IHttpClientFactory
    {
        string FactoryName { get; set; }
        HttpClient GetClient();
    }
}