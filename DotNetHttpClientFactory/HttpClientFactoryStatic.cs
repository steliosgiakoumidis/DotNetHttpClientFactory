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
    public class HttpClientFactoryStatic
    {
        private static ConcurrentQueue<HttpClientHandler> _handlerQueue = new ConcurrentQueue<HttpClientHandler>();
        private static Timer _timer = new Timer(TimeSpan.FromMinutes(2).TotalMilliseconds) {AutoReset = true, Enabled = true};
        private static TimeSpan _waitingTimeBeforeDisposal = TimeSpan.FromMinutes(2); 
        private static Action<HttpClientHandler> _handlerAction;
        private Action<System.Net.Http.HttpClient> _clientAction;

        static HttpClientFactoryStatic()
        {
            InitializeJob();
        }

        private static void InitializeJob()
        {
            SetupCleanUpJob();
            RegisterHandler();
        }
        
        private static void RegisterHandler()
        {
            _handlerQueue.Enqueue(new HttpClientHandler());
        }

        private static void SetupCleanUpJob()
        {
            _timer.Elapsed += TimerOnElapsed;
        }
        
        private static async void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _handlerQueue.Enqueue(new HttpClientHandler());

            if (!_handlerQueue.TryDequeue(out var previousHandler))
                throw new InvalidOperationException("Handler not found in queue");

            await Task.Delay(_waitingTimeBeforeDisposal);
            
            previousHandler.Dispose();
        }

        /// <summary>
        /// Gets an http client whose handler is recycled in order to cater for the dns record changes
        /// </summary>
        /// <returns>An HttpClient</returns>
        public static HttpClient GetClient()
        {
            _handlerQueue.TryPeek(out var httpClientHandler);
            
            return new HttpClient(httpClientHandler, false);
        }
    }
}