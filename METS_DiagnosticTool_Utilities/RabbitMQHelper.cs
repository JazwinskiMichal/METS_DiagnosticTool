using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static METS_DiagnosticTool_Utilities.TwincatHelper;
using static METS_DiagnosticTool_Utilities.VariableConfigurationHelper;

namespace METS_DiagnosticTool_Utilities
{
    public class RabbitMQHelper
    {
        public enum RoutingKeysDictionary
        {
            checkPLCVarExistance = 0,
            plcVarConfigsRead = 1,
            plcVarConfigsSave = 2,
            deleteVarConfig = 3,
            checkDoesPLCVarConfigUsed = 4,
            triggerPLCVarConfig = 5
        }

        internal const string checkPLCVarExistance = "checkPLCVarExistance";
        internal const string plcVarConfigsRead = "plcVarConfigRead";
        internal const string plcVarConfigSave = "plcVarConfigSave";
        internal const string deleteVarConfig = "deleteVarConfig";
        internal const string checkDoesPLCVarConfigUsed = "checkDoesPLCVarConfigUsed";
        internal const string triggerPLCVarConfig = "triggerPLCVarConfig";

        public static string[] RoutingKeys = new string[] { checkPLCVarExistance, plcVarConfigsRead, plcVarConfigSave , deleteVarConfig, checkDoesPLCVarConfigUsed, triggerPLCVarConfig};

        public static RpcServer _rpcServer;
        private static RpcClient _rpcClient;

        public static void Purge()
        {
            ConnectionFactory factory = new ConnectionFactory();

            factory.HostName = "localhost";

            using (IConnection connection = factory.CreateConnection())
            {
                using (IModel channel = connection.CreateModel())
                {
                    channel.QueuePurge("metsDiagTool_rpcQueue");
                }
            }
        }

        /// <summary>
        /// Initialize Rabbit MQ RPC Server
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static RpcServer InitializeServer(G_ET_EndPoint endPoint = G_ET_EndPoint.DiagnosticToolCore)
        {
            RpcServer _return = null;

            try
            {
                _rpcServer = new RpcServer();

                _return = _rpcServer;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Rabbit MQ Server Initialization Error ", ex.Message),
                               Logger.logEvents.RabbitMQServerInitailiztionError, endPoint);
            }

            return _return;
        }

        /// <summary>
        /// Server Close Connection
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static bool CloseServerConnection(G_ET_EndPoint endPoint = G_ET_EndPoint.DiagnosticToolCore)
        {
            bool _return = false;

            try
            {
                if (_rpcServer != null)
                    _rpcServer.Close();

                _return = true;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Rabbit MQ Server Close Connection Error ", ex.Message),
                                               Logger.logEvents.RabbitMQServerCloseConnectionError, endPoint);
            }

