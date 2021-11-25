MqttBridge
====================================

MqttBridge is a project to run a simple local [MQTT server](https://github.com/chkr1011/MQTTnet) from a json config file and bridge data from a local MQTT server to a remote one.

[![Build status](https://ci.appveyor.com/api/projects/status/pmjxmlygfiyna44h?svg=true)](https://ci.appveyor.com/project/SeppPenner/mqttbridge)
[![GitHub issues](https://img.shields.io/github/issues/SeppPenner/MqttBridge.svg)](https://github.com/SeppPenner/MqttBridge/issues)
[![GitHub forks](https://img.shields.io/github/forks/SeppPenner/MqttBridge.svg)](https://github.com/SeppPenner/MqttBridge/network)
[![GitHub stars](https://img.shields.io/github/stars/SeppPenner/MqttBridge.svg)](https://github.com/SeppPenner/MqttBridge/stargazers)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://raw.githubusercontent.com/SeppPenner/MqttBridge/master/License.txt)
[![Known Vulnerabilities](https://snyk.io/test/github/SeppPenner/MqttBridge/badge.svg)](https://snyk.io/test/github/SeppPenner/MqttBridge)

# JSON configuration (Adjust this to your needs)
```json
{
  "BridgeUrl" : "mqtt.test.de",
  "BridgePort": 8883,
  "UseSsl" : true,
  "BridgeUser":
    {
      "UserName": "Hans",
      "ClientId": "Hans",
      "Password": "Test"
    }
}
```

Change history
--------------

See the [Changelog](https://github.com/SeppPenner/MqttBridge/blob/master/Changelog.md).