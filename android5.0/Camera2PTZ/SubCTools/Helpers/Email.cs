//-----------------------------------------------------------------------
// <copyright file="Email.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Helpers
{
    using System.Collections.Generic;
    using System.Net.Mail;

    /// <summary>
    /// Class used to send emails.
    /// </summary>
    public class Email
    {
        /// <summary>
        /// SMTP Client.
        /// </summary>
        private readonly SmtpClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="Email"/> class used to send emails.
        /// </summary>
        public Email()
            : this(new MailAddress("subcsoftware@gmail.com", "SubC"), "SubC!#123")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Email"/> class used to send emails.
        /// </summary>
        /// <param name="fromAddress">Gmail Address to use to send the <see cref="Email"/>.</param>
        /// <param name="password">Login credentials for the <see cref="Email"/> account.</param>
        public Email(MailAddress fromAddress, string password)
        {
            From = fromAddress;

            client = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(fromAddress.Address, password),
            };
        }

        /// <summary>
        /// Gets the return <see cref="MailAddress"/> for the E-Mail.
        /// </summary>
        public MailAddress From
        {
            get;
        }

        // public string To
        // {
        //     get;
        // } = string.Empty;

        /// <summary>
        /// Sends a <see cref="Email"/>.
        /// </summary>
        /// <param name="title">The title for the <see cref="Email"/>.</param>
        /// <param name="version">The version.</param>
        /// <param name="referenceNumber">The reference number.</param>
        /// <param name="message">Message to send in the body of the <see cref="Email"/>.</param>
        public void Send(string title, string version, int referenceNumber, string message)
        {
            Send(title, version, referenceNumber, message, new Attachment[] { });
        }

        /// <summary>
        /// Sends an <see cref="Email"/> to a <see cref="MailAddress"/>.
        /// </summary>
        /// <param name="title">The title for the <see cref="Email"/>.</param>
        /// <param name="version">The Version.</param>
        /// <param name="referenceNumber">The reference number.</param>
        /// <param name="message">Message to send in the body of the <see cref="Email"/>.</param>
        /// <param name="attachments">Attachments to include in the <see cref="Email"/>.</param>
        public void Send(string title, string version, int referenceNumber, string message, IEnumerable<Attachment> attachments)
        {
            Send(new MailAddress("software@subccontrol.com", "SubC"), "Bug/Feature Request " + title + " " + version + " " + referenceNumber, message, attachments);
        }

        // public void Send(string subject, string message)
        // {
        //     Send(From, To, subject, message, new Attachment[] { });
        // }

        // public void Send(string from, string to, string subject, string message)
        // {
        //     Send(from, to, subject, message, new Attachment[] { });
        // }

        /// <summary>
        /// Sends an <see cref="Email"/> to a <see cref="MailAddress"/>.
        /// </summary>
        /// <param name="to"><see cref="MailAddress"/> to send the <see cref="Email"/> to.</param>
        /// <param name="subject">Subject for the <see cref="Email"/>.</param>
        /// <param name="message">Message to send in the body of the <see cref="Email"/>.</param>
        public void Send(MailAddress to, string subject, string message)
        {
            Send(to, subject, message, new Attachment[] { });
        }

        /// <summary>
        /// Sends an <see cref="Email"/> to a <see cref="MailAddress"/>.
        /// </summary>
        /// <param name="to"><see cref="MailAddress"/> to send the <see cref="Email"/> to.</param>
        /// <param name="subject">Subject for the <see cref="Email"/>.</param>
        /// <param name="message">Message to send in the body of the <see cref="Email"/>.</param>
        /// <param name="attachments">Attachments to include in the <see cref="Email"/>.</param>
        public void Send(MailAddress to, string subject, string message, IEnumerable<Attachment> attachments)
        {
            var mail = new MailMessage(From, to)
            {
                Subject = subject,
                Body = message,
            };

            foreach (var attachment in attachments)
            {
                mail.Attachments.Add(attachment);
            }

            client.SendAsync(mail, null);
        }
    }
}