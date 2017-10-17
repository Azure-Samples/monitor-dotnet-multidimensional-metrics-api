using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Azure Management dependencies
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Rest.Azure.OData;

// These examples correspond to the Monitor .Net SDK versions >= 0.18.0-preview
// Those versions include the multi-dimensional metrics API, which works with the previous single-dimensional metrics API too.
using Microsoft.Azure.Management.Monitor;
using Microsoft.Azure.Management.Monitor.Models;

namespace AzureMonitorCSharpExamples
{
    public class Program
    {
        private static MonitorClient readOnlyClient;

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Usage: AzureMonitorCSharpExamples <resourceId>");
            }

            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var secret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            if (new List<string> { tenantId, clientId, secret, subscriptionId }.Any(i => String.IsNullOrEmpty(i)))
            {
                Console.WriteLine("Please provide environment variables for AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET and AZURE_SUBSCRIPTION_ID.");
            }
            else
            {
                readOnlyClient = AuthenticateWithReadOnlyClient(tenantId, clientId, secret, subscriptionId).Result;
                var resourceId = args[1];

                RunMetricDefinitionsSample(readOnlyClient, resourceId).Wait();
                RunMetricsSample(readOnlyClient, resourceId).Wait();
             }
        }

        #region Authentication
        private static async Task<MonitorClient> AuthenticateWithReadOnlyClient(string tenantId, string clientId, string secret, string subscriptionId)
        {
            // Build the service credentials and Monitor client
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret);
            var monitorClient = new MonitorClient(serviceCreds);
            monitorClient.SubscriptionId = subscriptionId;

            return monitorClient;
        }
        #endregion

        #region Examples
        private static async Task RunMetricDefinitionsSample(MonitorClient readOnlyClient, string resourceUri)
        {
            // NOTE: this call does NOT accept a $filter as opposed to what it did in the previous releases (single-dim API)
            IEnumerable<MetricDefinition> metricDefinitions = await readOnlyClient.MetricDefinitions.ListAsync(resourceUri: resourceUri, cancellationToken: new CancellationToken());
            EnumerateMetricDefinitions(metricDefinitions);
        }

        private static async Task RunMetricsSample(MonitorClient readOnlyClient, string resourceUri)
        {
            Write("Call with default parameters");
            Response metrics = await readOnlyClient.Metrics.ListAsync(resourceUri: resourceUri, cancellationToken: CancellationToken.None);
            EnumerateMetrics(metrics);
            
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
                            cancellationToken: CancellationToken.None);
            EnumerateMetrics(metrics);

            // interval is equivalent to timeGrain in the single dimension API
            Write("Call to retrieve time series with timespan and interval parameters");
            metrics = await readOnlyClient.Metrics.ListAsync(
                            resourceUri: resourceUri,
                            timespan: timeSpan,
                            interval: System.TimeSpan.FromMinutes(5),
                            resultType: ResultType.Data,
                            cancellationToken: CancellationToken.None);
            EnumerateMetrics(metrics);

            Write("Call to retrieve time series with timespan, interval, and metric parameters");
            metrics = await readOnlyClient.Metrics.ListAsync(
                            resourceUri: resourceUri,
                            timespan: timeSpan,
                            interval: System.TimeSpan.FromMinutes(5),
                            metric: "CpuPercentage",
                            resultType: ResultType.Data,
                            cancellationToken: CancellationToken.None);
            EnumerateMetrics(metrics);

            Write("Call to retrieve time series with timespan, interval, metric, and aggregation parameters");
            metrics = await readOnlyClient.Metrics.ListAsync(
                            resourceUri: resourceUri,
                            timespan: timeSpan,
                            interval: System.TimeSpan.FromMinutes(5),
                            metric: "CpuPercentage",
                            aggregation: "Count",
                            resultType: ResultType.Data,
                            cancellationToken: CancellationToken.None);
            EnumerateMetrics(metrics);
            
            Write("Call to retrieve time series with timespan, interval, metric, and $filter parameters. NOTE: $filter is reserved for metadata only.");
            // Filter (just an example). The user must know which metadata are available.
            // More conditions can be added with the 'or' and 'and' operators
            ODataQuery<MetadataValue> odataFilterMetrics = new ODataQuery<MetadataValue>(
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
            odataFilterMetrics = new ODataQuery<MetadataValue>("Metadata3 eq '*'");
            var metadata = await readOnlyClient.Metrics.ListAsync(
                            resourceUri: resourceUri,
                            odataQuery: odataFilterMetrics,
                            timespan: timeSpan,
                            metric: "CpuPercentage",
                            resultType: ResultType.Metadata,
                            cancellationToken: CancellationToken.None);
            EnumerateMetrics(metrics);
        }
        #endregion

        #region Helpers
        private static void Write(string format, params object[] items)
        {
            Console.WriteLine(string.Format(format, items));
        }

        private static void EnumerateMetricDefinitions(IEnumerable<MetricDefinition> metricDefinitions, int maxRecords = 5)
        {
            var numRecords = 0;
            /* Structure of MetricDefinition
               string ResourceId 
               LocalizableString Name
               Unit? Unit
               AggregationType? PrimaryAggregationType 
               IList<MetricAvailability> MetricAvailabilities
               string Id
            */
            foreach (var metricDefinition in metricDefinitions)
            {
                Write(
                    "Id: {0}\n Name: {1}, {2}\nResourceId: {3}\nUnit: {4}\nPrimary aggregation type: {5}\nList of metric availabilities: {6}",
                    metricDefinition.Id,
                    metricDefinition.Name.Value,
                    metricDefinition.Name.LocalizedValue,
                    metricDefinition.ResourceId,
                    metricDefinition.Unit,
                    metricDefinition.PrimaryAggregationType,
                    metricDefinition.MetricAvailabilities);

                // Display only maxRecords records at most
                numRecords++;
                if (numRecords >= maxRecords)
                {
                    break;
                }
            }
        }

        private static void EnumerateMetrics(Response metrics, int maxRecords = 5)
        {
          Write(
                "Cost: {0}\r\nTimespan: {1}\r\nInterval: {2}\r\n",
                metrics.Cost,
                metrics.Timespan,
                metrics.Interval);
          
          var numRecords = 0;
          Write("Id\tName.Value\tName.Localized\tType\tUnit\tTimeseries");
          foreach (var metric in metrics.Value)
          {
              Write(
                  "{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                  metric.Id,
                  metric.Name.Value,
                  metric.Name.LocalizedValue,
                  metric.Type,
                  metric.Unit,
                  metric.Timeseries);

              // Display only 5 records at most
              numRecords++;
              if (numRecords >= maxRecords)
              {
                  break;
              }
          }
        }
        #endregion
    }
}
