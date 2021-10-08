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
        public List<TaskElement> Tasks {set;get;}
    }

    public class TaskElement {

    }
}