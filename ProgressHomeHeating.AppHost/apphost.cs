#:sdk Aspire.AppHost.Sdk@13.5.0
#:package Aspire.Hosting.Dotnet@13.5.0-preview.1.26417.10
#:package Aspire.Hosting.PostgreSQL@13.5.2
#:property UserSecretsId=dbb7e8e9-d940-4119-92de-d92b5f55b379

#pragma warning disable ASPIREDOTNETPROJECT001

var builder = DistributedApplication.CreateBuilder(args);

var azureOpenAiEndpoint = builder.AddParameter("azure-openai-endpoint");
var azureOpenAiApiKey = builder.AddParameter("azure-openai-api-key", secret: true);
var azureOpenAiDeploymentName = builder.AddParameter("azure-openai-deployment-name");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("progresshomeheating-postgres-data")
    .WithPgAdmin();

var operationsDb = postgres.AddDatabase("operationsdb");

var operationsApi = builder.AddDotnetProject("operationsapi", "../ProgressHomeHeating.OperationsApi/ProgressHomeHeating.OperationsApi.csproj")
    .WithReference(operationsDb)
    .WaitFor(operationsDb);

var agentApi = builder.AddDotnetProject("agentapi", "../ProgressHomeHeating.AgentApi/ProgressHomeHeating.AgentApi.csproj")
    .WithReference(operationsApi)
    .WaitFor(operationsApi)
    .WithEnvironment("AzureOpenAI__Endpoint", azureOpenAiEndpoint)
    .WithEnvironment("AzureOpenAI__ApiKey", azureOpenAiApiKey)
    .WithEnvironment("AzureOpenAI__DeploymentName", azureOpenAiDeploymentName);

builder.AddDotnetProject("web", "../ProgressHomeHeating.Web/ProgressHomeHeating.Web.csproj")
    .WithReference(operationsApi)
    .WaitFor(operationsApi)
    .WithReference(agentApi)
    .WaitFor(agentApi);

builder.Build().Run();
