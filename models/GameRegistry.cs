using System.Collections.Generic;
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
        }
    }

    public class TaskElement {

    }
}