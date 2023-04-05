using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;


public class Trade
{
    public string Ticker { get; set; }
    public DateTime TradeDate { get; set; }
    public string BuySell { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PrevClose { get; set; }
    public decimal DailyPL { get; set; }
    public decimal InceptionPL { get; set; }
}

public class Portfolio
{
    public List<Trade> Trades { get; set; }

    public Portfolio()
    {
        Trades = new List<Trade>();
    }

    public void AddTrade(Trade trade)
    {
        Trades.Add(trade);
    }

    public void SaveToFile(string filename)
    {
        using (FileStream fs = new FileStream(filename, FileMode.Create))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Portfolio));
            serializer.Serialize(fs, this);
        }
    }

    public static Portfolio LoadFromFile(string filename)
    {
        using (FileStream fs = new FileStream(filename, FileMode.Open))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Portfolio));
            return (Portfolio)serializer.Deserialize(fs);
        }
    }

    public void GenerateReport()
    {
        Console.WriteLine("Trades in Portfolio");
        Console.WriteLine("Ticker\tTrade Date\tBuy/Sell\tQuantity\tPrice\tCost");
        foreach (Trade trade in Trades)
        {
            Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                trade.Ticker, trade.TradeDate.ToShortDateString(), trade.BuySell,
                trade.Quantity, trade.Price.ToString("0.00"), trade.Cost.ToString("0.00"));
        }
    }

     public async Task GeneratePnLReportAsync()
    {
        Console.WriteLine("P&L Report");
        Console.WriteLine("Ticker\tAs Of Date\tCost\tQuantity\tPrice\tMarket Value\tPrev Close\tDaily P&L\tInception P&L");
        try {

            List<string[]> reportData = new List<string[]>();
            reportData.Add(new string[] { "Ticker", "As Of Date", "Cost", "Quantity", "Price", "Market Value", "Prev Close", "Daily P&L", "Inception P&L" });
            foreach (Trade trade in Trades)
            {
                var httpClient = new HttpClient();
                // Retrieve current price and previous close from Alpha Vantage API
                string apiKey = "72XO2JWCUYSW43OU";
                string apiUrl = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={trade.Ticker}&interval=60min&apikey={apiKey}";
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
    
                    // If the request is successful
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        JsonDocument doc = JsonDocument.Parse(responseBody);
                        JsonElement root = doc.RootElement;
                        JsonElement timeSeries = root.GetProperty("Time Series (60min)");

                        foreach (JsonProperty date in timeSeries.EnumerateObject())
                        {
                            JsonElement data = date.Value;
                            string open = data.GetProperty("1. open").GetString();
                            string high = data.GetProperty("2. high").GetString();
                            string low = data.GetProperty("3. low").GetString();
                            string close = data.GetProperty("4. close").GetString();
                            string volume = data.GetProperty("5. volume").GetString();

                        // Console.WriteLine("{0}: {1}, {2}, {3}, {4}, {5}", date.Name, open, high, low, close, volume);
                        }

                    
                        var currentDateTime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd 20:00:00");
                        var previousDateTime = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd 20:00:00");
                        // Console.WriteLine(timeSeries.GetProperty(currentDateTime));
                        decimal currentPrice = decimal.Parse(timeSeries.GetProperty(currentDateTime).GetProperty("4. close").GetString());
                        decimal prevClose = decimal.Parse(timeSeries.GetProperty(previousDateTime).GetProperty("4. close").GetString());
                        trade.CurrentPrice = currentPrice;
                        trade.PrevClose = prevClose;
                        trade.DailyPL = (currentPrice - prevClose) * trade.Quantity;
                        trade.InceptionPL = (currentPrice - trade.Price) * trade.Quantity;
                        var marketValue = trade.CurrentPrice * trade.Quantity;

                        var reportDate = DateTime.UtcNow.AddDays(-1).ToString("dd/MM/yyyy");
                        reportData.Add(   new string[] { trade.Ticker, reportDate, trade.Cost.ToString("0.00"), trade.Quantity.ToString(), trade.CurrentPrice.ToString("0.00"), marketValue.ToString("0.00"), trade.PrevClose.ToString("0.00"), trade.DailyPL.ToString("0.00"), trade.InceptionPL.ToString("0.00") });

                        
                        // Print trade details
                        Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                        trade.Ticker, reportDate,  trade.Cost.ToString("0.00"),
                        trade.Quantity, trade.CurrentPrice.ToString("0.00"),
                        marketValue.ToString("0.00"), trade.PrevClose.ToString("0.00"),
                        trade.DailyPL.ToString("0.00"), trade.InceptionPL.ToString("0.00"));
                    }
            

            }   
            string filePath = "PnLReport.csv";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (string[] row in reportData)
                {
                    string line = string.Join(",", row);
                    writer.WriteLine(line);
                }
            }

            Console.WriteLine("File saved to " + filePath);
        }
        catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }


}

public class Program
{

    public static async Task Main()
    {
        // Portfolio portfolio = new Portfolio();
        // portfolio.AddTrade(new Trade() { Ticker = "MSFT", TradeDate = new DateTime(2018, 2, 1), BuySell = "Buy", Quantity = 100, Price = 85.95m, Cost = 8595.00m });
        // portfolio.AddTrade(new Trade() { Ticker = "GOOG", TradeDate = new DateTime(2018, 2, 1), BuySell = "Buy", Quantity = 50, Price = 1065.00m, Cost = 53250.00m });
        // portfolio.AddTrade(new Trade() { Ticker = "MSFT", TradeDate = new DateTime(2018, 10, 1), BuySell = "Buy", Quantity = 150, Price = 87.82m, Cost = 13173.00m });

        string filename = "portfolio.xml";
        // portfolio.SaveToFile(filename);

        Portfolio loadedPortfolio = Portfolio.LoadFromFile(filename);
        loadedPortfolio.GenerateReport();
        await loadedPortfolio.GeneratePnLReportAsync();

       

    }
}
