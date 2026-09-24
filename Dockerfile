FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NHLGameSchedule/NHLGameSchedule.csproj", "NHLGameSchedule/"]
RUN dotnet restore "NHLGameSchedule/NHLGameSchedule.csproj"

COPY . .
WORKDIR "/src/NHLGameSchedule"
RUN dotnet publish "NHLGameSchedule.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore
RUN find /app/publish/wwwroot -maxdepth 2 -type f -name 'blazor.web.js' -print

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV Urls=http://0.0.0.0:10099
ENV EnableHttpsRedirection=false
EXPOSE 10099

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NHLGameSchedule.dll"]
