using System.Net.Mail;
using System.Net;
using WAHShopBackend.Models;
using WAHShopBackend.Data;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using System.Globalization;

namespace WAHShopBackend.EmailF
{
    public class EmailService(IConfiguration configuration, IOptions<ProjectInfo> ProjectInfo, MyDbContext context)
    {
        private IConfiguration _configuration = configuration;
        private IOptions<ProjectInfo> _projectInfo = ProjectInfo;
        private MyDbContext _context = context;

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
            if (order == null || order.UserId <= 0 || order.OrderItems == null || order.OrderItems.Count == 0)
            {
                return false; // Invalid order data
            }

            try
            {
                // Set culture to German (Germany)
                var culture = new CultureInfo("de-DE"); 

                EmailRequest emailRequest = new();

                if (order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                {
                    return false; // User or email is not set
                }
                emailRequest.ToEmail = order.User.Email;

                emailRequest.Subject = "Bestellbestätigung - Bestellnummer: " + order.Id;

                var productListHtml = string.Join("", order.OrderItems.Select(item =>
                                                  $"<li>{item.Product?.Name_de} {item.UnitPrice.ToString("C",culture)} x {item.Quantity} – {(item.UnitPrice * item.Quantity).ToString("C",culture)}</li>"));



                emailRequest.Body += "Hello " + order.User?.UserName + ",<br><br>" +
                                       $"Vielen Dank für Ihre Bestellung bei {_projectInfo.Value.Name}<br><br>" +
                                       "Bestellungsnummer: <strong>" + order.Id + "</strong><br>" +
                                       "Bestelldatum: <strong>" + order.OrderDate.ToString("dd.MM.yyyy") + "</strong><br>";

                // Gesamtpreis mit oder ohne versandkosten
                double totalPrice = 0;
                if (order.ShippingProviders != null && order.ShippingProviderId > 0)
                {
                    totalPrice = order.TotalPrice + order.ShippingProviders.PublicShippingCost;
                }
                else
                {
                    totalPrice = order.TotalPrice;
                }

                // discount code
                if (order.DiscountCodeId != null && order.DiscountCode != null)
                {
                    double originalTotal = order.TotalPrice / (1 - (order.DiscountCode.DiscountPercentage / 100.0));
                    double discountValue = originalTotal - order.TotalPrice;

                    emailRequest.Body +=
                        "Preis vor Rabatt: <strong>" + originalTotal.ToString("C", culture) + "</strong><br>" +
                        "Rabattbetrag: <strong>" + discountValue.ToString("C", culture) + "</strong><br>" +
                        $"Rabattprozentsatz : <strong>{order.DiscountCode?.DiscountPercentage ?? 0}%</strong><br>" +
                        "Preis nach Rabatt: <strong>" + order.TotalPrice.ToString("C", culture) + "</strong><br>";

                    // versandkosten
                    if (order.ShippingProviders != null && order.ShippingProviderId > 0)
                    {
                        emailRequest.Body += "Versandkosten: <strong>" + order.ShippingProviders.PublicShippingCost.ToString("C", culture) + "</strong><br>";
                    }
                    emailRequest.Body += "Gesamtpreis nach Rabatt: <strong>" + totalPrice.ToString("C", culture) + "</strong><br>";
                }
                // discount category
                else if (order.DiscountCategoryId != null && order.DiscountCategory != null)
                {

                    double categoryitemsPrice = 0;
                    foreach (var item in order.OrderItems)
                    {
                        if (item.CategoryId == order.DiscountCategory.CategoriesId)
                        {
                            categoryitemsPrice += item.Quantity * item.UnitPrice;
                        }
                    }

                    double categoryDiscountValue = categoryitemsPrice * (order.DiscountCategory.DiscountPercentage / 100.0);

                    double originalTotal = order.TotalPrice + categoryDiscountValue;
                    // get category name
                    Categories? categories = _context.Categories.FirstOrDefault(c => c.Id == (order.DiscountCategory != null ? order.DiscountCategory.CategoriesId : 0));
                    string categoryName = null!;
                    if (categories != null && !string.IsNullOrEmpty(categories.Name_de))
                    {
                        categoryName = categories.Name_de.Trim();
                    }

                    emailRequest.Body +=
                        "Preis vor Rabatt: <strong>" + originalTotal.ToString("C", culture) + "</strong><br>" +
                        "Rabattkategorie: <strong>" + categoryName + "</strong><br>" +
                        "Rabattbetrag: <strong>" + categoryDiscountValue.ToString("C", culture) + "</strong><br>" +
                        $"Rabattprozentsatz : <strong>{order.DiscountCategory?.DiscountPercentage ?? 0}%</strong><br>" +
                        "Preis nach Rabatt: <strong>" + order.TotalPrice.ToString("C", culture) + "</strong><br>";

                    // versandkosten
                    if (order.ShippingProviders != null && order.ShippingProviderId > 0)
                    {
                        emailRequest.Body += "Versandkosten: <strong>" + order.ShippingProviders.PublicShippingCost.ToString("C", culture) + "</strong><br>";
                    }

                    emailRequest.Body += "Gesamtpreis nach Rabatt: <strong>" + totalPrice.ToString("C", culture) + "</strong><br>";
                }
                else
                {
                    // versandkosten
                    if (order.ShippingProviders != null && order.ShippingProviderId > 0)
                    {
                        emailRequest.Body += "Versandkosten: <strong>" + order.ShippingProviders.PublicShippingCost.ToString("C", culture) + "</strong><br>";
                        emailRequest.Body += "Preis: <strong>" + order.TotalPrice.ToString("C", culture) + "</strong><br><br>";
                    }
                    emailRequest.Body += "Gesamtpreis: <strong>" + totalPrice.ToString("C", culture) + "</strong><br><br>";
                }

                // orderitems
                emailRequest.Body += "<strong>Bestellte Produkte:</strong><br>" +
                                       "<ul>" +
                                       productListHtml +
                                       "</ul><br>";
                // address
                if (order.Address != null)
                {
                    emailRequest.Body += "Lieferadresse:<br>" +
                                         $"{order.Address.FirstName} {order.Address.LastName}<br>" +
                                         $"{order.Address.Street}<br>" +
                                         $"{order.Address.ZipCode} {order.Address.City}-{order.Address.Country} <br><br>";
                }
                // payment method
                if (order.PaymentMethod != null)
                {
                    emailRequest.Body += "Zahlungsmethode: <strong>" + order.PaymentMethod.Description_de + "</strong><br>";
                    if (order.PaymentMethod.BankTransferDetails != null)
                    {
                        emailRequest.Body += "Banküberweisung Details:<br>" +
                                             $"Kontoinhaber: {order.PaymentMethod.BankTransferDetails.AccountHolderName}<br>" +
                                             $"IBAN: {order.PaymentMethod.BankTransferDetails.IBAN}<br>" +
                                             $"BIC: {order.PaymentMethod.BankTransferDetails.BIC}<br>" +
                                             $"Bankname: {order.PaymentMethod.BankTransferDetails.BankName}<br>" +
                                             $"Betrag: <strong>{totalPrice.ToString("C", culture)} </strong><br>" +
                                             $"- Bitte geben Sie bei der Überweisung Ihre Bestellnummer <strong> ({order.Id}) </strong> als Verwendungszweck an.<br>" +
                                             "- Ihre Bestellung wird nach Zahlungseingang bearbeitet und versendet.<br>" +
                                             "- Die Zahlung sollte innerhalb von <strong> 5 Werktagen </strong> erfolgen, um Verzögerungen zu vermeiden.<br><br>";
                    }
                }

                // contact
                emailRequest.Body += "Bei Fragen stehen wir Ihnen jederzeit gerne zur Verfügung.<br><br>" +
                    "<strong>Kontaktdaten</strong><br>" +
                    $"E-Mail: {_projectInfo.Value.Email}<br>" +
                    $"Whatsapp: {_projectInfo.Value.Whatsapp}<br>" +
                    $"Anschrift: {_projectInfo.Value.Address}<br>";

                emailRequest.Body += "</ul><br>" +
                    "Wir werden Ihre Bestellung in Kürze bearbeiten.<br><br>" +
                                       $"Mit freundlichen Grüßen,<br>{_projectInfo.Value.Name} Team";

                await SendEmailAsync(emailRequest.ToEmail, emailRequest.Subject, emailRequest.Body);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ValidationResult> SendEmailConfirmationAsync(User user,string token)
        {
            try
            {
                string forntendUrl = _configuration["AppSettings:FrontendUrl"] ?? "";
                var confirmationLink = $"{forntendUrl}/confirmemail?userId={Uri.EscapeDataString(user.Id.ToString())}&token={Uri.EscapeDataString(token)}";

                EmailRequest emailRequest = new ();
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                {
                    return new ValidationResult { Result = false, Message = "Benutzer wurde nicht gefunden" }; // User or email is not set
                }
                emailRequest.ToEmail = user.Email;
                emailRequest.Subject = "Konto bestätigen";

                emailRequest.Body = "Bestätige deine E-Mail<br>"+
                                     $"Bitte klicke hier, um deine E-Mail zu bestätigen: <a href='{confirmationLink}'><strong>E-Mail bestätigen</strong></a>";


                await SendEmailAsync(emailRequest.ToEmail, emailRequest.Subject, emailRequest.Body);

                return new ValidationResult { Result = true, Message = "E-Mail-Bestätigung gesendet" };
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error sending email confirmation: {ex.Message}");
                return new ValidationResult { Result = false, Message = $"Fehler beim Senden der E-Mail-Bestätigung: {ex.Message}" };
            }
        }
        public async Task<ValidationResult> OrderStatusChanged(Order order)
        {
            try
            {
                if (order == null || order.UserId <= 0 || order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                {
                    return new ValidationResult { Result = false, Message = "Ungültige Bestelldaten oder Benutzerinformationen" };
                }
                EmailRequest emailRequest = new();
                emailRequest.ToEmail = order.User.Email;
                emailRequest.Subject = "Bestellstatus aktualisiert - Bestellnummer: " + order.Id;
                emailRequest.Body = $"Hallo {order.User.UserName},<br><br>" +
                                    $"Der Status Ihrer Bestellung mit der Bestellnummer <strong>{order.Id}</strong> wurde aktualisiert.<br>" +
                                    $"Neuer Status: <strong>{order.Status?.Status_de ?? "Fehler bei Status abholen"}</strong><br><br>" +
                                    "Vielen Dank für Ihre Bestellung!";
                await SendEmailAsync(emailRequest.ToEmail, emailRequest.Subject, emailRequest.Body);
                return new ValidationResult { Result = true, Message = "E-Mail-Benachrichtigung gesendet" };
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error sending order status update: {ex.Message}");
                return new ValidationResult { Result = false, Message = $"Fehler beim Senden der E-Mail-Benachrichtigung: {ex.Message}" };
            }
        }
    }
}
