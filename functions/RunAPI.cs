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
using System.Linq;
using EaglesJungscharen.ATG.Utils;
using EaglesJungscharen.ATG.Models;
namespace EaglesJungscharen.ATG {
    public static class RunAPI {
        public static async Task<IActionResult> StartGame(string gameId, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            Game game = await gameTable.GetByIdAsync<Game>(gameId,"Game");
            List<TaskElement> elements = await teTable.GetAllAsync<TaskElement>(gameId);
            RunGameSE rgs = await gameRunTable.GetByIdAsync<RunGameSE>(gameId, "RunGame");
            if (rgs == null) {
                rgs =new RunGameSE() {
                    Id = gameId
                };
            }
            if (rgs.RunnerClientId != null && rgs.RunnerClientId != clientRunnerId) {
                return new UnauthorizedResult();
            }
            rgs.RunnerClientId = clientRunnerId;
            rgs.Status = "running";
            await gameRunTable.InsertOrReplaceAsync(gameId,"RunGame", rgs);
            List<RunTaskElementSE> rte = await teRunTable.GetAllAsync<RunTaskElementSE>(gameId);
            List<TaskElement> listRTE = elements.Where(element=>rte.Find(cRTE=>cRTE.Id ==element.Id)==null).ToList();
            List<RunTaskElementSE> newRTE = listRTE.Select(newRTE=>{
                RunTaskElementSE rc = new RunTaskElementSE() {
                    Id = newRTE.Id,
                    Status = "notstarted",
                };
                teRunTable.InsertOrReplaceAsync(rc.Id,gameId,rc);
                return rc; 
            }).ToList();
            rte.AddRange(newRTE);
            List<RunTaskElement> listAllRTE = rte.Select(runTaskElementSE => runTaskElementSE.GetRunTaskElement(elements.Find(te=>te.Id == runTaskElementSE.Id))).ToList();
            listAllRTE.Sort((e1,e2)=>e1.TaskElement.No.CompareTo(e2.TaskElement.No));
            RunTaskElement element = listAllRTE.First();
            RunTaskElementSE elementSE =rte.Find(currentRTE=>currentRTE.Id == element.Id);
            element.Status = "running";
            element.StartTime = DateTime.Now;
            elementSE.Status = "running";
            elementSE.StartTime = DateTime.Now;
            await teRunTable.InsertOrReplaceAsync(elementSE.Id,gameId,elementSE);
            return new OkObjectResult(rgs.GetRunGame(game,listAllRTE));
        }

        public static async Task<IActionResult> EndGame(string id, string clientRunnerId, CloudTable gameRunTable) {
            return new OkObjectResult("");
        }
        public static async Task<IActionResult> FinishTask(string id, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            return new OkObjectResult("");
        }
        public static async Task<IActionResult> GameStatus(string gameId, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            Game game = await gameTable.GetByIdAsync<Game>(gameId,"Game");
            List<TaskElement> elements = await teTable.GetAllAsync<TaskElement>(gameId);
            RunGameSE rgs = await gameRunTable.GetByIdAsync<RunGameSE>(gameId, "RunGame");
            if (rgs == null) {
                return new OkObjectResult(new RunGame() {
                    Game = game,
                    Id = gameId,
                    Status = "notstarted",
                    CurrentPointsComputer = game.ComputerTeamPoints,
                    CurrentPointsPlayer = game.PlayerTeamPoints
                });
            }
            List<RunTaskElementSE> rte = await teRunTable.GetAllAsync<RunTaskElementSE>(gameId);
            List<RunTaskElement> listRTE = rte.Select(runTaskElementSE => runTaskElementSE.GetRunTaskElement(elements.Find(te=>te.Id == runTaskElementSE.Id))).ToList();
            RunGame currentRunGame = rgs.GetRunGame(game,listRTE);
            return new OkObjectResult(currentRunGame);
        }

    }
}