namespace EaglesJungscharen.ATG.Models {

    public class CheckGameRequest {
        public string Game {set;get;}
        public string Verification {set;get;}
    }

    public class GameLoginEvent {
        public string GameId {set;get;}
        public string Action {set;get;}
    }
}