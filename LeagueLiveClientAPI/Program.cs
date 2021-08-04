using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace LeagueLiveClientAPI
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        static List<float> currentHealthPoints = new List<float>(); // Store current health data points
        static List<float> maxHealthPoints = new List<float>(); // Store max health data points

        private static int TICKS_PER_SECOND = 4;

        static async Task Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true; // Disable web certificate validation as League's local server is insecure

            try
            { 
                while (currentHealthPoints.Count < 3600*TICKS_PER_SECOND) // Run for 60 minutes or until game closes
                {
                    Console.SetCursorPosition(0, 0);
                    var response = await client.GetAsync("https://127.0.0.1:2999/liveclientdata/activeplayer"); // Request data
                    string json = await response.Content.ReadAsStringAsync(); // Extract json
                    var result = JsonConvert.DeserializeObject<JToken>(json); // Deserialize json
                    currentHealthPoints.Add(float.Parse(result["championStats"]["currentHealth"].ToString())); // Add data to lists
                    maxHealthPoints.Add(float.Parse(result["championStats"]["maxHealth"].ToString()));

                    Console.WriteLine("Tracking HP for: " + result["summonerName"].ToString()); // Print info
                    Console.WriteLine("Current HP: " + result["championStats"]["currentHealth"].ToString());
                    Thread.Sleep(1000/TICKS_PER_SECOND); // Sleep for 250ms
                }
            }
            catch(WebException e)
            {
                // Game client has closed! Game is now over.
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            Console.WriteLine("Finished tracking health.");

            GeneratePlot();

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Generates and saves the plot in the same directory as the executable.
        /// </summary>
        static void GeneratePlot()
        {
            // Create the lines
            var currentHealthLine = new OxyPlot.Series.LineSeries()
            {
                Title = $"Current Health",
                Color = OxyColor.FromArgb(150, 0, 255, 0),
                StrokeThickness = 1,
                MarkerSize = 2,
                MarkerType = OxyPlot.MarkerType.None
            };
            var maxHealthLine = new OxyPlot.Series.LineSeries()
            {
                Title = $"Max Health",
                Color = OxyColor.FromArgb(150, 255, 0, 0),
                StrokeThickness = 1,
                MarkerSize = 2,
                MarkerType = OxyPlot.MarkerType.None
            };

            // Add data to the lines
            for (int i = 0; i < currentHealthPoints.Count; i++)
            {
                currentHealthLine.Points.Add(new OxyPlot.DataPoint(i * 1f/TICKS_PER_SECOND, currentHealthPoints[i]));
                maxHealthLine.Points.Add(new OxyPlot.DataPoint(i * 1f/TICKS_PER_SECOND, maxHealthPoints[i]));
            }

            // Create the model and add the line series to it
            var model = new OxyPlot.PlotModel
            {
                Title = $"Current and Max Health"
            };
            model.Series.Add(currentHealthLine);
            model.Series.Add(maxHealthLine);

            // Need a new thread to export the plot
            var thread = new Thread(() =>
            {
                PngExporter.Export(model, "health_plot.png", 6000, 800, OxyColors.White);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(); // Run the thread

            Console.WriteLine("Plot generated.");
        }
    }
}
