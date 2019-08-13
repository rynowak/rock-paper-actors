FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-preview7 as SDK
WORKDIR /src
COPY . .
RUN dotnet publish -o /out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0.0-preview7 as RUNTIME
WORKDIR /app
COPY --from=SDK /out .
ENTRYPOINT [ "dotnet", "./Rochambot.dll" ]