# Animarr

Animarr is a fork of [Sonarr](https://github.com/Sonarr/Sonarr) built for anime. It is meant to sit next to Radarr (movies) and Sonarr (TV) as the app that manages anime.

Status: early. Right now it builds and runs as stock Sonarr 4.0.19.3001. The anime changes below are planned, not done.

## Goals

- Rank releases by language first (dual audio, then English dub, then Japanese with subs), then by quality.
- Track the languages you want per series and per episode, and show what you have, what exists, and when a dub first appeared.
- Show AniList and MyAnimeList ratings, popularity and dub status on series pages.
- Keep Sonarr's API, so Prowlarr, Seerr and other tools can talk to it as a Sonarr instance.

## Build

Backend (needs the .NET SDK pinned in `global.json`) and frontend (Node 20, Yarn 1):

```sh
dotnet publish src/NzbDrone.Console/Sonarr.Console.csproj -c Release -f net6.0 -r linux-x64 --self-contained false -p:Platform=Posix -o out/bin
yarn install --frozen-lockfile
yarn build --env production
```

The original Sonarr README is kept in `README.sonarr.md`.

## License and credit

Animarr is a derivative of Sonarr and is licensed under GPL-3.0, the same as Sonarr. See `LICENSE.md`. Sonarr is copyright its contributors; this project is not affiliated with or endorsed by the Sonarr team.
