# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./HRHelper.sln ./
COPY ./src/HRHelper/ ./src/HRHelper/
RUN dotnet restore HRHelper.sln
RUN dotnet publish src/HRHelper/HRHelper.csproj -c Release -o /app/publish

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HRHelper.dll"]
