using AndonApp.Data.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AndonApp.Services;

public interface IEmailService
{
    Task SendIncidentOpenedAsync(Incident incident);
    Task SendIncidentClosedAsync(Incident incident);
    Task<(bool Success, string Message)> TestSmtpAsync(string host, int port, string? user, string? pass, string from, string to);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private string EmailMode => _config["EMAIL_MODE"] ?? "LogOnly";
    private string SmtpHost => _config["SMTP_HOST"] ?? "localhost";
    private int SmtpPort => int.TryParse(_config["SMTP_PORT"], out var p) ? p : 587;
    private string? SmtpUser => _config["SMTP_USER"];
    private string? SmtpPass => _config["SMTP_PASS"];
    private string EmailFrom => _config["EMAIL_FROM"] ?? "andon@example.com";

    public async Task SendIncidentOpenedAsync(Incident incident)
    {
        var subject = $"[ANDON][{incident.Severity}] {incident.ProductionLine.Name} – {incident.AndonCode.Code} opened";
        var body = BuildBody(incident, "OPENED");
        await SendToRecipientsAsync(incident, subject, body);
    }

    public async Task SendIncidentClosedAsync(Incident incident)
    {
        var subject = $"[ANDON][{incident.Severity}] {incident.ProductionLine.Name} – {incident.AndonCode.Code} closed";
        var body = BuildBody(incident, "CLOSED");
        await SendToRecipientsAsync(incident, subject, body);
    }

    private string BuildBody(Incident incident, string action)
    {
        return $"""
            ANDON Incident {action}
            ========================

            Production Line : {incident.ProductionLine.Name}
            ANDON Code      : {incident.AndonCode.Code} – {incident.AndonCode.Name}
            Severity        : {incident.Severity}
            Status          : {action}
            Additional Info : {incident.AdditionalInfo ?? "(none)"}

            Opened At       : {incident.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC
            {(incident.ClosedAt.HasValue ? $"Closed At       : {incident.ClosedAt:yyyy-MM-dd HH:mm:ss} UTC" : "")}

            --
            ANDON Incident Management System
            """;
    }

    public async Task<(bool Success, string Message)> TestSmtpAsync(
        string host, int port, string? user, string? pass, string from, string to)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = "[ANDON] SMTP Configuration Test";
            message.Body = new TextPart("plain")
            {
                Text = $"This is a test email from the ANDON system.\n\nSent: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable);
            if (!string.IsNullOrEmpty(user))
                await client.AuthenticateAsync(user, pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return (true, $"Test email sent successfully to {to}.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task SendToRecipientsAsync(Incident incident, string subject, string body)
    {
        var recipients = incident.AndonCode.Recipients.Select(r => r.Email).ToList();

        if (!recipients.Any())
        {
            _logger.LogWarning("No recipients configured for ANDON code {Code}", incident.AndonCode.Code);
            return;
        }

        if (EmailMode == "LogOnly")
        {
            _logger.LogInformation(
                "[EMAIL LOG] To: {To} | Subject: {Subject}\n{Body}",
                string.Join(", ", recipients), subject, body);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(EmailFrom));
            foreach (var email in recipients)
                message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTlsWhenAvailable);
            if (!string.IsNullOrEmpty(SmtpUser))
                await client.AuthenticateAsync(SmtpUser, SmtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent: {Subject} -> {Recipients}", subject, string.Join(", ", recipients));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email: {Subject}", subject);
        }
    }
}
