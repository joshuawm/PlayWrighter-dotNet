

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS publish
COPY  . /app
WORKDIR /app
RUN dotnet publish -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal AS base
WORKDIR /app
#Install powershell to playwright scripts
RUN apt-get update -yq \
    && apt-get install wget -yq \
    && wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update -yq \
    && apt-get install powershell -yq
WORKDIR /app
COPY --from=publish /app/publish .
COPY playwright.ps1 .
# Install playwright dependencies and cleanup
RUN pwsh playwright.ps1 install --with-deps firefox
RUN rm -rf playwright.ps1
RUN apt remove wget powershell -yq
EXPOSE 7276
ENTRYPOINT ["dotnet", "playwrightapi.dll"]