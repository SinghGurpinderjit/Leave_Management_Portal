using Microsoft.Extensions.Options;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Common.Configuration;
using Shared.Common.Messages;
using Shared.Common.Models;
using Shared.Common.Models.LeaveManagementService;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Services.NotificationService.Extensions
{
    namespace NotificationService.Services
    {
        public class LeaveStatusUpdatedConsumer : BackgroundService
        {
            // JAEGER: ActivitySource for creating custom spans for RabbitMQ message consumption
            private static readonly ActivitySource ActivitySource = new("rabbitmq.consumer");

            // JAEGER: Propagator for extracting trace context from message headers (links producer → consumer)
            private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

            private readonly LeaveStatusUpdatedConsumerSettings _settings;
            private readonly ILogger<LeaveStatusUpdatedConsumer> _logger;
            private readonly IServiceProvider _serviceProvider;
            private IConnection? _connection;
            private IModel? _channel;

            public LeaveStatusUpdatedConsumer(
                IOptions<LeaveStatusUpdatedConsumerSettings> settings,
                ILogger<LeaveStatusUpdatedConsumer> logger,
                IServiceProvider serviceProvider)
            {
                _settings = settings.Value;
                _logger = logger;
                _serviceProvider = serviceProvider;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                InitializeRabbitMQ();

                _logger.LogInformation("Leave status updated consumer started. Listening to queue: {QueueName}", _settings.QueueName);

                var consumer = new EventingBasicConsumer(_channel);

                consumer.Received += (model, eventArgs) =>
                {

                    // ===== JAEGER: Extract trace context from RabbitMQ message headers =====
                    // This links the consumer span to the producer span (LeaveManagementService → NotificationService)
                    var parentContext = Propagator.Extract(default, eventArgs.BasicProperties.Headers, (headers, key) =>
                    {
                        if (headers != null && headers.TryGetValue(key, out var value))
                        {
                            return new[] { Encoding.UTF8.GetString((byte[])value) };
                        }
                        return Enumerable.Empty<string>();
                    });

                    // ===== JAEGER: Create consumer span for message processing =====
                    // This span appears in Jaeger UI linked to the OrderService's producer span
                    using var activity = ActivitySource.StartActivity(
                        "RabbitMQ.Consume",
                        ActivityKind.Consumer,
                        parentContext.ActivityContext);  // JAEGER: Links to producer span

                    // JAEGER: Add metadata tags to help filter/search traces in Jaeger UI
                    activity?.SetTag("messaging.system", "rabbitmq");
                    activity?.SetTag("messaging.destination", _settings.QueueName);
                    activity?.SetTag("messaging.operation", "receive");
                    activity?.SetTag("messaging.routing_key", eventArgs.RoutingKey);

                    Task.Delay(500).Wait();

                    try
                    {
                        var body = eventArgs.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        activity?.SetTag("messaging.message_payload_size_bytes", body.Length);  // JAEGER: Track message size

                        _logger.LogInformation("Message received from queue: {Message}", message);

                        var leaveStatusUpdatedMessage = JsonSerializer.Deserialize<LeaveNotificationMessage>(message);
                        //var leaveStatusUpdatedMessage = JsonSerializer.Deserialize<LeaveStatusUpdatedMessage>(message);

                        if (leaveStatusUpdatedMessage != null)
                        {
                            // JAEGER: Add business-specific tags for easier debugging
                            activity?.SetTag("leaverequest.id", leaveStatusUpdatedMessage.LeaveRequestId);
                            activity?.SetTag("leaverequest.user_id", leaveStatusUpdatedMessage.EmployeeId);

                            SendsNotificationAsync(leaveStatusUpdatedMessage).Wait();

                            // Acknowledge message
                            _channel!.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);

                        }

                        activity?.SetStatus(ActivityStatusCode.Ok);

                        _logger.LogInformation("Processed and acknowledged user leave status updated message for Employee: {EmployeeId}", leaveStatusUpdatedMessage?.EmployeeId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from queue");

                        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        activity?.SetTag("error.type", ex.GetType().FullName);
                        activity?.SetTag("error.message", ex.Message);
                        activity?.SetTag("error.stacktrace", ex.StackTrace);

                        _channel!.BasicNack(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _channel!.BasicConsume(
                    queue: _settings.QueueName,
                    autoAck: false,
                    consumer: consumer);

                await Task.CompletedTask;
            }

            public override Task StopAsync(CancellationToken cancellationToken)
            {
                _logger.LogInformation("Leave status updated consumer is stopping");

                _channel?.Close();
                _channel?.Dispose();

                _connection?.Close();
                _connection?.Dispose();

                return base.StopAsync(cancellationToken);
            }

            public override void Dispose()
            {
                _channel?.Dispose();
                _connection?.Dispose();
                base.Dispose();
            }

            #region Private Methods

            private async Task SendsNotificationAsync(LeaveNotificationMessage message)
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                string reason = message.Status == LeaveStatus.Rejected.ToString() ?
                        $" with reason: {message.ReviewReason}" : string.Empty;

                var recepients = message.NotificationType == LeaveNotificationType.LeaveApplied ?
                                    string.Join(",",
                                        new List<string>()
                                        {
                                            string.IsNullOrWhiteSpace(message.EmployeeEmail) ? message.EmployeeId.ToString() : message.EmployeeEmail,
                                            string.IsNullOrWhiteSpace(message.ManagerEmail) ? message.ManagerId.ToString(): message.ManagerEmail
                                        }
                                    )
                                    : message.EmployeeId.ToString();

                var message2 = $"""
                    Message: 
                    Your leave updates - type: {message.LeaveType}, status: {message.Status}
                    from: {message.StartDate} to: {message.EndDate} by Manager ID: {message?.ManagerId} {reason}
                """;

                var notificationMessage = new Notification
                {
                    EmployeeId = message.EmployeeId,
                    Type = NotificationType.Email,
                    Recepient = recepients,
                    Subject = $"Subject: Leave Status Update",
                    Message = message2
                };

                await notificationService.SendNotificationAsync(notificationMessage);
            }

            private void InitializeRabbitMQ()
            {
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

                // Declare exchange
                _channel.ExchangeDeclare(
                    exchange: _settings.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                // Declare queue with TTL (Time to Live) to prevent processing stale messages
                // This prevents issues like processing old messages after long service downtime
                var queueArgs = new Dictionary<string, object>();
                if (_settings.MessageTtlMilliseconds > 0)
                {
                    // Queue-level TTL: All messages expire after specified duration
                    // Message older than this are automatically deleted by RabbitMQ
                    queueArgs.TryAdd("x-message-ttl", _settings.MessageTtlMilliseconds);
                }

                _channel.QueueDeclare(
                    queue: _settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs);

                // Bind queue to exchange
                _channel.QueueBind(
                    queue: _settings.QueueName,
                    exchange: _settings.ExchangeName,
                    routingKey: _settings.RoutingKey);

                _logger.LogInformation("RabbitMQ consumer initialized with {TtlHours}h message TTL. Queue: {Queue}, Exchnage: {Exchange}, RoutingKey: {RoutingKey}",
                    _settings.MessageTtlMilliseconds / 3600000.0, _settings.QueueName, _settings.ExchangeName, _settings.RoutingKey);
            }
        }
    }
    #endregion
}
