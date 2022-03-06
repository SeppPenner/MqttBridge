MqttBridge
====================================

MqttBridge is a project to run a simple local [MQTT server](https://github.com/chkr1011/MQTTnet) from a json config file and bridge data from a local MQTT server to a remote one.

[![Build status](https://ci.appveyor.com/api/projects/status/pmjxmlygfiyna44h?svg=true)](https://ci.appveyor.com/project/SeppPenner/mqttbridge)
[![GitHub issues](https://img.shields.io/github/issues/SeppPenner/MqttBridge.svg)](https://github.com/SeppPenner/MqttBridge/issues)
[![GitHub forks](https://img.shields.io/github/forks/SeppPenner/MqttBridge.svg)](https://github.com/SeppPenner/MqttBridge/network)
[![GitHub stars](https://img.shields.io/github/stars/SeppPenner/MqttBridge.svg)](https://github.com/SeppPenner/MqttBridge/stargazers)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://raw.githubusercontent.com/SeppPenner/MqttBridge/master/License.txt)
[![Known Vulnerabilities](https://snyk.io/test/github/SeppPenner/MqttBridge/badge.svg)](https://snyk.io/test/github/SeppPenner/MqttBridge)

Docker:

[![Docker Pulls](https://img.shields.io/docker/pulls/sepppenner/mqttbride)](https://hub.docker.com/repository/docker/sepppenner/mqttbride)
[![Docker Image Size (latest by date)](https://img.shields.io/docker/image-size/sepppenner/mqttbride?sort=date)](https://hub.docker.com/repository/docker/sepppenner/mqttbride)
[![Docker Stars](https://img.shields.io/docker/stars/sepppenner/mqttbride)](https://hub.docker.com/repository/docker/sepppenner/mqttbride)

Docker (ARM):

[![Docker Pulls](https://img.shields.io/docker/pulls/sepppenner/mqttbride-arm)](https://hub.docker.com/repository/docker/sepppenner/mqttbride-arm)
[![Docker Image Size (latest by date)](https://img.shields.io/docker/image-size/sepppenner/mqttbride-arm?sort=date)](https://hub.docker.com/repository/docker/sepppenner/mqttbride-arm)
[![Docker Stars](https://img.shields.io/docker/stars/sepppenner/mqttbride-arm)](https://hub.docker.com/repository/docker/sepppenner/mqttbride-arm)

# JSON configuration (Adjust this to your needs)
```json
{
    "AllowedHosts": "*",
    "MqttBridge": {
        "Port": 1883,
        "Users": [
            {
                "UserName": "Hans",
                "Password": "Test"
            }
        ],
        "DelayInMilliSeconds": 30000,
        "TlsPort": 8883,
        "BridgeUrl": "mqtt.test.de",
        "BridgePort": 8883,
        "UseTls": true,
        "BridgeUser": [
            {
                "UserName": "Hans",
                "Password": "Test",
                "ClientId": "Hans"
            }
        ]
    }
}
```

# Run this project in Docker from the command line (Examples for Powershell, but should work in other shells as well):

1. Change the directory
    ```bash
    cd ..\src\MqttBridge
    ```

2. Publish the project
    ```bash
    dotnet publish -c Release --output publish/
    ```

3. Build the docker file:
    * `dockerhubuser` is a placeholder for your docker hub username, if you want to build locally, just name the container `mqttbride`
    * `1.0.2` is an example version tag, use it as you like
    * `-f Dockerfile .` (Mind the `.`) is used to specify the dockerfile to use

    ```bash
    docker build --tag dockerhubuser/mqttbride:1.0.2 -f Dockerfile .
    ```

4. Push the project to docker hub (If you like)
    * `dockerhubuser` is a placeholder for your docker hub username, if you want to build locally, just name the container `mqttbride`
    * `1.0.2` is an example version tag, use it as you like

    ```bash
    docker push dockerhubuser/mqttbride:1.0.2
    ```

5. Run the container:
    * `-d` runs the docker container detached (e.g. no logs shown on the console, is needed if run as service)
    * `--name="mqttbride"` gives the container a certain name
    * `-p 1883:1883` opens the internal container port 1883 (Default MQTT without TLS) to the external port 1883
    * `-p 8883:8883` opens the internal container port 8883 (Default MQTT with TLS) to the external port 8883
    * `-v "/home/config.json:/app/appsettings.json"` sets the path to the external configuration file (In the example located under `/home/appsettings.json`) to the container internally
    
    ```bash
    docker run -d --name="mqttbride" -p 1883:1883 -p 8883:8883 -v "/home/appsettings.json:/app/appsettings.json" --restart=always dockerhubuser/mqttbride:1.0.2
    ```

6. Check the status of all containers running (Must be root)
    ```bash
    docker ps -a
    ```

7. Stop a container
    * `containerid` is the id of the container obtained from the `docker ps -a` command
    ```bash
    docker stop containerid
    ```

8. Remove a container
    * `containerid` is the id of the container obtained from the `docker ps -a` command
    ```bash
    docker rm containerid
    ```

Change history
--------------

See the [Changelog](https://github.com/SeppPenner/MqttBridge/blob/master/Changelog.md).
