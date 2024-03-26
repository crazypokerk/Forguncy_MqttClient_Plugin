using System;
using System.Collections.Generic;
using MQTTnet.Client;

namespace MqttClient.Utils
{
    public static class ClientPool
    {
        public static Dictionary<string, IMqttClient> ClientConnectionPool = new Dictionary<string, IMqttClient>();
        public static Dictionary<string, string> ConnectionNameList = new Dictionary<string, string>();

        public static bool IsConnectionExist(string connectionName)
        {
            if (connectionName == null) throw new Exception("connection name must be valid!");
            return ConnectionNameList.ContainsKey(connectionName);
        }

        public static IMqttClient GetMqttClient(string connectionName)
        {
            if (connectionName == null) return null;
            if (!ConnectionNameList.ContainsKey(connectionName)) return null;
            var guid = ConnectionNameList[connectionName];
            return ClientConnectionPool[$"{connectionName}_{guid}"];
        }

        public static bool IsConnected(IMqttClient mqttClient)
        {
            return mqttClient.IsConnected;
        }
    }
}