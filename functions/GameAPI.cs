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
using EaglesJungscharen.ATG.Models;

namespace EaglesJungscharen.ATG
{
    public static class GameAPI
    {
        [FunctionName("StartGame")]
        public static async Task<IActionResult> StartGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/{id}/start")] HttpRequest req,
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
            ILogger log, [Table("GameRegistry")] CloudTable table, [Table("Game")] CloudTable gameTable)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string email = data?.email;
            log.LogInformation("E-Mail is: "+email);
            string gameCode = GameNumberUtil.GenerateGameNumber();
            string secureId = GameNumberUtil.GenerateSecureNumber();
            string gameId = Guid.NewGuid().ToString();
            GameRegistry regEntry = new GameRegistry() {
                Code = gameCode,
                EMail=email,
                GameId = gameId,
                Verification=secureId
            };
            Game game = new Game() {
                Id = gameId,
                Number = gameCode
            };
            await gameTable.InsertOrReplaceAsync(gameId, "Game",game);
            await table.InsertOrReplaceAsync(gameCode,"GameRegistry", regEntry);
            
            await SendGridUtil.SendGameMail(email,gameCode,secureId);

            return new OkObjectResult(new {game=gameCode});
        }
        [FunctionName("GetGame")]
        public static async Task<IActionResult> GetGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}")] HttpRequest req,
            ILogger log, string id, [Table("Game")] CloudTable gameTable)
        {
            Game game = await gameTable.GetByIdAsync<Game>(id,"Game");
            if (game == null) {
                return new NotFoundResult();
            }
            return new OkObjectResult(game);
        }
        [FunctionName("CheckEditGame")]
        public static async Task<IActionResult> CheckEditGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkeditgame")] HttpRequest req,
            ILogger log,[Table("GameRegistry")] CloudTable table)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CheckGameRequest data = JsonConvert.DeserializeObject<CheckGameRequest>(requestBody);
            GameRegistry regEntry = await table.GetByIdAsync<GameRegistry>(data.Game, "GameRegistry");
            if (regEntry == null) {
                return new NotFoundResult();
            }
            if (regEntry.Verification != data.Verification) {
                return new UnauthorizedResult();
            }
            return new OkObjectResult(new GameLoginEvent(){
                Action = "edit",
                GameId = regEntry.GameId
            });
        }
        [FunctionName("JoinGame")]
        public static async Task<IActionResult> JoinGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "joingame")] HttpRequest req,
            ILogger log,[Table("GameRegistry")] CloudTable table)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CheckGameRequest data = JsonConvert.DeserializeObject<CheckGameRequest>(requestBody);
            GameRegistry regEntry = await table.GetByIdAsync<GameRegistry>(data.Game, "GameRegistry");
            if (regEntry == null) {
                return new NotFoundResult();
            }
            return new OkObjectResult(new GameLoginEvent(){
                Action = "run",
                GameId = regEntry.GameId
            });
        }
    }
}
