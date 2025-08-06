docker kill http1-http2-tls-8.0-nanoserver-ltsc2022
docker rm   http1-http2-tls-8.0-nanoserver-ltsc2022
docker run -d --isolation=hyperv --name http1-http2-tls-8.0-nanoserver-ltsc2022 joaquinvcr.azurecr.io/dotnet:http1-http2-tls-8.0-nanoserver-ltsc2022
for /f "delims=" %%i in ('docker inspect --format "{{.NetworkSettings.Networks.nat.IPAddress}}" http1-http2-tls-8.0-nanoserver-ltsc2022') do start msedge.exe https://%%i