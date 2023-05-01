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
This service needs infrastructure services like [MongoDB](https://www.mongodb.com) and [RabbitMQ](https://www.rabbitmq.com) upfront. The infrastructure stack is already configured in [Play.Infrastructure](https://github.com/1384microservices/Play.Infrastructure) repository.

### Clone source
First, you must create a workspace folder and then switch to that workspace folder. **D:\Projects\Play.Economy** is a good idea but you should change to whatever fits your needs. Then, you must clone this repository to the workspace you've just created:

```powershell
New-Item -ItemType Directory -Path 'D:\Projects\PlayEconomy'
Set-Locatin -Path 'D:\Projects\PlayEconomy'
git clone https://github.com/1384microservices/Play.Catalog.git
```

### Build service
#### Build contracts
```powershell
Set-Location -Path '.\Play.Identity'
dotnet build .\src\Play.Identity.Contracts\
```

#### Build services
```powershell
Set-Location -Path '.\Play.Identity'
dotnet build .\src\Play.Identity.Service\
```

#### Build docker image
##### Build docker image using docker CLI
```powershell
Set-Location -Path '.\Play.Identity'

# Change with your GitHub Personal Access Token
$github_pat="[type your PAT here]"

$serviceImage="play.identity:latest"

docker build -t $serviceImage --build-args GITHUB_PAT=$github_pat .
```
##### Build docker image using docker compose CLI
To build docker image using docker CLI you need to specify github PAT within an environment variable file located at the root of this repository (near docker-compose.yaml file):

```ini
GITHUB_PAT=[your GitHub PAT]
```

```powershell
Set-Location -Path '.\Play.Identity'
docker-compose build
```

This process will create a docker image named `play.identity:latest` which you will use to start the service.

### Run service
#### Run service as dotnet process
You need to have the [infrastructure services](https://github.com/1384microservices/Play.Infrastructure) started upfront.

You need to provide configuration for MongoDB connection within [appsettings.json](src/Play.Identity.Service/appsettings.json). Check `MongoDbSettings` section.

You need to provide configuration for RabbitMQ connection within [appsettings.json](src/Play.Identity.Service/appsettings.json). Check `RabbitMQSettings` section.

The hostname for both MongoDB and RabbitMQ services is `localhost`. You can find the associated ports within [infrastructure services stack configuration file](https://github.com/1384microservices/Play.Infrastructure/blob/master/src/docker-compose.yml)

```powershell
Set-Locaton -Path '.\Play.Identity'
dotnet run .\src\Play.Identity.Service
```

#### Run service as docker container
You need to have the [infrastructure services](https://github.com/1384microservices/Play.Infrastructure) started upfront.

You need to provide MongoDB and RabbitMQ configuration values within a `.env` file located near docker-compose file like this:
```ini
MongoDbSettings__Host=mongo
MongoDbSettings__Port=27017
RabbitMQSettings__Host=rabbitmq
IdentitySettings__AdminUserPassword=[admin password]
```

You can find the hostname and associated ports for MongoDB and RabbitMQ within [infrastructure services stack configuration file](https://github.com/1384microservices/Play.Infrastructure/blob/master/src/docker-compose.yml).

```powershell
Set-Location -Path '.\Play.Identity'
docker-compose run
```

Of course, you can start the service container using docker cli but keep in mind that this is very verbose and unpractical:

```powershell
# Change with your admin password
$adminPassword="[PASSWORD HERE]"
docker run --rm -it -p 5002:5002 --name play.identity -e MongoDbSettings__Host=mongo -e MongoDbSettings__Port=27017 -e RabbitMQSettings__Host=rabbitmq -e IdentitySettings__AdminUserPassword=$adminPassword --network src_default play.identity:latest
```

## Publish artifacts
### Publish service contracts package
```powershell
# Change with your package version.
$version="1.0.3"

$owner="1384microservices"

# Change with your GitHub Personal Access Token
$gh_pat="[type your PAT here]"

$repositoryUrl="https://github.com/$owner/Play.Identity"

# Build package
dotnet pack .\src\Play.Identity.Contracts\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=$repositoryUrl -o ..\packages\

# Publish package
dotnet nuget push ..\packages\Play.Identity.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

### Publish service container image
```powershell
# Create docker image
$imageVersion="1.0.9"
docker-compose build
docker tag "play.identity:latest" "play.identity:${imageVersion}"

$appName="playeconomy1384"
$repositoryUrl="${appName}.azurecr.io"

docker tag "play.identity:latest" "${repositoryUrl}/play.identity:${imageVersion}"

az acr login --name $appName
docker push "${repositoryUrl}/play.identity:${imageVersion}"
```

### Create K8S secrets
```powershell
# Create identity service namespace
$k8sNS="identity"
kubectl create namespace  $k8sNS

# Configure identity service secrets
$cosmosDbConnectionString="[YOUR MONGODB CONNECTION STRING]"
$serviceBusConnectionString="[YOUR SB CONNECTION STRING]"
$adminPassword="Pass@word1"
kubectl create secret generic identity-secrets --from-literal=cosmosdb-connectionstring=$cosmosDbConnectionString --from-literal=servicebus-connectionstring=$serviceBusConnectionString --from-literal=admin-password=$adminPassword -n $k8sNS

# Delete secrets
kubectl delete secret -n $k8sNS identity-secrets
```

### Create K8S service pod
```powershell
# Create pod
$k8sNS="identity"
kubectl apply -f kubernetes\identity.yaml -n $k8sNS

# Query pod status
kubectl get pods -n $k8sNS

# Query logs
kubectl logs '[pod-name]' -n $k8sNS
kubectl describe '[pod-name]' -n $k8sNS

# Query service
kubectl get svc -n $k8sNS
```

### Creating the Azure Managed Identity and granting access to Key Vault secrets
```powershell
# Create azure Identity
$appName="playeconomy1384"
$k8sNS="identity"
az identity create --resource-group $appName --name $k8sNS

# Fetch Identity client id
$identityClientId = az identity show --resource-group $appName --name $k8sNS --query clientId -otsv

# Assign get and list permissions to client (Identity)
az keyvault set-policy -n $appName --secret-permissions get list --spn $identityClientId
```

### Establish the federated identity credentials
```powershell
$appName="playeconomy1384"
$k8sNS="identity"
$aksOIDIssuer=az aks show -n $appName -g $appName --query "oidcIssuerProfile.issuerUrl" -otsv
az identity federated-credential create --name $k8sNS --identity-name $k8sNS --resource-group $appName --issuer $aksOIDIssuer --subject "system:serviceaccount:${k8sNS}:${k8sNS}-serviceaccount"
```

### Create signing certificate
```powershell
$k8sNS="identity"
kubectl apply -f kubernetes\signing-cert.yaml -n $k8sNS
```































[^wsl]:[You need to have WSL upfront](https://learn.microsoft.com/en-us/windows/wsl/)