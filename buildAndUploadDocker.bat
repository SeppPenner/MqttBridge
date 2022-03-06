cd src\MqttBridge
dotnet publish -c Release --output publish/ -r linux-x64 --no-self-contained
docker build --tag sepppenner/mqttbridge:1.0.4 -f Dockerfile .
docker login -u sepppenner -p "%DOCKERHUB_CLI_TOKEN%"
docker push sepppenner/mqttbridge:1.0.4
@ECHO.Build successful. Press any key to exit.
pause