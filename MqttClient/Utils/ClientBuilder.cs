using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;

namespace MqttClient.Utils
{
    public class ClientBuilder
    {
        public string Topic { get; set; }
        public Action<string> MessageHandleFunc { get; set; }
        public ArrayList SubscribeResponse = new ArrayList();

        public MqttFactory mqttFactory = null;
        public IMqttClient mqttClient = null;
        public MqttClientOptions mqttClientOptions = null;

        class ResponseInfo
        {
            public string Payload { get; set; }
            public string Topic { get; set; }
            public string QoS { get; set; }
            public string Retained { get; set; }
        }

        public ClientBuilder(object[] configMqttClientOptions, string topic,
            Action<string> messageHandleFunc)
        {
            this.mqttFactory = (MqttFactory)configMqttClientOptions[0];
            this.mqttClient = (IMqttClient)configMqttClientOptions[1];
            this.mqttClientOptions = (MqttClientOptions)configMqttClientOptions[2];
            this.Topic = topic;
            this.MessageHandleFunc = messageHandleFunc;
        }
        
        public static MqttClientOptions CreateMqttClineConnection(string clientId, string borderAddress,
            string username, string password, double keepAlivePeriod)
        {
            var mqttClientOptions = new MqttClientOptionsBuilder().WithClientId(clientId)
                .WithTcpServer(borderAddress)
                .WithCredentials(username, password)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepAlivePeriod)).Build();

            return mqttClientOptions;
        }

        public async Task Subscribe_Single_Topic(string connectionName)
        {
            try
            {
                mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsyncFunction;

                if (!await mqttClient.TryPingAsync())
                {
                    MqttClientConnectResult connetionResult = null;
                    if (!ClientPool.IsConnectionExist(connectionName))
                    {
                        try
                        {
                            connetionResult = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                            var clientGuid = Guid.NewGuid().ToString("D");
                            ClientPool.ConnectionNameList.Add(connectionName, clientGuid);
                            ClientPool.ClientConnectionPool.Add($"{connectionName}_{clientGuid}", mqttClient);

                            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                                .WithTopicFilter(
                                    f => { f.WithTopic(this.Topic); })
                                .Build();
                            await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            ClientPool.ClientConnectionPool.Remove(
                                $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}");
                            ClientPool.ConnectionNameList.Remove(connectionName);
                            throw new MqttConnectingFailedException(
                                "Mqtt server connect failed, please check the network status.", e, connetionResult);
                        }
                    }
                    else
                    {
                        throw new Exception(
                            "connection name already exist, please rewrite the connection name and keep the connection name unique");
                    }
                }
            }
            finally
            {
                // await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public async Task Subscribe_Multiple_Topics(string connectionName, List<TopicObject> topicObjects)
        {
            try
            {
                mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsyncFunction;

                if (!await mqttClient.TryPingAsync())
                {
                    MqttClientConnectResult connetionResult = null;
                    if (!ClientPool.IsConnectionExist(connectionName))
                    {
                        try
                        {
                            connetionResult = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                            var clientGuid = Guid.NewGuid().ToString("D");
                            ClientPool.ConnectionNameList.Add(connectionName, clientGuid);
                            ClientPool.ClientConnectionPool.Add($"{connectionName}_{clientGuid}", mqttClient);

                            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder();
                            foreach (var topicObject in topicObjects)
                            {
                                mqttSubscribeOptions.WithTopicFilter(
                                    f => { f.WithTopic(topicObject.Topic.ToString()); });
                            }

                            await mqttClient.SubscribeAsync(mqttSubscribeOptions.Build(), CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            ClientPool.ClientConnectionPool.Remove(
                                $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}");
                            ClientPool.ConnectionNameList.Remove(connectionName);
                            throw new MqttConnectingFailedException(
                                "Mqtt server connect failed, please check the network status.", e, connetionResult);
                        }
                    }
                    else
                    {
                        throw new Exception(
                            "connection name already exist, please rewrite the connection name and keep the connection name unique");
                    }
                }
            }
            finally
            {
                // await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public static async Task DisconnectClient(string connectionName)
        {
            if (ClientPool.ClientConnectionPool.ContainsKey(
                    $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}"))
            {
                var client =
                    ClientPool.ClientConnectionPool[
                        $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}"];
                ClientPool.ClientConnectionPool.Remove(
                    $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}");
                ClientPool.ConnectionNameList.Remove(connectionName);
                await client.TryDisconnectAsync(0);
            }
            else
            {
                throw new MqttClientDisconnectedException(new Exception("Disconnected mqtt client failed."));
            }
        }

        private async Task<Task> ApplicationMessageReceivedAsyncFunction(MqttApplicationMessageReceivedEventArgs e)
        {
            if (e != null)
            {
                string msg = BytesToString(e.ApplicationMessage.PayloadSegment);
                string topic = e.ApplicationMessage.Topic;
                string qoS = e.ApplicationMessage.QualityOfServiceLevel.ToString();
                string retained = e.ApplicationMessage.Retain.ToString();
                MessageHandleFunc.Invoke(msg);
            }

            return Task.CompletedTask;
        }

        private string BytesToString(ArraySegment<byte> payload)
        {
            if (payload != null)
            {
                return Encoding.UTF8.GetString(payload.ToArray());
            }

            return null;
        }

        private static Task MakeSubscribeMultipleTopics(string[] topicsList)
        {
            var code = topicsList.Aggregate("var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()",
                (current, topic) => current + $".WithTopicFilter(f=>{{ f.WithTopic({topic});}}");

            code += ".build();";
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = false
            };
            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, code);
            if (results.Errors.HasErrors)
            {
                throw new Exception("Coed compilation failed!");
            }

            var compiledAssembly = results.CompiledAssembly;
            var method = compiledAssembly.GetType("GeneratedClass")?.GetMethod("GeneratedMethod");
            if (method != null) method.Invoke(null, null);
            return Task.CompletedTask;
        }
    }
}