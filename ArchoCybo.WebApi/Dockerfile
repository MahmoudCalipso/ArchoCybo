# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ArchoCybo.sln ./
COPY ArchoCybo.WebApi/ArchoCybo.WebApi.csproj ArchoCybo.WebApi/
COPY ArchoCybo.Application/ArchoCybo.Application.csproj ArchoCybo.Application/
COPY ArchoCybo.Infrastructure/ArchoCybo.Infrastructure.csproj ArchoCybo.Infrastructure/
COPY ArchoCybo.Domain/ArchoCybo.Domain.csproj ArchoCybo.Domain/
COPY ArchoCybo.SharedKernel/ArchoCybo.SharedKernel.csproj ArchoCybo.SharedKernel/
RUN dotnet restore ArchoCybo.WebApi/ArchoCybo.WebApi.csproj
COPY . .
RUN dotnet publish ArchoCybo.WebApi/ArchoCybo.WebApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","ArchoCybo.WebApi.dll"]
