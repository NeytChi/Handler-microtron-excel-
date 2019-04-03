using System;
using MimeKit;
using MailKit;
using System.IO;
using MailKit.Net.Smtp;
using MailKit.Net.Imap;
using Exceling.NDatabase;
using System.Collections.Generic;

namespace Exceling.Functional.Mail
{
    public class MailF
    {
        private MailboxAddress hostMail;
        private string ip = "127.0.0.1";
        private Database database;
        private LogProgram logger;
        private List<string> listEmails = new List<string>();
        private string mailAddress = "";
        private string mailPassword = "";
        private readonly string SMTPGmailServer = "smtp.gmail.com";
        private readonly int SMTPGmailPort = 25;
        private readonly string IMAPGmailServer = "mail.yobibyte.in.ua";
        private readonly int IMAPGmailPort = 993;
        private readonly string SenderEmail = "Microtron";

        public MailF(Database callDB, LogProgram logProgram)
        {
            this.database = callDB;
            this.logger = logProgram;
            Config config = new Config();
            this.ip = config.IP;
            this.mailAddress = config.GetConfigValue("email_receive", "string");
            if (this.mailAddress == "price@yobibyte.in.ua")
            {
                this.mailPassword = "K22ryMugL";
            }
            else
            {
                this.mailPassword = config.GetConfigValue("email_receive_password", "string");
            }
            SenderEmail = config.GetConfigValue("sender_email_name", "string");
            hostMail = new MailboxAddress(ip, mailAddress);
        }
        public void SendEmailAsync(string emailAddress, string subject, string message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(hostMail);
            emailMessage.To.Add(new MailboxAddress(emailAddress));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };
            try
            {
                using (var client = new SmtpClient())
                {
                    client.ConnectAsync(SMTPGmailServer, SMTPGmailPort, false);
                    client.AuthenticateAsync(hostMail.Address, mailPassword);
                    client.SendAsync(emailMessage);
                    client.DisconnectAsync(true);
                    logger.WriteLog(string.Format("Send message to {0}", emailAddress), LogLevel.Mail);
                }
            }
            catch (Exception)
            {
                logger.WriteLog("Error SendEmailAsync", LogLevel.Error);
            }
        }
        public List<string> ReceivedFileImap(string SaveToDirectory)
        {
            List<string> file_names = new List<string>();
            using (ImapClient client = new ImapClient())
            {
                client.Connect(IMAPGmailServer, IMAPGmailPort, true);
                client.Authenticate(mailAddress, mailPassword);
                IMailFolder inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadOnly);
                for (int i = 0; i < inbox.Count; i++)
                {
                    MimeMessage message = inbox.GetMessage(i);
                    if (message.Date.Month == DateTime.Now.Month && message.Date.Day == DateTime.Now.Day && message.Date.Year == DateTime.Now.Year)
                    {
                        IEnumerable<MimeEntity> attachments = message.Attachments;
                        foreach (MimeEntity file in attachments)
                        {
                            file_names.Add(SaveToDirectory + file.ContentType.Name);
                            GetFileImap(file, SaveToDirectory);
                        }
                    }
                }
            }
            logger.WriteLog("Get archives from email", LogLevel.Mail);
            return file_names;
        }
        private void GetFileImap(MimeEntity MessageFile, string SaveToDirectory)
        {
            if (MessageFile.ContentDisposition.Disposition == "attachment")
            {
                using (var stream = File.Create(SaveToDirectory + MessageFile.ContentType.Name))
                {
                    if (MessageFile is MessagePart)
                    {
                        var rfc822 = (MessagePart)MessageFile;
                        rfc822.Message.WriteTo(stream);
                    }
                    else
                    {
                        var part = (MimePart)MessageFile;
                        part.Content.DecodeTo(stream);
                    }
                }
            }
            logger.WriteLog("Get archive from email", LogLevel.Mail);
        }
    }
}

