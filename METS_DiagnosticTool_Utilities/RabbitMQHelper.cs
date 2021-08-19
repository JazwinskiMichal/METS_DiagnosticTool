﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static METS_DiagnosticTool_Utilities.TwincatHelper;

namespace METS_DiagnosticTool_Utilities
{
    public class RabbitMQHelper
    {
        internal const string checkPLCVarExistance = "checkPLCVarExistance";

        public static string[] RoutingKeys = new string[] { checkPLCVarExistance };

        private static RpcServer _rpcServer;
        private static RpcClient _rpcClient;

        /// <summary>
        /// Initialize Rabbit MQ RPC Server
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static bool InitializeServer(G_ET_EndPoint endPoint = G_ET_EndPoint.DiagnosticToolCore)
        {
            bool _return = false;

            try
            {
                _rpcServer = new RpcServer();

                _return = true;
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
        public static bool InitializeClient(G_ET_EndPoint endPoint = G_ET_EndPoint.DiagnosticToolCore)
        {
            bool _return = false;

            try
            {
                _rpcClient = new RpcClient();

                _return = true;
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.logLevel.Error, string.Concat("Rabbit MQ Client Initialization Error ", ex.Message),
                                               Logger.logEvents.RabbitMQClientInitailiztionError, endPoint);
            }

            return _return;
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

    internal class RpcServer
    {
        private readonly ConnectionFactory factory;
        private readonly IConnection connection;
        private readonly IModel channel;

        internal RpcServer()
        {
            factory = new ConnectionFactory() { HostName = "localhost" };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(queue: "metsDiagTool_rpcQueue", durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.BasicQos(0, 1, false);
            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: "metsDiagTool_rpcQueue", autoAck: false, consumer: consumer);

            // Bind Channel to defined Routing Keys
            foreach (string routingKey in RabbitMQHelper.RoutingKeys)
            {
                channel.QueueBind(queue: "metsDiagTool_rpcQueue", exchange: "direct_logs", routingKey: routingKey);
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
                        case RabbitMQHelper.checkPLCVarExistance:
                            // TO DO Here Actual code to make check for PLC Variable Existance
                            break;
                        default:
                            break;
                    }

                    int n = int.Parse(message);
                    //Console.WriteLine(" [.] received({0})", message);
                    response = n.ToString();
                }
                catch (Exception e)
                {
                    Logger.Log(Logger.logLevel.Error, string.Concat("Rabbit MQ Server Exception when trying to receive a Message from Client ", e.Message),
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

    internal class RpcClient
    {
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

        internal Task<string> CallAsync(string routingKey, string message, CancellationToken cancellationToken = default)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            string correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            callbackMapper.TryAdd(correlationId, tcs);

            channel.BasicPublish(
                exchange: "direct_logs",
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