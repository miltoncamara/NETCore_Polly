using Polly;
using System;
using System.Net.Http;
using System.Threading;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello Polly!");
            //Retry();
            //RetryForever();
            //WaitAndRetry();
            //Timeout();
            CircuitBreaker();
        }

        static void Timeout()
        {
            var timeout = Policy.Timeout(5, Polly.Timeout.TimeoutStrategy.Pessimistic, (ctx, timespan, ex) =>
            {
                Console.WriteLine($"Request failed.");
            });

            timeout.Execute(() =>
            {
                CallWS();
            });
        }

        static void CircuitBreaker()
        {
            var circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreaker(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(60)
            );

            var policy = Policy
            .Handle<Exception>()
            .Fallback(() => GetFallbackResponse())
            .Wrap(circuitBreaker);

            for (int i = 0; i <= 1000; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(3));

                if (circuitBreaker.CircuitState == Polly.CircuitBreaker.CircuitState.Closed)
                    Console.BackgroundColor = ConsoleColor.Magenta;
                if (circuitBreaker.CircuitState == Polly.CircuitBreaker.CircuitState.Open)
                    Console.BackgroundColor = ConsoleColor.Red;
                if (circuitBreaker.CircuitState == Polly.CircuitBreaker.CircuitState.HalfOpen)
                    Console.BackgroundColor = ConsoleColor.Blue;

                Console.WriteLine($"Estado do circuito {circuitBreaker.CircuitState}");

                policy.Execute(() =>
                {
                    CallWS();
                });
            }
        }

        static void GetFallbackResponse()
        {
            Console.ResetColor();
            Console.WriteLine($"Leia do Cache.");
        }

        static void WaitAndRetry()
        {
            var waitRetry = Policy
                              .Handle<Exception>()
                              .WaitAndRetry(3, i => TimeSpan.FromSeconds(5), (result, timeSpan, retryCount, context) =>
                              {
                                  Console.WriteLine($"Request failed. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                              });

            waitRetry.Execute(() =>
            {
                CallWS();
            });
        }

        static void RetryForever()
        {
            var retry = Policy.Handle<Exception>().RetryForever();

            retry.Execute(() =>
            {
                CallWS();
            });
        }

        static void CallWS()
        {
            //Thread.Sleep(TimeSpan.FromSeconds(10));
            Console.ResetColor();
            Console.WriteLine("Entrou CallWS");

            var httpClient = new HttpClient();
            var result = httpClient.GetAsync("http://demo3540014.mockable.io/polly").Result;
            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Erro ao chamar WS");

            Console.WriteLine(result.Content.ReadAsStringAsync().Result);
        }
    }
}
