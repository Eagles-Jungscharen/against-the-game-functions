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
            bool isPause = rgs.Status == "paused";
            int pauseInSeconds = isPause && rgs.StartAt.HasValue? Convert.ToInt32((DateTime.Now- rgs.StartAt.Value).TotalSeconds):0;
            if (rgs == null) {
                rgs =new RunGameSE() {
                    Id = gameId,
                    StartAt= DateTime.Now
                };
            } else {
                if (rgs.Status != "running") {
                    rgs.StartAt = DateTime.Now;
                }
            }
            //if (rgs.RunnerClientId != null && rgs.RunnerClientId != clientRunnerId) {
            //    return new UnauthorizedResult();
            //}
            //rgs.RunnerClientId = clientRunnerId;
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
            if (!isPause) {
                RunTaskElement element = listAllRTE.First();
                RunTaskElementSE elementSE =rte.Find(currentRTE=>currentRTE.Id == element.Id);
                element.Status = "running";
                element.StartTime = DateTime.Now;
                elementSE.Status = "running";
                elementSE.StartTime = DateTime.Now;
                await teRunTable.InsertOrReplaceAsync(elementSE.Id,gameId,elementSE);
            } else {
                listAllRTE.ForEach(async rte => {
                    if (rte.Status == "running" || rte.Status == "assigned") {
                        rte.StartTime = rte.StartTime.Value.AddSeconds(pauseInSeconds);
                        await teRunTable.InsertOrReplaceAsync(rte.Id,gameId,rte);
                    }

                });
            }
            return new OkObjectResult(await BuildGameStatus(gameId,gameTable,teTable,gameRunTable,teRunTable));
        }

        public static async Task<IActionResult> EndGame(string id, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            RunGameSE rgs = await gameRunTable.GetByIdAsync<RunGameSE>(id, "RunGame");
            if (rgs == null) {
                rgs =new RunGameSE() {
                    Id = id
                };
            }
            //if (rgs.RunnerClientId != null && rgs.RunnerClientId != clientRunnerId) {
            //    return new UnauthorizedResult();
            //}
            //rgs.RunnerClientId = clientRunnerId;
            rgs.Status = "stopped";
            await gameRunTable.InsertOrReplaceAsync(id,"RunGame", rgs);
            return new OkObjectResult(await BuildGameStatus(id,gameTable,teTable,gameRunTable,teRunTable));
        }
        public static async Task<IActionResult> PauseGame(string id, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            RunGameSE rgs = await gameRunTable.GetByIdAsync<RunGameSE>(id, "RunGame");
            Game game = await gameTable.GetByIdAsync<Game>(id,"Game");
            if (rgs == null) {
                rgs =new RunGameSE() {
                    Id = id
                };
            }
            //if (rgs.RunnerClientId != null && rgs.RunnerClientId != clientRunnerId) {
            //    return new UnauthorizedResult();
            //}
            //rgs.RunnerClientId = clientRunnerId;
            rgs.Status = "paused";
            if (rgs.StartAt.HasValue) {
                DateTime startAt = rgs.StartAt.Value;
                int mustRun = Convert.ToInt32(Math.Floor((DateTime.Now - startAt).TotalSeconds / game.Interval));
                rgs.RestartPoint =mustRun;
                rgs.StartAt = DateTime.Now;
            }  
            await gameRunTable.InsertOrReplaceAsync(id,"RunGame", rgs);
            return new OkObjectResult(await BuildGameStatus(id,gameTable,teTable,gameRunTable,teRunTable));
        }
        public static async Task<IActionResult> ResetGame(string id, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            RunGameSE rgs = await gameRunTable.GetByIdAsync<RunGameSE>(id, "RunGame");
            if (rgs == null) {
                rgs =new RunGameSE() {
                    Id = id
                };
            }
            //if (rgs.RunnerClientId != null && rgs.RunnerClientId != clientRunnerId) {
            //    return new UnauthorizedResult();
            //}
            //rgs.RunnerClientId = clientRunnerId;
            rgs.Status = "created";
            rgs.RestartPoint = 0;
            await gameRunTable.InsertOrReplaceAsync(id,"RunGame", rgs);
            List<RunTaskElementSE> rte = await teRunTable.GetAllAsync<RunTaskElementSE>(id);
            List<Task<TableResult>> jobs = rte.Select(rc => {
                rc.Status = "notstarted";
                rc.StartTime = null;
                return teRunTable.InsertOrReplaceAsync(rc.Id,id,rc); 
            }).ToList();
            Task.WaitAll(jobs.ToArray());
            return new OkObjectResult(await BuildGameStatus(id,gameTable,teTable,gameRunTable,teRunTable));
        }
        public static async Task<IActionResult> AssignTask(string id, string taskId, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            RunTaskElementSE rteSE = await teRunTable.GetByIdAsync<RunTaskElementSE>(taskId,id);
            rteSE.Status= "assigned";
            await teRunTable.InsertOrReplaceAsync(rteSE.Id, id, rteSE);
            return new OkObjectResult(await BuildGameStatus(id,gameTable,teTable,gameRunTable,teRunTable));
        }

        public static async Task<IActionResult> FinishTask(string id, string taskId, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            RunTaskElementSE rteSE = await teRunTable.GetByIdAsync<RunTaskElementSE>(taskId,id);
            rteSE.Status= "won";
            await teRunTable.InsertOrReplaceAsync(rteSE.Id, id, rteSE);
            return new OkObjectResult(await BuildGameStatus(id,gameTable,teTable,gameRunTable,teRunTable));
        }
        public static async Task<IActionResult> GameStatus(string gameId, string clientRunnerId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            return new OkObjectResult(await BuildGameStatus(gameId,gameTable,teTable,gameRunTable,teRunTable));
        }
        private static async Task<RunGame> BuildGameStatus (string gameId, CloudTable gameTable, CloudTable teTable, CloudTable gameRunTable, CloudTable teRunTable) {
            Game game = await gameTable.GetByIdAsync<Game>(gameId,"Game");
            List<TaskElement> elements = await teTable.GetAllAsync<TaskElement>(gameId);
            RunGameSE rgs = await gameRunTable.GetByIdAsync<RunGameSE>(gameId, "RunGame");
            if (rgs == null) {
                return new RunGame() {
                    Game = game,
                    Id = gameId,
                    Status = "notstarted",
                    CurrentPointsComputer = game.ComputerTeamPoints,
                    CurrentPointsPlayer = game.PlayerTeamPoints
                };
            }
            List<RunTaskElementSE> rte = await teRunTable.GetAllAsync<RunTaskElementSE>(gameId);
            if (rgs.Status == "running"){
                rte = UpdateRunTaskElements(game,rgs, rte,elements,teRunTable);
                
            } 
            List<RunTaskElement> listRTE = rte.Select(runTaskElementSE => runTaskElementSE.GetRunTaskElement(elements.Find(te=>te.Id == runTaskElementSE.Id))).OrderBy(e=>e.TaskElement.No).ToList();
            RunGame runGame =  rgs.GetRunGame(game,listRTE);
            if (rgs.Status == "running") {
                if (runGame.CurrentPointsComputer <= 0) {
                    rgs.Status = "won";
                    runGame.Status = "won";
                    await teRunTable.InsertOrReplaceAsync(rgs.Id, "RunGame", rgs);
                }
                if (runGame.CurrentPointsPlayer <=0 ) {
                    rgs.Status = "lost";
                    runGame.Status = "lost";
                    await teRunTable.InsertOrReplaceAsync(rgs.Id, "RunGame", rgs);
                }
            }
            return runGame;
        }
        private static List<RunTaskElementSE> UpdateRunTaskElements(Game game, RunGameSE rgs, List<RunTaskElementSE> rte, List<TaskElement> elements, CloudTable teRunTable) {
            if (!rgs.StartAt.HasValue) {
                return rte;
            }
            DateTime startAt = rgs.StartAt.Value;
            int mustRun = Convert.ToInt32(Math.Floor((DateTime.Now - startAt).TotalSeconds / game.Interval)) +rgs.RestartPoint;
            DateTime breakPoint = DateTime.Now.AddMinutes(game.TaskDuration *-1);
            return rte.Select(runTaskElement=>{
                TaskElement element = elements.Find(te=> te.Id == runTaskElement.Id);
                if (element == null) {
                    return runTaskElement;
                }
                if ((runTaskElement.Status == "running" || runTaskElement.Status == "assigned") && runTaskElement.StartTime.Value < breakPoint) {
                    runTaskElement.Status = "lost";
                    teRunTable.InsertOrReplaceAsync(runTaskElement.Id,rgs.Id,runTaskElement); 
                }
                if (runTaskElement.Status == "notstarted" && element.No <= mustRun ) {
                    runTaskElement.StartTime = DateTime.Now;
                    runTaskElement.Status = "running";
                    teRunTable.InsertOrReplaceAsync(runTaskElement.Id,rgs.Id,runTaskElement); 
                }
                return runTaskElement;
            }).ToList();
        }  
    }
}