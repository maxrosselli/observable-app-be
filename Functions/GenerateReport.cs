using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;

namespace MonitoringPOC.Functions
{
    public static class GenerateReport
    {
        [FunctionName("GenerateReport")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Generazione report con dati reali avviata.");

            string workspaceId = Environment.GetEnvironmentVariable("caf7b1f3-e6f6-4d70-8ab2-fddc5768dba7");

            var client = new LogsQueryClient(new DefaultAzureCredential());

            string queryDurata = @"
                requests
                | where timestamp > ago(5m)
                | summarize avg(duration)";

            string queryErrori = @"
                exceptions
                | where timestamp > ago(5m)
                | summarize count()";

            var durataResponse = await client.QueryWorkspaceAsync(workspaceId, queryDurata, new QueryTimeRange(TimeSpan.FromMinutes(5)));
            var erroriResponse = await client.QueryWorkspaceAsync(workspaceId, queryErrori, new QueryTimeRange(TimeSpan.FromMinutes(5)));

            double durataMedia = Convert.ToDouble(durataResponse.Value.Table.Rows[0][0]) / 1000; // in secondi
            int erroriTotali = Convert.ToInt32(erroriResponse.Value.Table.Rows[0][0]);

            string report = $"Report osservabilità - {DateTime.UtcNow:dd/MM/yyyy HH:mm}\n\n"
                + $"Prestazioni (ultimi 5 minuti):\n"
                + $"- Durata media delle richieste: {durataMedia:F2} sec\n"
                + $"Errori:\n"
                + $"- Numero di eccezioni: {erroriTotali}\n\n"
                + "Insight GenAI:\n"
                + "- Se la durata supera 2 sec, valuta ottimizzazioni.\n"
                + "- Se gli errori aumentano, verifica i log delle eccezioni.\n";

            return new OkObjectResult(report);
        }
    }
}