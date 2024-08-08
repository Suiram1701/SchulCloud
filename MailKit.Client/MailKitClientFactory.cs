using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace MailKit.Client;

/// <summary>
/// A factory for creating <see cref="ISmtpClient"/> instances.
/// </summary>
public sealed class MailKitClientFactory(ILogger<MailKitClientFactory> logger, MailKitClientSettings settings) : IDisposable
{
    private readonly ILogger _logger = logger;
    private readonly MailKitClientSettings _settings = settings;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Timer? _keepAliveTimer = settings.KeepAliveIntervall is not null
        ? new Timer(settings.KeepAliveIntervall.Value)
        : null;
    private SmtpClient? _client;

    /// <summary>
    /// Gets a connected and authenticated <see cref="ISmtpClient"/> instance.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created client.</returns>
    public async Task<ISmtpClient> GetSmtpClientAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);

        try
        {
            _client ??= new SmtpClient();

            if (!_client.IsConnected)
            {
                await _client.ConnectAsync(_settings.Endpoint, ct);
                _logger.LogDebug("SMTP client connected successful to the remote host '{host}'.", _settings.Endpoint);

                if (_settings.Credentials is not null)
                {
                    await _client.AuthenticateAsync(_settings.Credentials, ct);
                    _logger.LogDebug("SMTP client authentication successful to remote host '{host}'.", _settings.Endpoint);
                }

                if (_keepAliveTimer is not null)
                {
                    _keepAliveTimer.Start();
                    _keepAliveTimer.Elapsed += KeepAliveTimer_ElapsedAsync;
                    _client.MessageSent += Client_MessageSent;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred while connecting to the remote host '{host}'.", _settings.Endpoint);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }

        return _client;
    }

    private void Client_MessageSent(object? sender, MessageSentEventArgs e)
    {
        // Reset the timer to minimize the requests.
        if (_keepAliveTimer?.Enabled ?? false)
        {
            _keepAliveTimer.Stop();
            _keepAliveTimer.Start();
        }
    }

    private async void KeepAliveTimer_ElapsedAsync(object? sender, ElapsedEventArgs e)
    {
        if (_client?.IsConnected ?? false)
        {
            try
            {
                await _client.NoOpAsync();
                _logger.LogDebug("Remote host '{host}' pinged to keep the connection alive.", _settings.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while refreshing the connection to the remote host '{host}'.", _settings.Endpoint);
            }
        }
    }

    public void Dispose()
    {
        if (_keepAliveTimer is not null)
        {
            _keepAliveTimer.Elapsed -= KeepAliveTimer_ElapsedAsync;
            _keepAliveTimer.Dispose();
        }

        if (_client is not null)
        {
            _client.MessageSent -= Client_MessageSent;
            _client.Disconnect(true);
            _client.Dispose();
        }

        _semaphore?.Dispose();
    }
}