            return _return;
        }

        /// <summary>
        /// Initialize Rabbit MQ RPC Client
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static RpcClient InitializeClient(G_ET_EndPoint endPoint = G_ET_EndPoint.DiagnosticToolCore)
        {
            RpcClient _return = null;

            try
            {
                _rpcClient = new RpcClient();

                _return = _rpcClient;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Rabbit MQ Client Initialization Error ", ex.Message),
                                               Logger.logEvents.RabbitMQClientInitailiztionError, endPoint);
            }

            return _return;
        }

        public static async Task<string> SendToServer_TriggerPLCVarConfig(string routingKey, string variableAddress, bool trigger, LoggingType loggingType, int pollingRefreshTime, bool recording)
        {
            if (_rpcClient != null)
            {
                string _message = string.Concat("VariableAddress$", variableAddress, ";Trigger$", trigger.ToString().ToLower(),
                                    ";LoggingType$", (int)loggingType, ";PollingRefreshTime$", pollingRefreshTime.ToString(), ";Recording$", recording.ToString());

                return await _rpcClient.CallAsync(routingKey, _message);
            }
            else
                return string.Empty;
        }

        public static async Task<string> SendToServer_CheckPLCVarExistance(string routingKey, string message)
        {
            if (_rpcClient != null)
                return await _rpcClient.CallAsync(routingKey, message);
            else
                return string.Empty;
        }

        public static async Task<string> SendToServer_SavePLCVarConfigs(string routingKey, string xmlFilePath, string variableAddress, LoggingType loggingType, int pollingRefreshTime, bool recording)
        {
            // Create here message string to send to server
            if(_rpcClient != null)
            {
                string _message = string.Concat("XMLFileFullPath$", xmlFilePath, ";VariableAddress$", variableAddress,
                                    ";LoggingType$", (int)loggingType, ";PollingRefreshTime$", pollingRefreshTime.ToString(), ";Recording$", recording.ToString());
                
                return await _rpcClient.CallAsync(routingKey, _message);
            }
            else
                return string.Empty;
        }

        public static async Task<string> SendToServer_ReadPLCVarConfigs(string routingKey, string xmlFilePath)
        {
            // Create here message string to send to server
            if (_rpcClient != null)
                return await _rpcClient.CallAsync(routingKey, xmlFilePath);
            else
                return string.Empty;
        }

        public static async Task<string> SendToServer_DeletePLCVarConfig(string routingKey, string plcVariableAddress)
        {
            // Create here message string to send to server
            if (_rpcClient != null)
                return await _rpcClient.CallAsync(routingKey, plcVariableAddress);
            else
                return string.Empty;
        }

        public static async Task<string> SendToServer_CheckDoesPLCVarConfigUsed(string routingKey, string plcVariableAddress)
        {
            // Create here message string to send to server
            if (_rpcClient != null)
                return await _rpcClient.CallAsync(routingKey, plcVariableAddress);
            else
                return string.Empty;
        }

        /// <summary>
        /// Client Close Connection
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static bool CloseClientConnection(G_ET_EndPoint endPoint = G_ET_EndPoint.DiagnosticToolCore)
        {
            bool _return = false;

            try
            {
                if (_rpcClient != null)
                    _rpcClient.Close();

                _return = true;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Rabbit MQ Client Close Connection Error ", ex.Message),
                                               Logger.logEvents.RabbitMQClientCloseConnectionError, endPoint);
            }

            return _return;
        }
    }

    public class RpcServer
    {
        private readonly ConnectionFactory factory;
        private readonly IConnection connection;
        private readonly IModel channel;

        public event EventHandler<string> PLCVariableConfigurationTriggered;

        internal RpcServer()
        {
            factory = new ConnectionFactory() { HostName = "localhost" };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(queue: "metsDiagTool_rpcQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.ExchangeDeclare(exchange: "metsDiagTool_directExchange", type: ExchangeType.Direct);
            channel.BasicQos(0, 1, false);
            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: "metsDiagTool_rpcQueue", autoAck: false, consumer: consumer);

            // Bind Channel to defined Routing Keys
            foreach (string routingKey in RabbitMQHelper.RoutingKeys)
            {
                channel.QueueBind(queue: "metsDiagTool_rpcQueue", exchange: "metsDiagTool_directExchange", routingKey: routingKey);
            }

            // Attch Received Event
            consumer.Received += (model, ea) =>
            {
                string response = null;

                byte[] body = ea.Body.ToArray();
                IBasicProperties props = ea.BasicProperties;
                IBasicProperties replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    string routingKey = ea.RoutingKey;
                    string message = Encoding.UTF8.GetString(body);

                    // Send appropiate response base on RoutingKey
                    switch (routingKey)
                    {
                        // Check Existance of the PLC
                        case RabbitMQHelper.checkPLCVarExistance:
                            response = CheckPLCVariableExistance(message).ToString();
                            break;

                        // Read PLC Var Configurations (all at once)
                        case RabbitMQHelper.plcVarConfigsRead:
                            response = ReadPLCVariableConfigs(message).ToString();
                            break;

                        // Save PLC Var Configuration (single Variable)
                        case RabbitMQHelper.plcVarConfigSave:
                            response = SavePLCVariableConfig(message).ToString();
                            break;

                        // Delete PLC Var Configuration (single Variable)
                        case RabbitMQHelper.deleteVarConfig:
                            response = DeletePLCVariableConfig(message).ToString();
                            break;

                        // Check does the PLC var Config been already used
                        case RabbitMQHelper.checkDoesPLCVarConfigUsed:
                            response = CheckDoesThePLCVariableBeenUsed(message).ToString();
                            break;

                        case RabbitMQHelper.triggerPLCVarConfig:
                            PLCVariableConfigurationTriggered?.Invoke(this, message);
                            // Here it's possible to add confirmation to Client did the Server received Configuration correctly or not
                            response = true.ToString();
                            break;

                        default:
                            response = false.ToString();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(Logger.logLevel.Error, string.Concat("Rabbit MQ Server Exception when trying to receive a Message from Client ", e.ToString()),
                       Logger.logEvents.RabbitMQServerInitailiztionError);
                }
                finally
                {
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };
        }

        public void Close()
        {
            connection.Close();
        }
    }

    public class RpcClient
    {
        public event EventHandler<string> PLCVariableLiveViewTriggered;

        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly EventingBasicConsumer consumer;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper =
                    new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        internal RpcClient()
        {
            ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            // declare a server-named queue
            replyQueueName = channel.QueueDeclare(queue: "").QueueName;
            consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<string> tcs))
                    return;

                byte[] body = ea.Body.ToArray();
                string response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
            };

            channel.BasicConsume(
              consumer: consumer,
              queue: replyQueueName,
              autoAck: true);
        }

        public bool LiveViewRequested(string ADSIp, string ADSPort, bool trigger, VariableConfig variableConfiguration)
        {
            // Method to receive Configuration Variable, decode it to a single string and fire up an event to LiveView View Model

            bool _return = false;

            if (!string.IsNullOrEmpty(variableConfiguration.variableAddress))
            {
                string message = string.Concat("ADSIp$", ADSIp, ";ADSPort$", ADSPort, ";VariableAddress$", variableConfiguration.variableAddress, ";Trigger$", trigger.ToString().ToLower(),
                                    ";LoggingType$", (int)variableConfiguration.loggingType, ";PollingRefreshTime$", variableConfiguration.pollingRefreshTime.ToString());

                PLCVariableLiveViewTriggered?.Invoke(this, message);

                _return = true;
            }

            return _return;
        }

        internal Task<string> CallAsync(string routingKey, string message = "", CancellationToken cancellationToken = default)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            string correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            callbackMapper.TryAdd(correlationId, tcs);

            channel.BasicPublish(
                exchange: "metsDiagTool_directExchange",
                routingKey: routingKey,
                basicProperties: props,
                body: messageBytes);

            cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out var tmp));
            return tcs.Task;
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
