# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all source and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Use ASP.NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:$PORT
ENTRYPOINT ["dotnet", "TutorLinkBe.dll"]
