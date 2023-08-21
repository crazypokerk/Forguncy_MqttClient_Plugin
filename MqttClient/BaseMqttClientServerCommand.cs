using System;
using System.Collections.Generic;
using GrapeCity.Forguncy.Commands;
using GrapeCity.Forguncy.Plugin;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MqttClient
{
    public abstract class BaseMqttClientServerCommand : Command
    {
        [FormulaProperty]
        [DisplayName("连接名")]
        [Required]
        [Description("填写的连接名需要记住，如果需要主动触发断开连接命令需要同样的连接名；如在同一个工程中建立多个客户端连接，那么需要保证连接名唯一")]
        [OrderWeight(1)]
        public object ConnectionName { get; set; }

        [FormulaProperty]
        [DisplayName("连接地址")]
        [Required]
        [OrderWeight(2)]
        public object BrokerAddress { get; set; }

        [FormulaProperty]
        [DisplayName("端口")]
        [Required]
        [OrderWeight(3)]
        public object Port { get; set; }

        [FormulaProperty]
        [DisplayName("订阅主题")]
        [Required]
        [OrderWeight(4)]
        public object Topic { get; set; }

        [DisplayName("是否订阅多个主题")]
        [OrderWeight(5)]
        public bool IsSubMultipleTopics { get; set; }

        [DisplayName("配置多个订阅主题")]
        [OrderWeight(6)]
        [ListProperty]
        public List<TopicObject> TopicObjects { get; set; }

        [DisplayName("回调服务端命令")]
        [Description("异步处理订阅主题推送消息的回调服务端命令名称")]
        [Required]
        [ServerCommandNameProperty]
        [OrderWeight(777)]
        public string CallbackServerCommandName { get; set; }

        [DisplayName("回调服务端命令接收参数名称")]
        [Description("用于接收MQTT订阅主题的消息变量")]
        [Required]
        [OrderWeight(888)]
        public string CallbackServerCommandParamName { get; set; }

        [OrderWeight(999)]
        [DisplayName("将结果返回到变量")]
        [ResultToProperty]
        public string OutParamaterName { get; set; }

        public override CommandScope GetCommandScope()
        {
            return CommandScope.ExecutableInServer;
        }

        protected abstract object[] ConfigMqttClientOptions(params object[] paramsList);

        protected static int CheckPort(object port)
        {
            if (!int.TryParse(port.ToString(), out var validPort))
            {
                throw new FormatException("input port may not is a valid number");
            }

            return validPort;
        }

        public override bool GetDesignerPropertyVisible(string propertyName, CommandScope commandScope)
        {
            return propertyName == nameof(TopicObjects)
                ? IsSubMultipleTopics
                : base.GetDesignerPropertyVisible(propertyName, commandScope);
        }
    }

    public class TopicObject
    {
        [DisplayName("订阅主题")]
        [FormulaProperty]
        [SearchableProperty]
        public object Topic { get; set; }

        [DisplayName("描述")]
        [SearchableProperty]
        public string Description { get; set; }
    }
}