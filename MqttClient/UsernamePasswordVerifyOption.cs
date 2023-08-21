using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Threading.Tasks;
using GrapeCity.Forguncy.Commands;
using GrapeCity.Forguncy.Plugin;
using MqttClient.Utils;
using MQTTnet;
using MQTTnet.Client;

namespace MqttClient
{
    [Icon("pack://application:,,,/MqttClient;component/Resources/usernamepwd.png")]
    [Category("MQTT客户端")]
    public class UsernamePasswordVerifyOption : BaseMqttClientServerCommand, ICommandExecutableInServerSideAsync
    {
        private HttpClient _httpClient = new HttpClient();

        [FormulaProperty]
        [DisplayName("登录用户名")]
        [Required]
        [OrderWeight(11)]
        public string Username { get; set; }

        [FormulaProperty]
        [DisplayName("密码")]
        [Required]
        [OrderWeight(22)]
        public string Password { get; set; }

        [DisplayName("是否开启SSL")]
        [OrderWeight(33)]
        public bool EnableUseSsl { get; set; }

        public async Task<ExecuteResult> ExecuteAsync(IServerCommandExecuteContext dataContext)
        {
            var connectionName = await dataContext.EvaluateFormulaAsync(ConnectionName);
            var brokerAddress = await dataContext.EvaluateFormulaAsync(BrokerAddress);
            var port = await dataContext.EvaluateFormulaAsync(Port);
            var topic = await dataContext.EvaluateFormulaAsync(Topic);
            var username = await dataContext.EvaluateFormulaAsync(Username);
            var password = await dataContext.EvaluateFormulaAsync(Password);

            object[] configMqttOptions = null;
            if (EnableUseSsl)
            {
                configMqttOptions = this.ConfigMqttClientOptions(new object[]
                {
                    brokerAddress.ToString(), CheckPort(port), username.ToString(), password.ToString(),
                    EnableUseSsl
                });
            }
            else
            {
                configMqttOptions = this.ConfigMqttClientOptions(new object[]
                {
                    brokerAddress.ToString(), CheckPort(port), username.ToString(), password.ToString()
                });
            }

            var clientBuilder = ClientBuilderFactory.CreateClientBuilder(configMqttOptions, topic, dataContext,
                CallbackServerCommandName, CallbackServerCommandParamName, _httpClient);
            if (IsSubMultipleTopics)
            {
                await clientBuilder.Subscribe_Multiple_Topics(TopicObjects);
            }
            else
            {
                await clientBuilder.Subscribe_Single_Topic(connectionName.ToString());
            }

            return new ExecuteResult();
        }

        /**
         * username, password, enableUseSsl, brokerAddress, port
         */
        protected override object[] ConfigMqttClientOptions(params object[] paramsList)
        {
            var mqttFactory = new MqttFactory();
            var mqttClient = mqttFactory.CreateMqttClient();
            MqttClientOptions mqttClientOptions = null;
            if ((bool)paramsList[4])
            {
                mqttClientOptions =
                    new MqttClientOptionsBuilder().WithClientId($"Forguncy_{Guid.NewGuid().ToString()}")
                        .WithTcpServer((string)paramsList[0], (int?)paramsList[1])
                        .WithCredentials((string)paramsList[2], (string)paramsList[3])
                        .WithTls()
                        .Build();
            }
            else
            {
                mqttClientOptions =
                    new MqttClientOptionsBuilder().WithClientId($"Forguncy_{Guid.NewGuid().ToString()}")
                        .WithTcpServer((string)paramsList[0], (int?)paramsList[1])
                        .WithCredentials((string)paramsList[2], (string)paramsList[3])
                        .Build();
            }

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
                ? "[用户名密码验证]Mqtt客户端"
                : $"[用户名密码验证]Mqtt客户端返回执行结果到参数:{OutParamaterName}";
        }
    }
}