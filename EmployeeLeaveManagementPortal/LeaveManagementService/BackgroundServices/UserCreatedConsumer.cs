using LeaveManagementService.Services.Contracts;
using Microsoft.Extensions.Options;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Shared.Common.Configuration;
using Shared.Common.Messages;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace LeaveManagementService.Services
{
    public class UserCreatedConsumer : BackgroundService
    {
        // JAEGER: ActivitySource for creating custom spans for RabbitMQ message consumption
        private static readonly ActivitySource ActivitySource = new("rabbitmq.consumer");

        // JAEGER: Propagator for extracting trace context from message headers (links producer → consumer)
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private readonly UserCreatedConsumerSettings _settings;
        private readonly ILogger<UserCreatedConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IConnection? _connection;
        private IModel? _channel;

        public UserCreatedConsumer(
            IOptions<UserCreatedConsumerSettings> settings,
            ILogger<UserCreatedConsumer> logger,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _settings = settings.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            InitializeRabbitMQ();

            _logger.LogInformation("User created consumer started. Listening to queue: {QueueName}", _settings.QueueName);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, eventArgs) =>
            {

                _logger.LogInformation("UserCreatedConsumer | message received");

                Task.Delay(500).Wait();

                // ===== JAEGER: Extract trace context from RabbitMQ message headers =====
                // This links the consumer span to the producer span (UserService → LeaveManagementService)
                var parentContext = Propagator.Extract(default, eventArgs.BasicProperties.Headers, (headers, key) =>
                {
                    if (headers != null && headers.TryGetValue(key, out var value))
                    {
                        return new[] { Encoding.UTF8.GetString((byte[])value) };
                    }
                    return Enumerable.Empty<string>();
                });

                //// ===== JAEGER: Create consumer span for message processing =====
                //// This span appears in Jaeger UI linked to the OrderService's producer span
                using var activity = ActivitySource.StartActivity(
                    "RabbitMQ.Consume",
                    ActivityKind.Consumer,
                    parentContext.ActivityContext);  // JAEGER: Links to producer span

                //// JAEGER: Add metadata tags to help filter/search traces in Jaeger UI
                activity?.SetTag("messaging.system", "rabbitmq");
                activity?.SetTag("messaging.destination", _settings.QueueName);
                activity?.SetTag("messaging.operation", "receive");
                activity?.SetTag("messaging.routing_key", eventArgs.RoutingKey);

                try
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    activity?.SetTag("messaging.message_payload_size_bytes", body.Length);  // JAEGER: Track message size

                    _logger.LogInformation("Message received from queue: {Message}", message);

                    var userCreatedMessage = JsonSerializer.Deserialize<UserCreatedMessage>(message);

                    if (userCreatedMessage != null)
                    {
                        // JAEGER: Add business-specific tags for easier debugging
                        activity?.SetTag("user.user_id", userCreatedMessage.EmployeeId);

                        AddDefaultLeaveBalanceForUser(userCreatedMessage).Wait();
                    }

                    _logger.LogInformation("UserCreatedConsumer | acknowledged user created message: {UserId}", userCreatedMessage?.EmployeeId);

                    // Acknowledge message
                    _channel!.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);

                    activity?.SetStatus(ActivityStatusCode.Ok);

                    _logger.LogInformation("Processed and acknowledged user created message for user: {UserId}", userCreatedMessage?.EmployeeId);
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

        private async Task AddDefaultLeaveBalanceForUser(UserCreatedMessage message)
        {
            using var scope = _serviceProvider.CreateScope();
            var leaveBalanceService = scope.ServiceProvider.GetRequiredService<ILeaveBalanceService>();
            var response = await leaveBalanceService.AddDefaultLeaveBalanceForUser(message.EmployeeId);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // send success notification to user
                Log.Information("Successfully added leave balance for {UserId}", message.EmployeeId);
            }
            else
            {
                // send failure notification to user
                Log.Warning("Failed to add leave balance for {UserId}, ErrorMessage: {Message}", message.EmployeeId, string.Join(',', response.Errors));
            }
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

            var queueArgs = new Dictionary<string, object> { };

            // Declare queue with TTL (Time to Live) to prevent processing stale messages
            // This prevents issues like processing old messages after long service downtime
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
