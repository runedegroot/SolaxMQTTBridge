#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["SolaxMQTTBridge.csproj", "."]
RUN dotnet restore "./SolaxMQTTBridge.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "SolaxMQTTBridge.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SolaxMQTTBridge.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SolaxMQTTBridge.dll"]

EXPOSE 2901