using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using WebGate.Azure.CloudTableUtils.CloudTableExtension;

using EaglesJungscharen.ATG.Utils;

namespace EaglesJungscharen.ATG
{
    public static class GameAPI
    {
        [FunctionName("StartGame")]
        public static async Task<IActionResult> StartGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "game/{id}/start")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
        [FunctionName("NewGame")]
        public static async Task<IActionResult> NewGame(
            [HttpTrigger(AuthorizationLevel.Anonymous,  "post", Route = "registergame")] HttpRequest req,
            ILogger log, [Table("GameRegistry")] CloudTable table)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string email = data?.email;
            log.LogInformation("E-Mail is: "+email);
            string gameId = GameNumberUtil.GenerateGameNumber();
            string secureId = GameNumberUtil.GenerateSecureNumber();
            await SendGridUtil.SendGameMail(email,gameId,secureId);

            return new OkObjectResult(new {game=gameId});
        }
        [FunctionName("GetGame")]
        public static async Task<IActionResult> GetGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
        
    }
}
