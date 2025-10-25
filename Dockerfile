# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore dependencies
COPY *.sln .
COPY TutorLinkBe/*.csproj ./TutorLinkBe/
RUN dotnet restore ./TutorLinkBe/TutorLinkBe.csproj

# Copy all source and build
COPY . .
WORKDIR /src/TutorLinkBe
RUN dotnet publish -c Release -o /app/publish

# Use ASP.NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 10000
ENTRYPOINT ["dotnet", "TutorLinkBe.dll"]
