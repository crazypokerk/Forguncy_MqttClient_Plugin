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
using MQTTnet.Exceptions;

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
        public EncodingType EncodingType;

        class ResponseInfo
        {
            public string Payload { get; set; }
            public string Topic { get; set; }
            public string QoS { get; set; }
            public string Retained { get; set; }
        }

        public ClientBuilder(object[] configMqttClientOptions, string topic,
            Action<string> messageHandleFunc, EncodingType encodingType)
        {
            this.mqttFactory = (MqttFactory)configMqttClientOptions[0];
            this.mqttClient = (IMqttClient)configMqttClientOptions[1];
            this.mqttClientOptions = (MqttClientOptions)configMqttClientOptions[2];
            this.Topic = topic;
            this.MessageHandleFunc = messageHandleFunc;
            this.EncodingType = encodingType;
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
                            var clientGuid = Guid.NewGuid().ToString("D");
                            ClientPool.ConnectionNameList.Add(connectionName, clientGuid);
                            ClientPool.ClientConnectionPool.Add($"{connectionName}_{clientGuid}", mqttClient);
                            connetionResult = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                        }
                        catch (MqttCommunicationTimedOutException e)
                        {
                            ClientPool.ClientConnectionPool.Remove(
                                $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}");
                            ClientPool.ConnectionNameList.Remove(connectionName);

                            string customMessage = "MQTT客户端连接超时，请检查连接是否正常！";
                            Exception newExpection = new InvalidOperationException(customMessage, e);
                            throw newExpection;
                        }
                        catch (MqttConnectingFailedException e)
                        {
                            ClientPool.ClientConnectionPool.Remove(
                                $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}");
                            ClientPool.ConnectionNameList.Remove(connectionName);
                            
                            string customMessage = "MQTT客户端连接失败，请检查连接信息是否正确！";
                            Exception newExpection = new InvalidOperationException(customMessage, e);
                            throw newExpection;
                        }

                        try
                        {
                            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                                .WithTopicFilter(
                                    f => { f.WithTopic(this.Topic); })
                                .Build();
                            await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            string customMessage = "MQTT客户端主题订阅失败，请检查主题是否正常或正确！";
                            Exception newExpection = new InvalidOperationException(customMessage, e);
                            throw newExpection;
                        }
                    }
                    else
                    {
                        throw new Exception(
                            "连接名已存在，请重写连接名并保证连接名唯一！");
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
                            var clientGuid = Guid.NewGuid().ToString("D");
                            ClientPool.ConnectionNameList.Add(connectionName, clientGuid);
                            ClientPool.ClientConnectionPool.Add($"{connectionName}_{clientGuid}", mqttClient);
                            connetionResult = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                        }
                        catch (MqttCommunicationTimedOutException e)
                        {
                            ClientPool.ClientConnectionPool.Remove(
                                $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}");
                            ClientPool.ConnectionNameList.Remove(connectionName);

                            string customMessage = "MQTT客户端连接超时，请检查连接是否正常！";
                            Exception newExpection = new InvalidOperationException(customMessage, e);
                            throw newExpection;
                        }
                        catch (MqttConnectingFailedException e)
                        {
                            ClientPool.ClientConnectionPool.Remove(
                                $"{connectionName}_{ClientPool.ConnectionNameList[connectionName]}");
                            ClientPool.ConnectionNameList.Remove(connectionName);
                            
                            string customMessage = "MQTT客户端连接失败，请检查连接信息是否正确！";
                            Exception newExpection = new InvalidOperationException(customMessage, e);
                            throw newExpection;
                        }

                        try
                        {
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
                            string customMessage = "MQTT客户端主题订阅失败，请检查主题是否正常或正确！";
                            Exception newExpection = new InvalidOperationException(customMessage, e);
                            throw newExpection;
                        }
                    }
                    else
                    {
                        throw new Exception(
                            "连接名已存在，请重写连接名并保证连接名唯一！");
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
                throw new MqttClientDisconnectedException(new Exception("错误，断开连接失败！"));
            }
        }

        private async Task<Task> ApplicationMessageReceivedAsyncFunction(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                if (e != null)
                {
                    var msg = InputEncodingString(e.ApplicationMessage.PayloadSegment, EncodingType);
                    string topic = e.ApplicationMessage.Topic;
                    string qoS = e.ApplicationMessage.QualityOfServiceLevel.ToString();
                    string retained = e.ApplicationMessage.Retain.ToString();
                    MessageHandleFunc.Invoke(msg);
                }
            }
            catch (Exception excepetion)
            {
                string customMessage = "MQTT客户端主题订阅失败，请检查主题是否正常或正确！";
                Exception newExpection = new InvalidOperationException(customMessage, excepetion);
                throw newExpection;
            }

            return Task.CompletedTask;
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

        private static string InputEncodingString(ArraySegment<byte> payload, EncodingType encodingType)
        {
            string resultMsg,codingName = null;
            byte[] bytes = payload.ToArray();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (payload != null)
            {
                switch (encodingType)
                {
                    case EncodingType.Utf8:
                        resultMsg = Encoding.UTF8.GetString(bytes);
                        codingName = "utf-8";
                        break;
                    case EncodingType.Gbk:
                        Encoding gbkEncoding = Encoding.GetEncoding("GBK");
                        resultMsg = gbkEncoding.GetString(bytes);
                        codingName = "gbk";
                        break;
                    case EncodingType.Big5:
                        Encoding big5Encoding = Encoding.GetEncoding("Big5");
                        resultMsg = big5Encoding.GetString(bytes);
                        codingName = "big5";
                        break;
                    case EncodingType.Base64:
                        resultMsg = Convert.ToBase64String(bytes);
                        codingName = "base64";
                        break;
                    default:
                        throw new Exception("不支持的编码类型！");
                }

                return resultMsg;
            }

            return null;
        }
    }
}