using System;
using System.Text;
namespace EaglesJungscharen.ATG.Utils {
    public static class GameNumberUtil {
        public static string GenerateGameNumber() {
            return GenerateNumberSequence(3)+"-"+GenerateNumberSequence(3)+"-"+GenerateNumberSequence(3);
        }
        public static string GenerateSecureNumber() {
            return GenerateNumberSequence(2)+"-"+GenerateNumberSequence(5)+"-"+GenerateNumberSequence(4);
        }

        private static string GenerateNumberSequence(int lenght) {
            Random rand = new Random();
            StringBuilder sb = new StringBuilder("");
            for(int x=0; x<lenght;x++) {
                sb.Append(rand.Next(10));
            }
            return sb.ToString();
        } 
    }
}