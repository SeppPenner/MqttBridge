cd src\MqttBridge
dotnet publish -c Release --output publish/ -r linux-arm --no-self-contained
docker build --tag sepppenner/mqttbridge-arm:1.0.8 -f Dockerfile.armv7 .
docker login -u sepppenner -p "%DOCKERHUB_CLI_TOKEN%"
docker push sepppenner/mqttbridge-arm:1.0.8
@ECHO.Build successful. Press any key to exit.
pause