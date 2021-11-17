using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
namespace EaglesJungscharen.ATG.Utils {
    public static class SendGridUtil{
        public static async Task SendGameMail(string target, string gameCode, string secureCode) {
            var apiKey = Environment.GetEnvironmentVariable("SEND_GRID_API_KEY");
            var senderMail = Environment.GetEnvironmentVariable("SENDER_MAIL");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(senderMail, "Against The Game");
            var subject = "Against The Game Registration";
            var to = new EmailAddress(target);
            var plainTextContent = "Game Code: "+gameCode +"\nSecure Code: "+secureCode;
            var htmlContent = "Game Code: <strong>"+gameCode+"</strong><br>Secure Code: <strong>"+secureCode+"</strong>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }
}