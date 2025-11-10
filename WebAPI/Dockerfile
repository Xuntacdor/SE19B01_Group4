FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore only the API project to avoid pulling tests
COPY WebAPI/WebAPI.csproj WebAPI/
RUN dotnet restore WebAPI/WebAPI.csproj

# Copy the rest of the source (tests are excluded by .dockerignore)
COPY . .
RUN dotnet publish WebAPI/WebAPI.csproj -c Release -o /app/publish --no-restore

# ================= runtime stage =================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
