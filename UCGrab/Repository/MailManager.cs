using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace UCGrab.Repository
{
    public class MailManager
    {
        private String MailSender { get; set; }
        private String MailAppPassword { get; set; }
        public MailManager()
        {
            MailSender = ConfigurationManager.AppSettings["MailSender"];
            MailAppPassword = ConfigurationManager.AppSettings["MailSenderAppPassword"];
        }
        public bool SendEmail(string szRecipient, string subject, string szMsgBody, ref string errResponse)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    var smtp = new SmtpClient();
                    message.From = new MailAddress(MailSender);
                    message.To.Add(new MailAddress(szRecipient));
                    message.Subject = subject;
                    message.IsBodyHtml = true; //to make message body as html
                    message.Body = szMsgBody;
                    smtp.Port = 587;
                    smtp.Host = "smtp.gmail.com"; //for gmail host
                    smtp.EnableSsl = true;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(MailSender, MailAppPassword);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Send(message);
                    //
                    errResponse = "Message Sent";
                    //
                    return true;

                }
            }
            catch (Exception ex)
            {
                errResponse = ex.Message;
                //
                return false;
            }
        }
    }
}