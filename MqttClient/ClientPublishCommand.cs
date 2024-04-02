using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using GrapeCity.Forguncy.Commands;
using GrapeCity.Forguncy.Plugin;
using MqttClient.Utils;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;

namespace MqttClient
{
    [Icon("pack://application:,,,/MqttClient;component/Resources/publish.png")]
    [Category("MQTT客户端")]
    public class ClientPublishCommand : BaseMqttClientServerCommand, ICommandExecutableInServerSideAsync
    {
        [FormulaProperty]
        [Description("注意：需要在连接成功后方可发布消息至对应主题")]
        [DisplayName("发送的消息")]
        [Required]
        [OrderWeight(5)]
        public object Payload { get; set; }

        [FormulaProperty]
        [DisplayName("发送主题")]
        [Required]
        [OrderWeight(6)]
        public object SendTopic { get; set; }

        [DisplayName("是否为对象需要序列化Json字符串")]
        [OrderWeight(7)]
        public bool IsNeedSerializeJsonData { get; set; }

        protected override object[] ConfigMqttClientOptions(params object[] paramsList)
        {
            throw new NotImplementedException();
        }

        // [DisplayName("链接保持时间")]
        // [DoubleProperty(Min = 10, Max = 1000, AllowNull = false)]
        // [OrderWeight(8)]
        // public double KeepAlive { get; set; }

        public async Task<ExecuteResult> ExecuteAsync(IServerCommandExecuteContext dataContext)
        {
            try
            {
                var connectionName = await dataContext.EvaluateFormulaAsync(ConnectionName);
                // var brokerAddress = await dataContext.EvaluateFormulaAsync(BrokerAddress);
                // var username = await dataContext.EvaluateFormulaAsync(Username);
                // var password = await dataContext.EvaluateFormulaAsync(Password);
                var payload = await dataContext.EvaluateFormulaAsync(Payload);
                var sendTopic = await dataContext.EvaluateFormulaAsync(SendTopic);
                var isNeedSerializeJsonData = IsNeedSerializeJsonData;
                // var keepAlive = KeepAlive;

                if (isNeedSerializeJsonData)
                {
                    payload = JsonConvert.SerializeObject(payload);
                }

                var isExistConneciton = ClientPool.IsConnectionExist(connectionName.ToString());

                if (isExistConneciton)
                {
                    var thisMqttConnection = ClientPool.GetMqttClient(connectionName.ToString());
                    var thisMqttConnectionIsConnected = ClientPool.IsConnected(thisMqttConnection);
                    await Publish_Application_Message(thisMqttConnectionIsConnected ? thisMqttConnection : null,
                        sendTopic, payload.ToString());
                }
                else
                {
                    throw new MqttClientDisconnectedException(
                        new Exception("there is no exist connection, please connect mqtt server first!"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            // else
            // {
            //     var mqttClientOptions = ClientBuilder.CreateMqttClineConnection($"Forguncy_{Guid.NewGuid().ToString()}",
            //         brokerAddress.ToString(), username.ToString(), password.ToString(), keepAlive);
            // }
            return new ExecuteResult() { ErrCode = 0, Message = "Send success!" };
        }

        public static async Task Publish_Application_Message(IMqttClient mqttClient, object sendTopic, string payload)
        {
            /*
             * This sample pushes a simple application message including a topic and a payload.
             *
             * Always use builders where they exist. Builders (in this project) are designed to be
             * backward compatible. Creating an _MqttApplicationMessage_ via its constructor is also
             * supported but the class might change often in future releases where the builder does not
             * or at least provides backward compatibility where possible.
             */
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(sendTopic.ToString())
                .WithPayload(payload)
                .Build();
            if (mqttClient != null)
            {
                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            }
            else
            {
                var mqttFactory = new MqttFactory();
                var newMqttClient = mqttFactory.CreateMqttClient();
                await newMqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            }
        }

        public static async Task Publish_Multiple_Application_Messages()
        {
            /*
             * This sample pushes multiple simple application message including a topic and a payload.
             *
             * See sample _Publish_Application_Message_ for more details.
             */

            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("broker.hivemq.com")
                    .Build();

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("samples/temperature/living_room")
                    .WithPayload("19.5")
                    .Build();

                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

                applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("samples/temperature/living_room")
                    .WithPayload("20.0")
                    .Build();

                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

                applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("samples/temperature/living_room")
                    .WithPayload("21.0")
                    .Build();

                await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

                await mqttClient.DisconnectAsync();
            }
        }

        public override bool GetDesignerPropertyVisible(string propertyName, CommandScope commandScope)
        {
            switch (propertyName)
            {
                case nameof(BrokerAddress):
                    return false;
                case nameof(Port):
                    return false;
                case nameof(Topic):
                    return false;
                case nameof(CallbackServerCommandName):
                    return false;
                case nameof(CallbackServerCommandParamName):
                    return false;
                case nameof(IsSubMultipleTopics):
                    return false;
                case nameof(EncodingType):
                    return false;
                default:
                    return base.GetDesignerPropertyVisible(propertyName, commandScope);
            }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(OutParamaterName)
                ? "MQTT_Client[发布消息]"
                : $"MQTT_Client[发布消息]返回执行结果到参数:{OutParamaterName}";
        }
    }
}