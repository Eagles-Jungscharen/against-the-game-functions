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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/{id}/run")] HttpRequest req,
            ILogger log, string id, [Table("Game")] CloudTable gameTable, [Table("TaskElements")] CloudTable teTable,[Table("GameRun")] CloudTable gameRunTable, [Table("TaskElementsRun")] CloudTable teRunTable)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            string action = req.Query["action"];
            string clientRunnerId = req.Headers["runnerClientId"];
            switch(action) {
                case "start":
                    return await RunAPI.StartGame(id, clientRunnerId, gameTable, teTable, gameRunTable, teRunTable);
                case "end":
                    return await RunAPI.EndGame(id, clientRunnerId, gameTable, teTable, gameRunTable, teRunTable);
                case "pause":
                    return await RunAPI.PauseGame(id, clientRunnerId, gameTable, teTable, gameRunTable, teRunTable);
                case "reset":
                    return await RunAPI.ResetGame(id, clientRunnerId, gameTable, teTable, gameRunTable, teRunTable);
                case "assignTask":
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic taskIdBody = JsonConvert.DeserializeObject(requestBody);
                    string taskId = taskIdBody.taskId;
                    return await RunAPI.AssignTask(id, taskId,clientRunnerId, gameTable, teTable, gameRunTable, teRunTable);
                case "completeTask":
                    string requestBodyCT = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic taskIdBodyCT = JsonConvert.DeserializeObject(requestBodyCT);
                    string taskIdCT = taskIdBodyCT.taskId;
                    return await RunAPI.FinishTask(id, taskIdCT, clientRunnerId, gameTable, teTable, gameRunTable, teRunTable);
                case "status":
                    return await RunAPI.GameStatus(id, clientRunnerId, gameTable, teTable, gameRunTable, teRunTable);
                default:
                    return new BadRequestResult();
            }
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
        [FunctionName("SaveGame")]
        public static async Task<IActionResult> SaveGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/{id}")] HttpRequest req,
            ILogger log, string id, [Table("GameRegistry")] CloudTable table, [Table("Game")] CloudTable gameTable)
        {
            if (!req.Headers.ContainsKey("game-sec")) {
                return new UnauthorizedResult();
            }
            string secureId = req.Headers["game-sec"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Game inputGame = JsonConvert.DeserializeObject<Game>(requestBody);

            GameRegistry regEntry = await table.GetByIdAsync<GameRegistry>(inputGame.Number, "GameRegistry");
            if (secureId != regEntry.Verification) {
                return new UnauthorizedResult();
            }

            Game game = await gameTable.GetByIdAsync<Game>(id,"Game");
            if (game == null) {
                return new NotFoundResult();
            }
            game.Update(inputGame);
            await gameTable.InsertOrReplaceAsync(id,"Game",game);
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

        [FunctionName("GetTaskElements")]
        public static async Task<IActionResult> GetTaskElements(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "taskelements")] HttpRequest req,
            ILogger log,[Table("TaskElements")] CloudTable table)
        {
            string gameId = req.Query["gameId"];
            List<TaskElement> elements = await table.GetAllAsync<TaskElement>(gameId);
            return new OkObjectResult(elements);
        }
        [FunctionName("SaveTaskElements")]
        public static async Task<IActionResult> SaveTaskElements(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "taskelements")] HttpRequest req,
            ILogger log,[Table("TaskElements")] CloudTable table, [Table("GameRegistry")] CloudTable regTable, [Table("Game")] CloudTable gameTable)
        {
            string secureId = req.Headers["game-sec"];
            string gameId = req.Query["gameId"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TaskElement data = JsonConvert.DeserializeObject<TaskElement>(requestBody);

            Game game = await gameTable.GetByIdAsync<Game>(gameId,"Game");
            GameRegistry regEntry = await regTable.GetByIdAsync<GameRegistry>(game.Number, "GameRegistry");
            if (regEntry == null) {
                return new NotFoundResult();
            }
            if (regEntry.Verification != secureId) {
                return new UnauthorizedResult();
            }
            if (data.Id =="@new") {
                data.Id = Guid.NewGuid().ToString();
            }
            await table.InsertOrReplaceAsync(data.Id, gameId, data);
            return new OkObjectResult(data);
        }

    }
}
