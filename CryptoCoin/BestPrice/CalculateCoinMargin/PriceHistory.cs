using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateCoinMargin
{
    public class PriceHistory : TableEntity
    {
        public string CoinName { get; set; }
        public string Currency { get; set; }
        public double CoinbasePrice { get; set; }
        public double BtcmarketPrice { get; set; }
        public double Margin { get; set; }

        public PriceHistory()
        {
        }

        public PriceHistory(string partitionKey)
        {
            this.PartitionKey = partitionKey + DateTime.Now.ToString("yyyyMMdd");
            this.RowKey = Guid.NewGuid().ToString();
        }
    }
}
