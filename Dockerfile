# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the code and build the app
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official .NET 8 runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Expose port 5121 (your app's port)
EXPOSE 5121

# Set the ASP.NET Core URL to use port 5121
ENV ASPNETCORE_URLS=http://+:5121

# Run the app
ENTRYPOINT ["dotnet", "CodeEditorBackend.dll"]