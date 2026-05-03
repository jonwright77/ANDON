using AndonApp.Data.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;

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

    // ---- Public send methods ----

    public async Task SendIncidentOpenedAsync(Incident incident)
    {
        var subject = $"[ANDON][{incident.Severity}] {incident.ProductionLine.Name} – {incident.AndonCode.Code} opened";
        var (plain, html) = BuildOpenedBody(incident);
        await SendToRecipientsAsync(incident, subject, plain, html);
    }

    public async Task SendIncidentClosedAsync(Incident incident)
    {
        var subject = $"[ANDON][CLOSED] {incident.ProductionLine.Name} – {incident.AndonCode.Code} closed";
        var (plain, html) = BuildClosedBody(incident);
        await SendToRecipientsAsync(incident, subject, plain, html);
    }

    // ---- Body builders ----

    private static (string plain, string html) BuildOpenedBody(Incident incident)
    {
        var color = SeverityColor(incident.Severity);
        var severityLabel = incident.Severity.ToString();

        var rows = new List<(string Label, string Value)>
        {
            ("Production Line", incident.ProductionLine.Name),
            ("ANDON Code",      $"{incident.AndonCode.Code} – {incident.AndonCode.Name}"),
            ("Severity",        severityLabel),
            ("Status",          "OPEN"),
            ("Additional Info", incident.AdditionalInfo ?? "(none)"),
            ("Opened At",       $"{incident.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC"),
        };

        var plain = BuildPlainText("ANDON INCIDENT OPENED", rows);
        var html  = BuildHtml(
            bannerColor:   color,
            bannerEyebrow: $"{severityLabel} ALERT",
            bannerTitle:   "Incident Opened",
            rows:          rows);

        return (plain, html);
    }

    private static (string plain, string html) BuildClosedBody(Incident incident)
    {
        var duration = incident.ClosedAt.HasValue
            ? FormatDuration(incident.ClosedAt.Value - incident.CreatedAt)
            : "–";

        var rows = new List<(string Label, string Value)>
        {
            ("Production Line", incident.ProductionLine.Name),
            ("ANDON Code",      $"{incident.AndonCode.Code} – {incident.AndonCode.Name}"),
            ("Severity",        incident.Severity.ToString()),
            ("Status",          "CLOSED"),
            ("Additional Info", incident.AdditionalInfo ?? "(none)"),
            ("Opened At",       $"{incident.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC"),
            ("Closed At",       $"{incident.ClosedAt:yyyy-MM-dd HH:mm:ss} UTC"),
            ("Duration Open",   duration),
        };

        var plain = BuildPlainText("ANDON INCIDENT CLOSED", rows);
        var html  = BuildHtml(
            bannerColor:   "#1a7f1a",
            bannerEyebrow: "All Clear",
            bannerTitle:   "Incident Closed",
            rows:          rows);

        return (plain, html);
    }

    // ---- HTML / plain-text rendering ----

    private static string BuildHtml(
        string bannerColor,
        string bannerEyebrow,
        string bannerTitle,
        List<(string Label, string Value)> rows)
    {
        var rowsHtml = string.Join("\n", rows.Select((r, i) =>
        {
            var border = i < rows.Count - 1 ? "border-bottom:1px solid #e5e7eb;" : "";
            return $"""
                <tr>
                  <td style="padding:11px 0;{border}font-size:0.78rem;font-weight:700;
                             text-transform:uppercase;letter-spacing:0.07em;color:#6b7280;
                             width:38%;vertical-align:top;">{WebUtility.HtmlEncode(r.Label)}</td>
                  <td style="padding:11px 0;{border}font-size:0.95rem;font-weight:600;
                             color:#111827;vertical-align:top;">{WebUtility.HtmlEncode(r.Value)}</td>
                </tr>
            """;
        }));

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:36px 0;">
                <tr><td align="center">

                  <table width="600" cellpadding="0" cellspacing="0"
                         style="background:#ffffff;border-radius:8px;overflow:hidden;
                                box-shadow:0 2px 12px rgba(0,0,0,0.10);max-width:600px;width:100%;">

                    <!-- Banner -->
                    <tr>
                      <td style="background:{bannerColor};padding:36px 40px;text-align:center;">
                        <div style="font-size:0.78rem;font-weight:700;letter-spacing:0.18em;
                                    text-transform:uppercase;color:rgba(255,255,255,0.75);
                                    margin-bottom:10px;">{WebUtility.HtmlEncode(bannerEyebrow)}</div>
                        <div style="font-size:2.2rem;font-weight:800;color:#ffffff;line-height:1;
                                    letter-spacing:-0.01em;">{WebUtility.HtmlEncode(bannerTitle)}</div>
                      </td>
                    </tr>

                    <!-- Data table -->
                    <tr>
                      <td style="padding:32px 40px;">
                        <table width="100%" cellpadding="0" cellspacing="0">
                          {rowsHtml}
                        </table>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style="background:#f9fafb;padding:18px 40px;
                                 border-top:1px solid #e5e7eb;text-align:center;">
                        <p style="margin:0;font-size:0.78rem;color:#9ca3af;">
                          ANDON Incident Management System
                        </p>
                      </td>
                    </tr>

                  </table>

                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string BuildPlainText(string title, List<(string Label, string Value)> rows)
    {
        var lines = new List<string> { title, new string('=', title.Length), "" };
        foreach (var (label, value) in rows)
            lines.Add($"{label,-20}: {value}");
        lines.Add("");
        lines.Add("--");
        lines.Add("ANDON Incident Management System");
        return string.Join("\n", lines);
    }

    // ---- Helpers ----

    private static string SeverityColor(Severity severity) => severity switch
    {
        Severity.RED   => "#b00020",
        Severity.AMBER => "#b85c00",
        _              => "#1a7f1a"
    };

    private static string FormatDuration(TimeSpan d)
    {
        if (d.TotalMinutes < 1) return "Less than 1 minute";
        if (d.TotalHours < 1)
        {
            var m = (int)d.TotalMinutes;
            return $"{m} minute{(m != 1 ? "s" : "")}";
        }
        var h = (int)d.TotalHours;
        return d.Minutes > 0 ? $"{h}h {d.Minutes}m" : $"{h} hour{(h != 1 ? "s" : "")}";
    }

    // ---- Transport ----

    private async Task SendToRecipientsAsync(Incident incident, string subject, string plain, string html)
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
                string.Join(", ", recipients), subject, plain);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(EmailFrom));
            foreach (var email in recipients)
                message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { TextBody = plain, HtmlBody = html };
            message.Body = bodyBuilder.ToMessageBody();

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

    public async Task<(bool Success, string Message)> TestSmtpAsync(
        string host, int port, string? user, string? pass, string from, string to)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = "[ANDON] SMTP Configuration Test";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = $"This is a test email from the ANDON system.\n\nSent: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                HtmlBody = $"""
                    <!DOCTYPE html><html><body style="font-family:'Segoe UI',Arial,sans-serif;padding:32px;">
                    <h2 style="color:#1a7f1a;">&#10003; ANDON SMTP Test</h2>
                    <p>SMTP configuration is working correctly.</p>
                    <p style="color:#6b7280;font-size:0.85rem;">Sent: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    </body></html>
                    """
            };
            message.Body = bodyBuilder.ToMessageBody();

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
}
