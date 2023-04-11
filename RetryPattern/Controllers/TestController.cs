using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace RetryPattern.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

        public TestController()
        {
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(5, retryAttempt => {
                    Console.WriteLine($"Attempt {retryAttempt} failed. Waiting 2 seconds");
                    return TimeSpan.FromSeconds(4);
                });

            _circuitBreakerPolicy = Policy.Handle<Exception>()
                .CircuitBreakerAsync(1, TimeSpan.FromMinutes(1),
                (ex, t) =>
                {
                    Console.WriteLine("Circuit broken!");
                },
                () =>
                {
                    Console.WriteLine("Circuit Reset!");
                });
        }

        [HttpGet("[action]")]
        public async Task<string> GetHelloMessageAsync()
        {
            Console.WriteLine("GetHelloMessage running");
            try
            {
                return await _retryPolicy.ExecuteAsync(Hello);
            }
            catch
            {
                return "Fail";
            }
        }

        [HttpGet("[action]")]
        public async Task<string> GetGoodbyeMessageAsync()
        {
            Console.WriteLine("GetGoodbyeMessage running");
            ThrowRandomException();
            return await _circuitBreakerPolicy.ExecuteAsync(Goodbye);
        }

        private static Task<string> Hello()
        {
            ThrowRandomException();
            return Task.FromResult("Hello");
        }

        private static Task<string> Goodbye()
        {
            return Task.FromResult("Goodbye");
        }

        private static void ThrowRandomException()
        {
            var diceRoll = new Random().Next(0, 10);

            if (diceRoll > 5)
            {
                Console.WriteLine("ERROR! Throwing Exception");
                throw new Exception("Exception");
            }
        }

    }
}