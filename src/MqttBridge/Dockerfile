FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
COPY publish .

ENV ASPNETCORE_URLS="http://*:5000" \
    ASPNETCORE_ENVIRONMENT="Production" \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=Europe/Berlin \
    LANG=de_DE.UTF-8 \
    LANGUAGE=${LANG} \
    LC_ALL=${LANG}

CMD ["dotnet", "MqttBridge.dll"]