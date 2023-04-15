# Play Economy identity service
Play economy game items catalog service.

## About
This service implements identity service REST API and register, login and logout UI components.

## Contribute
### Prerequisites
* Install [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/)
* Install git: `winget install --id Git.Git --source winget`
* Install dotnet 6 (or greater) SDK: `winget install --d Microsoft.DotNet.SDK.6`
* Install docker[^wsl]: `winget install --id Docker.DockerDesktop`
* Install visual studio code: `winget install --id VisualStudioCode --source winget`

### Build development infrastructure.
This service needs some infrastructure services like MongoDB to run. The infrastructure stack is already configured at [Play.Infrastructure](https://github.com/1384microservices/Play.Infrastructure) repository.

### Clone source
Create a project folder on your box. **D:\Projects\Play Economy** is a good idea but you can choose whatever fits your needs. For Windows boxes you have to issue this command in a powershell window: `New-Item -ItemType Directory -Path 'D:\Projects\Play Economy'`. Switch to this directory: `Set-Locatin -Path 'D:\Projects\Play Economy'`. 

Clone this repository to your box: `git clone https://github.com/1384microservices/Play.Catalog.git`.

### Run service
Within your service repository root folder (ie `D:\Projects\Play Economy`) start the service by issuing `dotnet run --Project .\src\Play.Catalog.Service\Play.Catalog.Service.csproj`

### Pack and publish contracts
```powershell
# Change with your package version.
$version="1.0.2"

$owner="1384microservices"

# Change with your GitHub Personal Access Token
$gh_pat="[type your PAT here]"

$repositoryUrl="https://github.com/$owner/Play.Identity"

# Build package
dotnet pack .\src\Play.Identity.Contracts\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=$repositoryUrl -o ..\packages\

# Publish package
dotnet nuget push ..\packages\Play.Identity.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

### Dockerize
You can either build the service docker image using docker CLI or docker compose.

#### Dockerize using docker CLI
```powershell
# Change with your GitHub Personal Access Token
$gh_pat="[type your PAT here]"

$serviceImage="play.identity:latest"
docker build -t $serviceImage --build-args GITHUB_PAT=$gh_pat .
```

#### Dockerize using docker-compose CLI
First you need to create a .env configuratin file within the service root that contains GitHub Personal Access Token.
```ini
GITHUB_PAT=[your GitHub PAT]
```
Than, in the project root folder you need to build the image using docker-compose CLI:

```powershell
docker-compose build
```

[^wsl]:[You need to have WSL upfront](https://learn.microsoft.com/en-us/windows/wsl/)