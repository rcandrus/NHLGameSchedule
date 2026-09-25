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
RUN framework_asset=$(find /root/.nuget/packages/microsoft.aspnetcore.app.internal.assets \
        -path '*/_framework/blazor.web.js' | sort -V | tail -n 1) \
    && test -n "$framework_asset" \
    && mkdir -p /app/publish/wwwroot/_framework \
    && cp "$framework_asset" /app/publish/wwwroot/_framework/blazor.web.js \
    && test -f /app/publish/wwwroot/_framework/blazor.web.js

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir -p /app/keys

ENV ASPNETCORE_ENVIRONMENT=Production
ENV Urls=http://0.0.0.0:10099
ENV EnableHttpsRedirection=false
EXPOSE 10099
VOLUME ["/app/keys"]

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NHLGameSchedule.dll"]
