Push-Location ..
try {
  dotnet run -c Release -- --environment Development --urls http://localhost:5024
}
finally {
  Pop-Location
}

# App available at http://localhost:5024
# Orleans Dashboard at http://localhost:5024/dashboard