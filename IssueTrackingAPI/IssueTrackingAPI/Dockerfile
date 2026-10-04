# syntax=docker/dockerfile:1

# ---------- Stage 1: build & publish ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ solution (build context = thư mục gốc repo, xem docker-compose.yml)
COPY . .

RUN dotnet restore IssueTrackingAPI/IssueTrackingAPI/IssueTrackingAPI.csproj
RUN dotnet publish IssueTrackingAPI/IssueTrackingAPI/IssueTrackingAPI.csproj \
    -c Release -o /app/publish --no-restore

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "IssueTrackingAPI.dll"]
