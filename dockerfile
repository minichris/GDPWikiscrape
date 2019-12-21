FROM mcr.microsoft.com/dotnet/core/sdk:2.2.207-alpine3.10 AS build
WORKDIR /app
COPY src/*.csproj ./GDPWikiscrape/
WORKDIR /app/GDPWikiscrape
RUN dotnet restore
WORKDIR /app/
COPY src/. ./GDPWikiscrape/
WORKDIR /app/GDPWikiscrape
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:2.2.8-alpine3.10 AS runtime
WORKDIR /app
COPY --from=build /app/GDPWikiscrape/out ./
ENTRYPOINT ["dotnet", "Parser.dll"]