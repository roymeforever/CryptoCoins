using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Net;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace CalculateCoinMargin
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
        {
            log.WriteLine(message);
        }

        private static TableRequestOptions tableRequestOptions = new TableRequestOptions()
        {
            RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(2), 5),
            LocationMode = LocationMode.PrimaryThenSecondary,
            MaximumExecutionTime = TimeSpan.FromSeconds(10)
        };
        [NoAutomaticTrigger]
        public static void Run(TextWriter textWriterLogger)
        {
            RunCoin(@"https://coinmarketcap.com/currencies/litecoin/#markets", "LTC");
            RunCoin(@"https://coinmarketcap.com/currencies/ethereum/#markets", "ETH");
        }

        [NoAutomaticTrigger]
        public static void RunCoin(string URL, string coin)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = web.Load(URL);
            HtmlNode[] nodes = document.DocumentNode.SelectNodes("//table[@id='markets-table']//tr").ToArray();
            float coinbase = 1;
            float btcmarket = 1;
            var gdaxnode = document.DocumentNode.SelectNodes("//table[@id='markets-table']//tr").Where(x => x.InnerHtml.Contains("exchanges/gdax") && x.InnerHtml.Contains(coin+"/USD")).First();
            HtmlNode price = gdaxnode.SelectSingleNode(".//span[@class='price']");
            if (price != null)
            {
                var str = price.InnerHtml.Replace('$', ' ');
                float.TryParse(str, out coinbase);
            }
            var btcnode = document.DocumentNode.SelectNodes("//table[@id='markets-table']//tr").Where(x => x.InnerHtml.Contains("exchanges/btc-markets") && x.InnerHtml.Contains(coin+"/AUD")).First();
            price = btcnode.SelectNodes(".//span[@class='price']").First();
            if (price != null)
            {
                var str = price.InnerHtml.Replace('$', ' ');
                float.TryParse(str, out btcmarket);
            }
            float margin = (float)((btcmarket / coinbase) * 100.0);
            Console.WriteLine("Profile Margin {0:N2}% exclude 5% Fee", (btcmarket / coinbase)*100);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
               ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            tableClient.DefaultRequestOptions = tableRequestOptions;
            // Retrieve a reference to the table.
            CloudTable table = tableClient.GetTableReference("CryptoCoins");

            // Create the table if it doesn't exist.
            if (!table.Exists())
                table.CreateIfNotExists();

            var ph = new PriceHistory(coin)
            {
                CoinName = coin,
                Currency = "USD",
                BtcmarketPrice = btcmarket,
                CoinbasePrice = coinbase,
                Margin = margin
            };


            // Create the TableOperation object that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(ph);

            // Execute the insert operation.
            table.Execute(insertOperation);

            Console.WriteLine("Insert Success");
        }
    }
}
