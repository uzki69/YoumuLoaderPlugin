build project, put .dll and meta.json in a folder and put inside jellyfin plugins folder, idk how it works correctly but the folder name can be the plugin name + version example: YoumuLoader_1.0.0.0

Only tested on linux/arch btw

just to know, if you run a docker/podman container you should install deno and link deno, jellyfin ffmpeg and jellyfin ffprobe to /usr/bin so it can
run yt-dlp just fine
