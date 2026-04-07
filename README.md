build project, put .dll and meta.json in a folder and put inside jellyfin plugins folder, idk how it works correctly but the folder name can be the plugin name + version example: YoumuLoader_1.0.0.0

Only tested on linux/arch btw

just to know, if you run a docker/podman container you should install deno and link deno, jellyfin ffmpeg and jellyfin ffprobe to /usr/bin so it can
run yt-dlp just fine

run file template, please modify for your needs (mine is diffent btw)

```
#!/bin/bash

set -e

if [ "$1" == "debug" ]; then
    file1="./YoumuLoader/bin/Debug/net9.0/YoumuLoader.dll"
    dotnet build
else
    file1="./YoumuLoader/bin/Release/net9.0/publish/YoumuLoader.dll"
    dotnet publish
fi

file2="./meta.json"
out="/jellyfin/config/plugins/Youmu Loader_1.0.0.0/"

if [[  $(test -d "$out") ]]; then
	mkdir "$out"
fi

cp $file1 $file2 "$out"
sudo systemctl --user restart jellyfin.service
echo "completed!"
```

for running in a container I needed to make this way.

```
FROM docker.io/jellyfin/jellyfin:latest
RUN apt-get update && apt-get install -y unzip \
        && curl -fsSL https://deno.land/install.sh | sh \
        && mv /root/.deno/bin/deno /usr/bin/deno \
        && ln -s /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/bin \
        && ln -s /usr/lib/jellyfin-ffmpeg/ffprobe /usr/bin \
        && apt-get clean && rm -rf /var/lib/apt/lists/*
```

It fucks up with the autoupdate but I don't any other idea