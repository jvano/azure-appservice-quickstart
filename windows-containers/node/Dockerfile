FROM mcr.microsoft.com/windows/servercore:ltsc2019 AS build-machine
RUN powershell.exe -Command "& { Start-BitsTransfer -Source https://nodejs.org/dist/v14.14.0/node-v14.14.0-win-x64.zip -Destination C:\node-v14.14.0-win-x64.zip }"
RUN powershell.exe -Command "& { Expand-Archive -Path C:\node-v14.14.0-win-x64.zip -DestinationPath C:\ }"

FROM mcr.microsoft.com/windows/nanoserver:ltsc2019 AS web-machine
COPY --from=build-machine /node-v14.14.0-win-x64/ /node-v14.14.0-win-x64/
WORKDIR /app
COPY package*.json ./
RUN C:\node-v14.14.0-win-x64\npm install
COPY index.js ./
ENTRYPOINT ["/node-v14.14.0-win-x64/node.exe", "index.js"]
EXPOSE 80