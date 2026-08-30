using Azure.Messaging.ServiceBus;
using DevAssist.Application.Interfaces.Documents;
using DevAssist.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevAssist.Infrastructure.Documents.BackgroundIndexing;

/// <summary>
/// Durable indexing queue backed by Azure Service Bus.
/// Registered when ServiceBus:ConnectionString is set — otherwise the
/// in-memory DocumentIndexingQueue is used.
///
/// Messages are sent with a string body equal to the document ID (Guid).
/// The consumer receives with ReceiveMode.ReceiveAndDelete so no
/// explicit Complete/Abandon is required.
/// </summary>
public sealed class ServiceBusDocumentIndexingQueue : IDocumentIndexingQueue, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusReceiver _receiver;
    private readonly ILogger<ServiceBusDocumentIndexingQueue> _logger;

    public ServiceBusDocumentIndexingQueue(
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusDocumentIndexingQueue> logger)
    {
        _logger = logger;
        var settings = options.Value;
        _client = new ServiceBusClient(settings.ConnectionString);
        _sender = _client.CreateSender(settings.QueueName);
        _receiver = _client.CreateReceiver(settings.QueueName,
            new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });
    }

    public void Enqueue(Guid documentId)
    {
        // Fire-and-forget: send is async but the caller expects a sync interface.
        _ = SendAsync(documentId);
    }

    private async Task SendAsync(Guid documentId)
    {
        try
        {
            var message = new ServiceBusMessage(documentId.ToString());
            await _sender.SendMessageAsync(message);
            _logger.LogInformation("Enqueued document {DocumentId} to Service Bus.", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue document {DocumentId} to Service Bus.", documentId);
        }
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await _receiver.ReceiveMessageAsync(
                    maxWaitTime: TimeSpan.FromSeconds(5),
                    cancellationToken: cancellationToken);

                if (message is null) continue;

                if (Guid.TryParse(message.Body.ToString(), out var documentId))
                    return documentId;

                _logger.LogWarning("Received invalid document ID from Service Bus: {Body}", message.Body.ToString());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving message from Service Bus; retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Guid.Empty;
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _receiver.DisposeAsync();
        await _client.DisposeAsync();
    }
}
