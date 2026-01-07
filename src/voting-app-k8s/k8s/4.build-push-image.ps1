$resourceGroup = "rg-usw-391575-s4-aks-dev-demo-01"
$containerRegistry = "acruswaksdevdemo"

$acrLoginServer = $(az acr show --name $containerRegistry --resource-group $resourceGroup --query loginServer --output tsv)
az acr login --name $containerRegistry

Push-Location ..
# Build, push, apply, and restart - stop on any failure
docker build . -t "$acrLoginServer/orleans/votingapp"
if ($LASTEXITCODE -eq 0) {
  docker push "$acrLoginServer/orleans/votingapp"
}
if ($LASTEXITCODE -eq 0) {
  kubectl apply -f ./k8s/5.voting-app-deployment.yaml
}
if ($LASTEXITCODE -eq 0) {
  kubectl rollout restart deployment/votingapp
}
Pop-Location
