# NHL Game Schedule

A Blazor application that displays home games for configured NHL teams in a monthly calendar.

## Run locally

```powershell
cd NHLGameSchedule
dotnet run --launch-profile http
```

The default local service URL is `http://0.0.0.0:10099`. Configure the listening address and port with the `Urls` setting in `NHLGameSchedule/appsettings.json`.

## Build the container

From the repository root:

```powershell
docker build -t nhlgameschedule .
docker run --name nhlgameschedule -p 10099:10099 nhlgameschedule
```

Open `http://localhost:10099`.

The container listens on port `10099` by default. Portainer can deploy the published image using:

```text
ghcr.io/rcand/nhlgameschedule:latest
```

The image is published automatically by GitHub Actions after a push to `main`. The package may need to be made public in the repository's **Packages** settings before an unauthenticated Portainer host can pull it.

## Configure teams

Edit `NHLGameSchedule/appsettings.json`:

```json
"NhlSchedule": {
  "Teams": [
    {
      "Abbreviation": "ANA",
      "Name": "Anaheim Ducks"
    }
  ]
}
```

Container settings can override configuration using environment variables, for example:

```text
NhlSchedule__Teams__0__Abbreviation=ANA
NhlSchedule__Teams__0__Name=Anaheim Ducks
Urls=http://0.0.0.0:10099
```
