# DotNetHttpClientFactory

## The problem

DotNetHttpClientFactory is a class that helps dotnet projects to over come a known issue with HttpClient. The dilemma using HttpCleint is the following:

1) Create new instances of HttpClient, degrade performance and risk socket depletion when application under heavy load.

2) Create a static HttpClient and and in case the website we are calling, falls over to a different IP address the calls fail (DNS changes are not respected in static clients).

To address this issue the DotNetHttpClientFactory project was created.  

## The solution

This problem is solved by creating non-disposed HttpClientHanlders, the object responsible for handling the connection. The handler is reused in every http call and recycled every 2 minutes. The reason of the recycling is to refresh the dns records. This way we both have a light weight creation of HttpClients and we respect the Dns records updates  

## How to use

1) Use any depedency injection framework and inject different implementations of the same interface (IHttpCleintFactory). In the example below will be used Autofac:

    ```csharp
    var builder = new ContainerBuilder();
    var clientFactoryGoogle = new HttpClientFactory("Google");
    var clientFactoryYahoo = new HttpClientFactory("Yahoo", opt =>
            {
                opt.BaseAddress = new Uri("https://www.google.com");
                opt.Timeout = TimeSpan.FromMinutes(1);
            });
            
    var clientFactoryAmazon = new HttpClientFactory("Amazon", opt =>
            {
                opt.BaseAddress = new Uri("https://www.google.com");
                opt.Timeout = TimeSpan.FromMinutes(1);
            }, opt =>
            {
                opt.Credentials = new CredentialCache();
                opt.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;

            });
   
    builder.RegisterInstance(clientFactoryGoogle).As<IHttpClientFactory>();
    builder.RegisterInstance(clientFactoryYahoo).As<IHttpClientFactory>();
    builder.RegisterInstance(clientFactoryAmazon).As<IHttpClientFactory>();
    var container = builder.Build();
    ```
   Autofac will automatically put all viewers into an IEnumerable<IHttpClientFactory>, which you can resolve like this:
   ```csharp
   var httpClientFactories = container.Resolve<IEnumerable<IHttpClientFactory>>();
   ```
   Then you can select the appropriate factory be querying the name its name:
   ```csharp
   private readonly IEnumerable<IHttpClientFactory> _clientFactories;

   public HomeController(IEnumerable<IHttpClientFactory> clientFactories)
   {
   _clientFactories = clientFactories;
   }
   
   var clientFactory = _clientFactories.Single(it => it.FactoryName == "Google");
   
   using var client = clientFactory.GetClient();   
    ```
   
 2) Create a factory with different implementations and pass it where necessary:
    ```csharp
    IEnumerable<IHttpClientFactory> clientFactories = new List<IHttpClientFactory>()
    {
        new HttpClientFactory("Google", o =>
        {
            o.BaseAddress = new Uri("https://google.com");
        }),
        new HttpClientFactory("Amazon", httpClient =>
        {
            httpClient.BaseAddress = new Uri("https://www.amazon.com/");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }, handler =>
        {
            handler.MaxResponseHeadersLength = 53;
            handler.PreAuthenticate = true;
            handler.AllowAutoRedirect = true;
            handler.Proxy = new WebProxy();
        })
    };    
     var clientFactory = clientFactories.Single(it => it.FactoryName == "Google");   
     using var client = clientFactory.GetClient()
    ```
    
 3) Simplest way is to use the static version of the class where unfortunately no http client handler or http client options are available fir modification for thread safety purposes.
    ```csharp
    using var client = HttpClientFactoryStatic.GetClient();
    ```
## Contact
For further info feel free to contact me at stelios.giakoumidis[at]gmail.com

