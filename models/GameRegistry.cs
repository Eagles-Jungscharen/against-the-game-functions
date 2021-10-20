using System.Collections.Generic;
using System.Linq;
using System;
namespace EaglesJungscharen.ATG.Models{
    public class GameRegistry {
        public string Code {set;get;}
        public string Verification {set;get;}
        public string GameId {set;get;}
        public string EMail {set;get;}
    }

    public class Game {
        public string Id {set;get;}
        public string Name {set;get;}
        public string Number {set;get;}
        public string PlayerTeamName {set;get;}
        public string ComputerTeamName {set;get;}
        public int TaskPoints {set;get;}
        public int TaskDuration {set;get;}
        public int PlayerTeamPoints {set;get;}
        public int ComputerTeamPoints {set;get;}
        public int Interval {set;get;}
        public List<TaskElement> Tasks {set;get;}
        public void Update(Game inputGame) {
            this.ComputerTeamName = inputGame.ComputerTeamName;
            this.ComputerTeamPoints = inputGame.ComputerTeamPoints;
            this.Interval = inputGame.Interval;
            this.Name = inputGame.Name;
            this.PlayerTeamName = inputGame.PlayerTeamName;
            this.PlayerTeamPoints = inputGame.PlayerTeamPoints;
            this.TaskPoints = inputGame.TaskPoints;
            this.TaskDuration = inputGame.TaskDuration;
        }
    }

    public class TaskElement {
        public string Id {set;get;}
        public int No {set;get;}
        public string Name {set;get;}
        public bool Started {set;get;}
        public bool Done {set;get;}
        public DateTime? EndTime {set;get;}
    }

    public class RunGame {
        public string Id {set;get;}
        public Game Game {set;get;}
        public string Status {set;get;}
        public DateTime? StartAt {set;get;}
        public string RunnerClientId {set;get;}
        public int CurrentPointsPlayer {set;get;}
        public int CurrentPointsComputer {set;get;}
        public List<RunTaskElement> Tasks {set;get;}
    }

    public class RunGameSE {
        public string Id {set;get;}
        public string Status {set;get;}
        public DateTime? StartAt {set;get;}
        public DateTime? LastTaskAt {set;get;}
        public string RunnerClientId {set;get;}
        public int RestartPoint {set;get;}
        public void Update(RunGame rg, DateTime lastTaskAt) {
            this.Status = rg.Status;
            this.StartAt = rg.StartAt;
            this.LastTaskAt = lastTaskAt;
        }
        public RunGame GetRunGame(Game game, List<RunTaskElement> taskElements) {
            int pointComputer = taskElements.Sum(te=>te.Status == "lost"?game.TaskPoints:0);
            int pointPlayer = taskElements.Sum(te=>te.Status == "won"?game.TaskPoints:0);
            
            return new RunGame() {
                Id = this.Id,
                Game = game,
                StartAt = this.StartAt,
                Status = this.Status,
                RunnerClientId = this.RunnerClientId,
                CurrentPointsComputer = game.ComputerTeamPoints - pointPlayer,
                CurrentPointsPlayer = game.PlayerTeamPoints - pointComputer,
                Tasks = taskElements
            };
        }
    }

    public class RunTaskElement {
        public string Id {set;get;}
        public TaskElement TaskElement {set;get;}
        public string Status {set;get;}
        public DateTime? StartTime {set;get;}

    }

    public class RunTaskElementSE {
        public string Id {set;get;}
        public string Status {set;get;}
        public DateTime? StartTime {set;get;}
        public void Update(RunTaskElement rte) {
            this.Status = rte.Status;
            this.StartTime = rte.StartTime;
        }
        public RunTaskElement GetRunTaskElement(TaskElement te) {
            return new RunTaskElement() {
                Id = this.Id,
                TaskElement = te,
                Status = this.Status,
                StartTime = this.StartTime
            };
        }
    }

}