---
services: azure-monitor
platforms: dotnet
author: gucalder
---

# Retrieve Azure Monitor multi-dimensional metrics with .NET

This sample explains how to retrieve Monitor multi-dimensional metrics and multi-dimensional metric definitions using the Azure .NET SDK release_0.18.0-preview or higher.

**NOTE**: calls to this API can retrieve single dimensional metrics/metric definitions too.

**NOTE**: please refer to the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct).

**NOTE**: for contributions refer to the [CONTRIBUTING.md](https://github.com/Azure-Samples/monitor-dotnet-multidimensional-metrics-api/blob/master/CONTRIBUTING.md) file.

**On this page**

- [Run this sample](#run)
- [What is program.cs doing?](#example)
    - [List multi-dim metric definitions for a resource](#list-metricdefinitions)
    - [List multi-dim metrics for a resource](#list-metrics)

<a id="run"></a>
## Run this sample

1. If you don't have it, install the [.NET Core SDK](https://www.microsoft.com/net/core).

1. Clone the repository.

    ```
    git clone https://github.com/Azure-Samples/monitor-multidimensional-metrics-dotnet.git
    ```

1. Install the dependencies.

    ```
    dotnet restore
    ```

1. Create an Azure service principal either through
    [Azure CLI](https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal-cli/),
    [PowerShell](https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/)
    or [the portal](https://azure.microsoft.com/documentation/articles/resource-group-create-service-principal-portal/).

1. Export these environment variables using your subscription id and the tenant id, client id and client secret from the service principle that you created. 

    ```
    export AZURE_TENANT_ID={your tenant id}
    export AZURE_CLIENT_ID={your client id}
    export AZURE_CLIENT_SECRET={your client secret}
    export AZURE_SUBSCRIPTION_ID={your subscription id}
    ```

1. Run the sample.

    ```
    dotnet run resourceId
    ```

<a id="example"></a>
## What is program.cs doing?

The sample retrieves multi dimensional metric definitions and multi dimensional metrics for a given resource.
It starts by setting up a MonitorClient object using your subscription and credentials.

```csharp
// Build the service credentials and Monitor client
var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret);
var monitorClient = new MonitorClient(serviceCreds);
monitorClient.SubscriptionId = subscriptionId;
```

<a id="list-metricdefinitions"></a>
### List multi dimensional metric definitions for a resource

List the multi dimensional metric definitions for the given resource, which is defined in the current subscription.

```csharp
IEnumerable<MetricDefinition> metricDefinitions = await readOnlyClient.MetricDefinitions.ListAsync(resourceUri: resourceUri, cancellationToken: new CancellationToken());
```

**NOTE**: call to the multi-dim metrics API do not support filters for the metric definitions.

<a id="list-metrics"></a>
### List multi dimensional metrics for a resource

```csharp
IEnumerable<Metric> metrics = await readOnlyClient.Metrics.ListAsync(resourceUri: resourceUri, cancellationToken: CancellationToken.None);
```

or with arguments

```csharp

// The timespan is the concatenation of the start and end date/times separated by "/"
string startDate = DateTime.Now.AddHours(-3).ToString("o");
string endDate = DateTime.Now.ToString("o");
string timeSpan = startDate + "/" + endDate;

Write("Call with more parameters, but no filter");
metrics = await readOnlyClient.Metrics.ListAsync(
  resourceUri: resourceUri, 
  timespan: timeSpan,
  interval: TimeSpan.FromMinutes(1),
  metric: "Transactions",

  resultType: ResultType.Data,
  cancellationToken: CancellationToken.None);

Write("Call to retrieve time series with timespan parameter");
metrics = await readOnlyClient.Metrics.ListAsync(
                resourceUri: resourceUri,
                timespan: timeSpan,
                resultType: ResultType.Data,
                cancellationToken: CancellationToken.None).Result;
EnumerateMetrics(metrics);

// interval is equivalent to timeGrain in the single dimension API
Write("Call to retrieve time series with timespan and interval parameters");
metrics = await readOnlyClient.Metrics.ListAsync(
                resourceUri: resourceUri,
                timespan: timeSpan,
                interval: System.TimeSpan.FromMinutes(5),
                resultType: ResultType.Data,
                cancellationToken: CancellationToken.None).Result;
EnumerateMetrics(metrics);

Write("Call to retrieve time series with timespan, interval, and metric parameters");
metrics = await readOnlyClient.Metrics.ListAsync(
                resourceUri: resourceUri,
                timespan: timeSpan,
                interval: System.TimeSpan.FromMinutes(5),
                metric: "CpuPercentage",
                resultType: ResultType.Data,
                cancellationToken: CancellationToken.None).Result;
EnumerateMetrics(metrics);

Write("Call to retrieve time series with timespan, interval, metric, and aggregation parameters");
metrics = await readOnlyClient.Metrics.ListAsync(
                resourceUri: resourceUri,
                timespan: timeSpan,
                interval: System.TimeSpan.FromMinutes(5),
                metric: "CpuPercentage",
                aggregation: "Count",
                resultType: ResultType.Data,
                cancellationToken: CancellationToken.None).Result;
EnumerateMetrics(metrics);

Write("Call to retrieve time series with timespan, interval, metric, and $filter parameters. NOTE: $filter is reserved for metadata only.");
// Filter (just an example). The user must know which metadata are available.
// More conditions can be added with the 'or' and 'and' operators
ODataQuery<MetadataValue> odataFilterMetrics = new ODataQuery<Metric>(
    string.Format(
        "Metadata1 eq '{0}' and Metadata2 eq '{1}' or Metadata3 eq '*'",
        "m1",
        "m2"));
        
metrics = readOnlyClient.Metrics.List(
                resourceUri: resourceUri,
                timespan: timeSpan,
                interval: System.TimeSpan.FromMinutes(5),
                metric: "CpuPercentage",
                odataQuery: odataFilterMetrics,
                aggregation: "Count",
                resultType: ResultType.Data);
EnumerateMetrics(metrics);

Write("Call to retrieve metadata with timespan");
// For this query (for metadata) requires at least one metadata eq '*'
odataFilterMetrics = new ODataQuery<Metric>("Metadata3 eq '*'");
var metadata = await readOnlyClient.Metrics.ListAsync(
                resourceUri: resourceUri,
                odataQuery: odataFilterMetrics,
                timespan: timeSpan,
                metric: "CpuPercentage",
                resultType: ResultType.Metadata,
                cancellationToken: CancellationToken.None).Result;
EnumerateMetrics(metrics);
```

**NOTE**: there are other parameters not illustrated here like aggregation, $top, $orderby. Please refer to the SDK documentation for details.