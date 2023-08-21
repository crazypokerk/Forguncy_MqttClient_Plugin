using System.Net.Http;
using System.Text;
using GrapeCity.Forguncy.Commands;

namespace MqttClient.Utils
{
    public static class ClientBuilderFactory
    {
        public static ClientBuilder CreateClientBuilder(object[] configMqttOptions, object topic,
            IServerCommandExecuteContext dataContext, string callbackServerCommandName,
            string callbackServerCommandParamName, HttpClient _httpClient)
        {
            return new ClientBuilder(configMqttOptions, topic.ToString(), async (message) =>
            {
                dataContext.Parameters[callbackServerCommandParamName] = message;
                await _httpClient.PostAsync($"{dataContext.AppBaseUrl}ServerCommand/{callbackServerCommandName}",
                    new StringContent(@$"{{""{callbackServerCommandParamName}"":""{message}""}}", Encoding.UTF8,
                        "application/json"));
            });
        }
    }
}