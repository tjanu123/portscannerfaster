using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PortScanner.Faster
{
    internal class Program
    {
        private static List<ConnectionResult> ConnectionResults = new List<ConnectionResult>();
        private static List<IPAddress> SubnetHosts { get; set; } = new List<IPAddress>();
        private const int MAX_PORT = 65535;
        private const int NUMBER_OF_HOSTS = 254;

        private static async Task Main(string[] args)
        {
            await StartScanningAsync();
            await WriteResultsToFileAsync();
        }

        public static async Task StartScanningAsync()
        {
            var taskResults = new List<Task>();
            var stopWatch = new Stopwatch();

            var clientIp = GetLocalIpAddress();
            for (int i = 1; i <= NUMBER_OF_HOSTS; i++)
            {
                var clientIpBytes = clientIp.GetAddressBytes();
                clientIpBytes[3] = (byte)i;
                SubnetHosts.Add(new IPAddress(clientIpBytes));
            }

            SubnetHosts.Remove(clientIp);

            stopWatch.Start();

            foreach (var ip in SubnetHosts)
            {
                new Thread(() =>
               {
                   for (int port = 0; port <= MAX_PORT; port++)
                   {
                       taskResults.Add(CheckConnectionAsync(ip, port));
                   }
               }).Start();
            }

            await Task.WhenAll(taskResults);

            stopWatch.Stop();
            Console.WriteLine("Time elapsed:" + stopWatch.ElapsedMilliseconds);
        }

        private static async Task CheckConnectionAsync(IPAddress ip, int port)
        {
            using var tcpClient = new TcpClient();
            try
            {
                await tcpClient.ConnectAsync(ip, port);
                Console.WriteLine($"CONNECTED IP: {ip} - port number: {port}");
                ConnectionResults.Add(new ConnectionResult { IpAddress = ip, Port = port });
            }
            catch (SocketException)
            {
                Console.WriteLine($"REJECTED IP: {ip} - port number: {port}");
            }
        }

        public static async Task WriteResultsToFileAsync()
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            await using var writer = new StreamWriter(desktopPath + "\\PortScannerResults.txt");
            foreach (var result in ConnectionResults)
            {
                await writer.WriteLineAsync($"CONNECTED  IP: {result.IpAddress} - port number: {result.Port}");
            }
        }

        private static IPAddress GetLocalIpAddress()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address;
        }
    }

    public class ConnectionResult
    {
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }
    }
}