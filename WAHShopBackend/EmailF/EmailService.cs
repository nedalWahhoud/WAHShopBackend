using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static WAHShopBackend.EmailF.IEmailService;
using WAHShopBackend.Models;
using WAHShopBackend.Data;

namespace WAHShopBackend.EmailF
{
    public class EmailService(IConfiguration configuration)
    {
        private IConfiguration _configuration = configuration;

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient(_configuration["Smtp:Host"]!)
            {
                Port = int.Parse(_configuration["Smtp:Port"]!),
                Credentials = new NetworkCredential(_configuration["Smtp:Username"]!, _configuration["Smtp:Password"]!),
                EnableSsl = true,
            };

            var fromAddress = _configuration["Smtp:From"];
            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                throw new ArgumentNullException(nameof(fromAddress), "The 'From' address cannot be null or empty.");
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task<bool> OrderConfirmation(Order order)
        {
            if (order == null || order.UserId <= 0 || order.OrderItems == null || !order.OrderItems.Any())
            {
                return false; // Invalid order data
            }

            try
            {
                EmailRequest emailRequest = new EmailRequest();

                if (order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                {
                    return false; // User or email is not set
                }
                emailRequest.ToEmail = order.User.Email;

                emailRequest.Subject = "Bestellbestätigung - Bestellnummer: " + order.Id;

                var productListHtml = string.Join("", order.OrderItems.Select(item =>
                                                  $"<li>{item.Product?.Name_de} x {item.Quantity} – {(item.Product!.SalePrice * item.Quantity).ToString("C")}</li>"));

                emailRequest.Body = "Hello " + order.User?.UserName + ",<br><br>" +
                                       "Vielen Dank für Ihre Bestellung!<br><br>" +
                                       "Bestellungsnummer: <strong>" + order.Id + "</strong><br>" +
                                       "Bestelldatum: <strong>" + order.OrderDate.ToString("dd.MM.yyyy") + "</strong><br>" +
                                       "Gesamtpreis: <strong>" + order.TotalPrice.ToString("C") + "</strong><br><br>" +

                                       "<strong>Bestellte Produkte:</strong><br>" +
                                       "<ul>" +
                                       productListHtml +
                                       "</ul><br>" +
                "Wir werden Ihre Bestellung in Kürze bearbeiten.<br><br>" +
                                       "Mit freundlichen Grüßen,<br>Syriana Team";
                await SendEmailAsync(emailRequest.ToEmail, emailRequest.Subject, emailRequest.Body);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
