using System;
using System.Net;
using System.Net.Http;
using DotNetHttpClientFactory;

namespace HttpClientFactoryForDotNet
{
    class Program
    {
        private static HttpClient _client;
        static void Main(string[] args)
        {
            var client = new HttpClientFactory("Google");
            
            var client3 = new HttpClientFactory("Google", opt =>
            {
                opt.BaseAddress = new Uri("https://www.google.com");
                opt.Timeout = TimeSpan.FromMinutes(1);
            });
            
            var client4 = new HttpClientFactory("Google", opt =>
            {
                opt.BaseAddress = new Uri("https://www.google.com");
                opt.Timeout = TimeSpan.FromMinutes(1);
            }, opt =>
            {
                opt.Credentials = new CredentialCache();
                opt.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;

            });


            _client = new HttpClientFactory("Stelios").GetClient();


            using var client2 = HttpClientFactoryStatic.GetClient();
            
            
            Console.WriteLine("Hello World!");
        }
    }
}