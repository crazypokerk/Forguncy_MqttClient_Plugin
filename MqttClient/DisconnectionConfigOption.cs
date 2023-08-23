using System;
using System.ComponentModel;
using System.Threading.Tasks;
using GrapeCity.Forguncy.Commands;
using GrapeCity.Forguncy.Plugin;
using MqttClient.Utils;

namespace MqttClient
{
    [Icon("pack://application:,,,/MqttClient;component/Resources/disconnection.png")]
    [Category("MQTT客户端")]
    public class DisconnectionConfigOption : BaseMqttClientServerCommand, ICommandExecutableInServerSideAsync
    {
        [FormulaProperty]
        [DisplayName("等待时间(s)")]
        [Description("如想等待几秒后触发断开连接，那么可以设置等待时间，单位为秒")]
        [OrderWeight(11)]
        public object WaitSeconds { get; set; }

        protected override object[] ConfigMqttClientOptions(params object[] paramsList)
        {
            throw new NotImplementedException();
        }

        public async Task<ExecuteResult> ExecuteAsync(IServerCommandExecuteContext dataContext)
        {
            var connectionName = await dataContext.EvaluateFormulaAsync(ConnectionName);
            var waitSeconds = await dataContext.EvaluateFormulaAsync(WaitSeconds);

            if (waitSeconds != null && int.TryParse(waitSeconds.ToString(), out var validSeconds))
            {
                await Task.Delay(TimeSpan.FromSeconds(validSeconds));
            }

            var client = ClientPool.GetMqttClient(connectionName.ToString());
            try
            {
                if (client != null)
                {
                    await ClientBuilder.DisconnectClient(connectionName.ToString());
                    dataContext.Parameters[OutParamaterName] = $"连接: {connectionName}已断开";
                }
                else
                {
                    dataContext.Parameters[OutParamaterName] = "连接名对应的Mqtt连接不存在，请确认后重新输入连接名！";
                }
            }
            catch (Exception e)
            {
                dataContext.Parameters[OutParamaterName] = e.ToString();
            }

            return new ExecuteResult();
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
                default:
                    return base.GetDesignerPropertyVisible(propertyName, commandScope);
            }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(OutParamaterName)
                ? "[断开连接]Mqtt客户端"
                : $"[断开连接]Mqtt客户端返回执行结果到参数:{OutParamaterName}";
        }
    }
}