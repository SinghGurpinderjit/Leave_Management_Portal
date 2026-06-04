using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Common.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
// ========== JAEGER TRACING IMPORTS ===========
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Shared.Common.Messaging;

public class RabbitMQPublisher : IMessagePublisher, IDisposable
{
    // JAEGER: ActivitySource for creating custom spans for RabbitMQ message publishing
    private static readonly ActivitySource ActivitySource = new("rabbitmq.publisher");

    // JAEGER: Propagator for injecting trace context into message headers (links producer → consumer)
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly object _lock = new();

    public RabbitMQPublisher(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

        _connection = factory.CreateConnection();

        _channel = _connection.CreateModel();

        // Declare exchnage
        _channel.ExchangeDeclare(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _logger.LogInformation("RabbitMQ Publisher connected to {Host}:{Port}", _settings.HostName, _settings.Port);
    }

    public Task PublishAsync<T>(T message, string routingKey) where T : class
    {
        // ===== JAEGER: Create producer span for RabbitMQ publish operation ===== 
        // This span appears in Jaeger UI and links to the consumer span in LeaveManagementService and NotificationService using
        using var activity = ActivitySource.StartActivity("RabbitMQ.Publish", ActivityKind.Producer);

        // JAEGER: Add metadata tags to help filter/search traces in Jaeger UI
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", _settings.ExchangeName);
        activity?.SetTag("messaging.routing_key", routingKey);
        activity?.SetTag("messaging.protocol", "AMQP");

        var messageJson = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(messageJson);

        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        if (_settings.MessageTtlMilliseconds > 0)
            properties.Expiration = _settings.MessageTtlMilliseconds.ToString();

        // ===== JAEGER: Inject trace context into RabbitMQ message headers
        // ===== // This propagates the trace ID and span ID so the consumer can link to this producer span 
        // Result: You can see the full flow in Jaeger:
        properties.Headers ??= new Dictionary<string, object>();
        Propagator.Inject(new PropagationContext(activity?.Context ?? Activity.Current?.Context ?? default, Baggage.Current),
            properties.Headers,
            (headers, key, value) =>
            {
                headers[key] = value; // JAEGER: Adds traceparent and tracestate headers
            });

        activity?.SetTag("messaging.message_payload_size_bytes", body.Length); // JAEGER: Track message size

        lock (_lock)
        {
            _channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
        }

        _logger.LogInformation("Published message (TTL: {TtlHours} to exchange: {Exchange} with routing key: {RoutingKey})",
            _settings.MessageTtlMilliseconds, _settings.ExchangeName, routingKey);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }

}
