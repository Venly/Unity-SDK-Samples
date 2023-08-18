using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Venly.Companion;

namespace VenlyDash_Azure
{
    public static class VenlyDashEntry
    {
        [FunctionName("VenlyDashEntry")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await VenlyAzure.HandleRequest(req, typeof(VenlyDashExtensions));
        }
    }
}
