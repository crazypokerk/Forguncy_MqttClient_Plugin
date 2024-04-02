using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using GrapeCity.Forguncy.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MqttClient.Utils
{
    public static class ClientBuilderFactory
    {
        public static ClientBuilder CreateClientBuilder(EncodingType encodingType, object[] configMqttOptions,
            object topic,
            IServerCommandExecuteContext dataContext, string callbackServerCommandName,
            string callbackServerCommandParamName, HttpClient _httpClient)
        {
            return new ClientBuilder(configMqttOptions, topic.ToString(), async (message) =>
            {
                dataContext.Parameters[callbackServerCommandParamName] = message;
                if (IsHexString(message))
                {
                    message = HexToString(message);
                }

                string codingName = null;

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                byte[] inBytes;
                byte[] outBytes;
                switch (encodingType)
                {
                    case EncodingType.Gbk:
                        inBytes = Encoding.GetEncoding("gbk").GetBytes(message);
                        outBytes = Encoding.Convert(Encoding.GetEncoding("gbk"), Encoding.UTF8, inBytes);
                        break;
                    case EncodingType.Big5:
                        inBytes = Encoding.GetEncoding("big5").GetBytes(message);
                        outBytes = Encoding.Convert(Encoding.GetEncoding("big5"), Encoding.UTF8, inBytes);
                        break;
                    case EncodingType.Utf8:
                        inBytes = Encoding.GetEncoding("utf-8").GetBytes(message);
                        outBytes = Encoding.Convert(Encoding.GetEncoding("utf-8"), Encoding.UTF8, inBytes);
                        break;
                    case EncodingType.Base64:
                        outBytes = Convert.FromBase64String(message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(encodingType), encodingType, null);
                }

                message = Encoding.UTF8.GetString(outBytes);

                JObject jObject = new JObject();
                jObject.Add(new JProperty(callbackServerCommandParamName, message));
                string jsonMsg = JsonConvert.SerializeObject(jObject);

                await _httpClient.PostAsync(
                    $"{dataContext.AppBaseUrl}ServerCommand/{callbackServerCommandName}",
                    new StringContent(jsonMsg, Encoding.UTF8, "application/json"));
            }, encodingType);
        }

        private static bool IsHexString(string input)
        {
            string pattern = "^([0-9A-Fa-f]+)$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(input);
        }

        private static string HexToString(string hex)
        {
            byte[] bytes = Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
            string normalString = System.Text.Encoding.UTF8.GetString(bytes);
            return normalString;
        }
    }
}