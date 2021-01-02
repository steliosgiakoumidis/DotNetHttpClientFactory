using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace DotNetHttpClientFactory
{
    /// <summary>
    /// A helper class to create HttpClient and respecting the DNS changes in a performant way 
    /// </summary>
    public class HttpClientFactory : IHttpClientFactory
    {
        private ConcurrentQueue<HttpClientHandler> _handlerQueue = new ConcurrentQueue<HttpClientHandler>();
        private Timer _timer = new Timer(TimeSpan.FromMinutes(2).TotalMilliseconds) {AutoReset = true, Enabled = true};
        private TimeSpan _waitingTimeBeforeDisposal = TimeSpan.FromMinutes(2); 
        private Action<HttpClientHandler> _handlerAction;
        private Action<System.Net.Http.HttpClient> _clientAction;
        /// <summary>
        /// The name of your client factory. Will be used to select the right factory out of an IEnumeble when you register multiple implementations on the same interface 
        /// </summary>
        public string FactoryName { get; set; }

        /// <summary>
        /// Creates a new http client factory with no client or client handler options
        /// </summary>
        /// <param name="name">The name of the factory</param>
        public HttpClientFactory(string name)
        {
            FactoryName = name;
            InitializeJob();
        }


        /// <summary>
        /// Creates a new http client factory with http client options
        /// </summary>
        /// <param name="name">The name of the factory</param>
        /// <param name="action">Options for the http client</param>
        public HttpClientFactory(string name, Action<HttpClient> action)
        {
            FactoryName = name;
            _clientAction = action;
            InitializeJob();
        }

        /// <summary>
        /// Creates a new http client factory with http client and http client handler options
        /// </summary>
        /// <param name="name">The name of the factory</param>
        /// <param name="clientAction">Options for the http client</param>
        /// <param name="handlerAction">Options for the http client handler</param>
        public HttpClientFactory(string name, Action<HttpClient> clientAction, Action<HttpClientHandler> handlerAction)
        {
            FactoryName = name;
            _clientAction = clientAction;
            _handlerAction = handlerAction;
            InitializeJob();        
        }
        
        private void InitializeJob()
        {
            SetupCleanUpJob();
            RegisterHandler();
        }
        
        private void RegisterHandler()
        {
            _handlerQueue.Enqueue(GetHandler());
        }

        private void SetupCleanUpJob()
        {
            _timer.Elapsed += TimerOnElapsed;
        }
        
        private async void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _handlerQueue.Enqueue(GetHandler());

            if (!_handlerQueue.TryDequeue(out var previousHandler))
                throw new InvalidOperationException("Handler not found in queue");

            await Task.Delay(_waitingTimeBeforeDisposal);
            
            previousHandler.Dispose();
        }

        /// <summary>
        /// Gets an http client with all options applied whose handler is recycled in order to cater for the dns record changes
        /// </summary>
        /// <returns>An HttpClient</returns>
        public HttpClient GetClient()
        {
            var handlerPeekedSuccesfully = _handlerQueue.TryPeek(out var httpClientHandler);
            if (!handlerPeekedSuccesfully)
            {
                throw new InvalidOperationException("No handler was found enqueued");
            }
            
            var httpClient = new HttpClient(httpClientHandler, false);
            _clientAction?.Invoke(httpClient);
            
            return httpClient;
        }

        private HttpClientHandler GetHandler()
        {
            var handler = new HttpClientHandler();
            if (_handlerAction == null)
                return handler;
            
            _handlerAction(handler);

            return handler;
        }
    }
}