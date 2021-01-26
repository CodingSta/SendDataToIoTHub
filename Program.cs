
using Microsoft.Azure.Devices.Client;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimulatedDevice
{
    internal class Program
    {
        private static DeviceClient s_deviceClient;
        private static readonly TransportType s_transportType = TransportType.Mqtt;
        private static string s_connectionString = "HostName=mingyuHub.azure-devices.net;DeviceId=device01;SharedAccessKey=TZnqr611U4ms31DDB1uC9H9Esm/rOeCJ6K8XXXOyZzU=";
        private static TimeSpan s_telemetryInterval = TimeSpan.FromSeconds(1); // Seconds

        private static async Task Main(string[] args)
        {
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);
            // Method 등록
            await s_deviceClient.SetMethodHandlerAsync("doThat", doThat, null);
            Console.WriteLine("Press control-C to exit.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };
            await SendDeviceToCloudMessagesAsync(cts.Token);
            s_deviceClient.Dispose();
            Console.WriteLine("Device simulator finished.");
        }
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken ct)
        {
            double minTemperature = 20;
            double minHumidity = 60;
            var rand = new Random();

            while (!ct.IsCancellationRequested)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;
                string messageBody = JsonSerializer.Serialize(
                    new
                    {
                        temperature = currentTemperature,
                        humidity = currentHumidity,
                    });
                using var message = new Message(Encoding.ASCII.GetBytes(messageBody))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8",
                };
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine($"{DateTime.Now} > Sending message: {messageBody}");
                await Task.Delay(1000);
            }
        }

        private static Task<MethodResponse> doThat(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("Do That!");
            var data = Encoding.UTF8.GetString(methodRequest.Data);
            if (int.TryParse(data, out int telemetryIntervalInSeconds))
            {
                s_telemetryInterval = TimeSpan.FromSeconds(telemetryIntervalInSeconds);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Telemetry interval set to {s_telemetryInterval}");
                Console.ResetColor();
                string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            else
            {
                string result = "{\"result\":\"Invalid parameter\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }
    }
}
