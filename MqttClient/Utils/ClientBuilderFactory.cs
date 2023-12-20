using System.Net.Http;
using System.Text;
using GrapeCity.Forguncy.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                // Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                // string str = string.Join(", ", dict);
                // var jsonObj = new { $"{callbackServerCommandParamName}" = message };
                // var jsonMsg = JsonSerializer.Serialize(jsonObj);

                JObject jObject = new JObject();
                jObject.Add(new JProperty(callbackServerCommandParamName, message));
                string jsonMsg = JsonConvert.SerializeObject(jObject);


                await _httpClient.PostAsync($"{dataContext.AppBaseUrl}ServerCommand/{callbackServerCommandName}",
                    new StringContent(jsonMsg, Encoding.UTF8, "application/json"));
            });
        }
    }
}