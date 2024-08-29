using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GHIssue2851
{
    public static class Orchestrator
    {
        [Function(nameof(Orchestrator))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Orchestrator));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            // Replace name and input with values relevant for your Durable Functions Activity
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), (1, "Hello")));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [Function(nameof(SayHello))]
        public static (int, string) SayHello([ActivityTrigger] (int, string) name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(SayHello));
            logger.LogInformation($"Hello: {name}");
            return name;
        }

        [Function("Orchestrator_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Orchestrator_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(Orchestrator));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
