using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using GrapeCity.Forguncy.Commands;
using GrapeCity.Forguncy.Plugin;
using MqttClient.Utils;
using MQTTnet;
using MQTTnet.Client;

namespace MqttClient
{
    [Icon("pack://application:,,,/MqttClient;component/Resources/anonymous.png")]
    [Category("MQTT客户端")]
    public class AnonymousVerifyOption : BaseMqttClientServerCommand, ICommandExecutableInServerSideAsync
    {
        private HttpClient _httpClient = new HttpClient();

        public async Task<ExecuteResult> ExecuteAsync(IServerCommandExecuteContext dataContext)
        {
            var connectionName = await dataContext.EvaluateFormulaAsync(ConnectionName);
            var brokerAddress = await dataContext.EvaluateFormulaAsync(BrokerAddress);
            var port = await dataContext.EvaluateFormulaAsync(Port);
            var topic = await dataContext.EvaluateFormulaAsync(Topic);
            var paramsList = new object[] { brokerAddress.ToString(), CheckPort(port) };
            var configMqttOptions = this.ConfigMqttClientOptions(paramsList);

            var clientBuilder = ClientBuilderFactory.CreateClientBuilder(configMqttOptions, topic, dataContext,
                CallbackServerCommandName, CallbackServerCommandParamName, _httpClient);

            try
            {
                if (IsSubMultipleTopics)
                {
                    await clientBuilder.Subscribe_Multiple_Topics(connectionName.ToString(), TopicObjects);
                }
                else
                {
                    await clientBuilder.Subscribe_Single_Topic(connectionName.ToString());
                }
            }
            catch (Exception e)
            {
                return new ExecuteResult() { ErrCode = 500, Message = e.ToString() };
            }

            dataContext.Parameters[OutParamaterName] = "Anonymous verify, status normal, subscribe successful.";
            return new ExecuteResult(){ ErrCode = 0, Message = "Subscribe successful!" };;
        }

        protected override object[] ConfigMqttClientOptions(params object[] paramsList)
        {
            var mqttFactory = new MqttFactory();
            var mqttClient = mqttFactory.CreateMqttClient();

            var mqttClientOptions =
                new MqttClientOptionsBuilder().WithClientId($"Forguncy_{Guid.NewGuid().ToString()}")
                    .WithTcpServer((string)paramsList[0], (int?)paramsList[1]).Build();

            return new object[]
            {
                mqttFactory,
                mqttClient,
                mqttClientOptions
            };
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(OutParamaterName)
                ? "MQTT_Client[匿名验证]"
                : $"MQTT_Client[匿名验证]返回执行结果到参数:{OutParamaterName}";
        }
    }
}