#установка окружения
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
#настройка директорий
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
#не забудьте заменить на свои значения!
COPY ["./WebApplication1/WebApplication1.csproj", "WebApplication1/"] 
RUN dotnet restore "WebApplication1/WebApplication1.csproj"
COPY . .
WORKDIR "/src/WebApplication1"
RUN dotnet build "WebApplication1.csproj" -c Release -o /app/build
#упаковка файлов
FROM build AS publish
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish /p:UseAppHost=false
#запуск приложения
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
