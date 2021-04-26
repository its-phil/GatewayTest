using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Security;
using Tpm2Lib;

namespace GatewayTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var invalidIotHubAddress = "invalid.azure.devices.com";
            var validGatewayAddress = "localhost";
            var deviceId = "valid-device-id";

            Console.WriteLine("Connecting to the TPM simulator...");
            var tpmDevice = ConnectToTpmSimulator();
            var securityProvider = new SecurityProviderTpmHsm(deviceId, tpmDevice);
            var authenticationMethod = new DeviceAuthenticationWithTpm(deviceId, securityProvider);

            Console.WriteLine("Connecting to invalid IoT Hub address directly...");
            using (var deviceClient = DeviceClient.Create(invalidIotHubAddress, authenticationMethod))
            {
                var cts = new CancellationTokenSource(1000);
                try { await deviceClient.OpenAsync(cts.Token); }
                catch (Exception ex) { Console.WriteLine($"Swallowed expected exception '{ex.Message}'."); }
            }

            Console.WriteLine("Connecting to invalid IoT Hub address via valid gateway...");
            using (var deviceClient = DeviceClient.Create(invalidIotHubAddress, validGatewayAddress, authenticationMethod))
            {
                var cts = new CancellationTokenSource(1000);
                await deviceClient.OpenAsync(cts.Token);
            }
        }

        private static Tpm2Device ConnectToTpmSimulator(string simulatorHost = "127.0.0.1", int simulatorPort = 2321)
        {
            var tpmDevice = new TcpTpmDevice(simulatorHost, simulatorPort);
            tpmDevice.Connect();
            tpmDevice.SetSocketTimeout(10);
            tpmDevice.PowerCycle();

            using (var tpm2 = new Tpm2(tpmDevice))
            {
                tpm2.Startup(Su.Clear);
            }

            return tpmDevice;
        }
    }
}
