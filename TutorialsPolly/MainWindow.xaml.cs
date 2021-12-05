using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Polly;
using Polly.Caching;

namespace TutorialsPolly
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Api = "http://localhost/WebAPi/api/v1/customerss";
        private HttpClient httpClient;
        public MainWindow()
        {
            InitializeComponent();

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("accept", "application/json");
        }


        #region Retry Policy

        private async void BtnRetryOnce_Click(object sender, RoutedEventArgs e)
        {
            //it will retry once only

            var retryOncePolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                                    .Or<HttpRequestException>()
                                    .RetryAsync(onRetry: (result,retryCount,context)=>
                                    {
                                        Debug.WriteLine($"RetryAttempt:{retryCount},StatusCode:{result.Result.StatusCode}");
                                    });



            var response = await retryOncePolicy.ExecuteAsync(async()=> await httpClient.GetAsync(Api));


            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }

        }
        private async void BtnRetryTimes_Click(object sender, RoutedEventArgs e)
        {
            //it will retry 3 time until success or retry time out
            var retryTimesPolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                .Or<HttpRequestException>()
                .RetryAsync(3,onRetry: (result, retryCount, context) =>
                {
                    Debug.WriteLine($"RetryAttempt:{retryCount},StatusCode:{result.Result.StatusCode}");
                });

            var response = await retryTimesPolicy.ExecuteAsync(async () => await httpClient.GetAsync(Api));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }
        }
        private async void BtnRetryForever_Click(object sender, RoutedEventArgs e)
        {
            //it will retry for ever until success 

            var retryForeverPolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                .Or<HttpRequestException>()
                .RetryForeverAsync(onRetry: (result, retryCount, context) =>
                {
                    Debug.WriteLine($"RetryAttempt:{retryCount},StatusCode:{result.Result.StatusCode}");
                });

            var response = await retryForeverPolicy.ExecuteAsync(async () => await httpClient.GetAsync(Api));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }
        }

        private async void BtnWaitAndRetry_Click(object sender, RoutedEventArgs e)
        {
            //it will execute method and if fail it will wait 3 second and retry until success or retry count out of 5 times

            var waitAndRetryPolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(retryCount:5,
                                   sleepDurationProvider: retryCounter => TimeSpan.FromSeconds(3),
                                   onRetry: (result, retryCount, context) =>
                                   {
                                       Debug.WriteLine($"RetryAttempt:{retryCount},StatusCode:{result.Result.StatusCode}");
                                   });

            var response = await waitAndRetryPolicy.ExecuteAsync(async () => await httpClient.GetAsync(Api));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }
        }

        private async void BtnWaitAndRetryForever_Click(object sender, RoutedEventArgs e)
        {
            //it will execute method and if fail it will wait 3 second and retry until success

            var waitAndRetryForeverPolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                .Or<HttpRequestException>()
                .WaitAndRetryForeverAsync(sleepDurationProvider: retryCounter => TimeSpan.FromSeconds(3),
                                            onRetry: (result, retryCount, context) =>
                                            {
                                                Debug.WriteLine($"RetryAttempt:{retryCount},StatusCode:{result.Result.StatusCode}");
                                            });

            var response = await waitAndRetryForeverPolicy.ExecuteAsync(async () => await httpClient.GetAsync(Api));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }
        }

        #endregion

        #region Fallback Policy

        private async void BtnFallback_Click(object sender, RoutedEventArgs e)
        {
            var customer = new
            {
                FirstName="Ahmed",
                LastName="Ali"
            };

            //When Fail Return default value
            var fallbackPolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                .Or<HttpRequestException>()
                .FallbackAsync(new HttpResponseMessage(HttpStatusCode.OK) {Content=new ObjectContent(customer.GetType(), customer,new JsonMediaTypeFormatter()) },
                                onFallbackAsync: result=>
                                {
                                    return Task.Run(()=>Debug.WriteLine($"Fallback , StatusCode:{result.Result.StatusCode}"));
                                }
                              );

            var response = await fallbackPolicy.ExecuteAsync(async () => await httpClient.GetAsync(Api));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }
        }

        #endregion

        #region CircuitBreaker Policy

        private async void BtnCircuitBreaker_Click(object sender, RoutedEventArgs e)
        {
            //When Fail 3 Request it stop Calling Service again

            var circuitBreakerPolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking:3,
                                     durationOfBreak:TimeSpan.FromSeconds(3));

            var response = await circuitBreakerPolicy.ExecuteAsync(async () => await httpClient.GetAsync(Api));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }
        }


        #endregion

        #region Wrapped Policy

        private async void BtnWrappedRetryWithFallback_Click(object sender, RoutedEventArgs e)
        {
            //When Fail start try 3 times after that apply fallback value

            var customer = new
            {
                FirstName = "Ahmed",
                LastName = "Ali"
            };

            var retryTimesPolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                .Or<HttpRequestException>()
                .RetryAsync(3, onRetry: (result, retryCount, context) =>
                {
                    Debug.WriteLine($"RetryAttempt:{retryCount},StatusCode:{result.Result.StatusCode}");
                });

            var fallbackPolicy = Policy.HandleResult<HttpResponseMessage>(x => x.IsSuccessStatusCode == false)
                .Or<HttpRequestException>()
                .FallbackAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ObjectContent(customer.GetType(), customer, new JsonMediaTypeFormatter()) },
                    onFallbackAsync: result =>
                    {
                        return Task.Run(() => Debug.WriteLine($"Fallback , StatusCode:{result.Result.StatusCode}"));
                    }
                );

            //Wrap Fallback Around Retry Policy
            var wrappedPolicy = fallbackPolicy.WrapAsync(retryTimesPolicy);




            var response = await wrappedPolicy.ExecuteAsync(async () => await httpClient.GetAsync(Api));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }

        }

        #endregion

        #region Timeout Policy

        private async void BtnTimeout_Click(object sender, RoutedEventArgs e)
        {
            //when not success during 1 second then timeout and stop request

            var timeoutPolicy = Policy.TimeoutAsync(seconds: 1, onTimeoutAsync: async (context, timespan, inputTask) => Debug.WriteLine($"Timeout Over {timespan.TotalSeconds} Second"));

            var response = await timeoutPolicy.ExecuteAsync(async () => await httpClient.GetAsync(Api));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }

        }

        #endregion

        #region Cache Policy

        private async void BtnCache_Click(object sender, RoutedEventArgs e)
        {
            
            var cachePolicy =  Policy.CacheAsync<HttpResponseMessage>(new InMemoryCacheProvider(),
                                                                    TimeSpan.FromSeconds(60),
                                                                    onCacheError: (context, key, exception) =>
                                                                    {
                                                                        Debug.WriteLine($"Cache Error {key}");
                                                                    }
                                                                    );

            var response = await cachePolicy.ExecuteAsync(async (context) => await httpClient.GetAsync(Api),new Context("MyOperationKey"));


            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Request Success");
            }
            else
            {
                MessageBox.Show("Request Fail");
            }
        }

        private class InMemoryCacheProvider:IAsyncCacheProvider
        {
            public async Task<(bool, object)> TryGetAsync(string key, CancellationToken cancellationToken, bool continueOnCapturedContext)
            {
                throw new NotImplementedException();
            }

            public async Task PutAsync(string key, object value, Ttl ttl, CancellationToken cancellationToken, bool continueOnCapturedContext)
            {
                throw new NotImplementedException();
            }
        }

        #endregion


    }
}
