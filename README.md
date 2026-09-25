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

Persist the `/app/keys` directory with a Portainer volume. ASP.NET Core stores its Data Protection keys there so antiforgery tokens and Blazor circuits continue to work across container restarts.

Each workflow run also publishes immutable image tags:

```text
ghcr.io/rcandrus/nhlgameschedule:build-42
ghcr.io/rcandrus/nhlgameschedule:sha-<commit-sha>
```

Use the GitHub Actions run number or commit SHA to deploy a specific build in Portainer instead of `latest`. The `latest` tag can move when a newer commit is pushed; build and SHA tags do not.

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
