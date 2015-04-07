using Semver;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using YubiCrypt.Configurator;

namespace YubiCrypt.CommandLineTool
{
    class Program
    {
        static void Main(string[] args)
        {
            string invokedVerb = null;
            object invokedVerbInstance = null;

            var options = new CommandLineOptions();


            if (!CommandLine.Parser.Default.ParseArguments(args, options,
              (verb, subOptions) =>
              {
                  // if parsing succeeds the verb name and correct instance
                  // will be passed to onVerbCommand delegate (string,object)
                  invokedVerb = verb;
                  invokedVerbInstance = subOptions;
              }))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }


            switch (invokedVerb)
            {
                case "provision":
                    DoProvisioning(invokedVerbInstance);
                    break;
                case "erase":
                    DoErase(invokedVerbInstance);
                    break;
                case "hmac-test":
                    PerformCRTest(invokedVerbInstance);
                    break;
                default:
                    break;
            }
            if (Debugger.IsAttached)
                Console.ReadLine();
        }

        private static void DoErase(object invokedVerbInstance)
        {
            var eraseOptions = invokedVerbInstance as EraseSubOptions;
            if (eraseOptions == null)
            {
                WriteError("Error parsing the arguments");
                return;
            }
            if (eraseOptions.Slot < 1 || eraseOptions.Slot > 2)
            {
                WriteError("Slot option must either be 1 or 2.");
                return;
            }
            if (Yubikey.EraseConfig(eraseOptions.Slot == 1 ? Yubikey.YkSlot.Slot1 : Yubikey.YkSlot.Slot2))
                WriteSuccess("Successfully erased the configuration stored in Slot #{0}", eraseOptions.Slot);
            else
                WriteError("An error occurred while erasing the configuration stored in Slot #{0}", eraseOptions.Slot);
        }

        private static void DoProvisioning(object invokedVerbInstance)
        {
            var provisionSubOptions = invokedVerbInstance as ProvisionSubOptions;
            if (provisionSubOptions == null)
            {
                WriteError("Error parsing the arguments");
                return;
            }

            WriteWarning("Yubikey slots 1 and 2 will be erased! Type 'continue' to proceed.");
            if (!Console.ReadLine().Equals("continue", StringComparison.InvariantCultureIgnoreCase))
                return;

            YubikeyConfigurator ykConfigurator = new YubikeyConfigurator();
            ykConfigurator.WriteError = WriteError;
            ykConfigurator.WriteInfo = WriteInfo;
            ykConfigurator.WriteSuccess = WriteSuccess;
            ykConfigurator.WriteWarning = WriteWarning;
            ykConfigurator.Provision(true);

        }

        private static void PerformCRTest(object invokedVerbInstance)
        {
            var cmdOptions = invokedVerbInstance as HMACTestSubOptions;
            if (cmdOptions == null)
            {
                WriteError("Error parsing the arguments");
                return;
            }

            if (cmdOptions.Slot < 1 || cmdOptions.Slot > 2)
            {
                WriteError("Slot option must either be 1 or 2.");
                return;
            }

            WriteWarning("Slot 1 will be erased! Type 'continue' to proceed.");
            if (!Console.ReadLine().Equals("continue", StringComparison.InvariantCultureIgnoreCase))
                return;

            WriteInfo("Starting test...", cmdOptions);

            string testKey = "ffffffffffffffffffffffffffffffffffffffff";
            string testMessage = "Hello World";
            byte[] testMessageBytes = ASCIIEncoding.ASCII.GetBytes(testMessage);

            WriteInfo("Key (Hex): {0}", testKey);
            WriteInfo("  Message: {0}", testMessage);

            WriteInfo("Writing dummy key to slot 1...", cmdOptions);

            if (!Yubikey.WriteTestChallengeResponse())
            {
                WriteError("Failed to write dummy key to the Yubikey.");
                return;
            }

            WriteInfo("Sending challenge to Yubikey...", cmdOptions);

            byte[] ykResponse;
            var ykSuccess = Yubikey.YkChallengeResponse(testMessageBytes, out ykResponse, Yubikey.YkSlot.Slot1);
            if (!ykSuccess)
            {
                WriteError("Failed to get a response from the Yubikey.");
                return;
            }

            var ykhash = BitConverter.ToString(ykResponse);

            WriteInfo("Got response: {0}", cmdOptions, ykhash);

            WriteInfo("Generating HMAC locally...", cmdOptions);

            var h = System.Security.Cryptography.HMACSHA1.Create();
            h.Key = StringToByteArray(testKey);
            var hash = h.ComputeHash(testMessageBytes);
            var final = BitConverter.ToString(hash);

            WriteInfo("Got response: {0}", cmdOptions, final);

            bool areEqual = ykhash.Equals(final);

            if (areEqual)
                WriteSuccess("Test OK!");
            else
            {
                WriteError("Test Fail!");
                WriteError("Yubikey HMAC: {0}", ykhash);
                WriteError("Local HMAC: {0}", final);
            }

            if (cmdOptions.EraseAfterCompleted)
            {
                WriteInfo("Erasing Slot 1 config...", cmdOptions);
                Yubikey.EraseConfig(Yubikey.YkSlot.Slot1);
                WriteInfo("Done.", cmdOptions);
            }
        }

        private static void WriteWarning(string message, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: " + string.Format(message, args));
            Console.ResetColor();
        }

        private static void WriteError(string message, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + string.Format(message, args));
            Console.ResetColor();
        }
        private static void WriteSuccess(string message, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Format(message, args));
            Console.ResetColor();
        }

        private static void WriteInfo(string message, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Info: " + string.Format(message, args));
            Console.ResetColor();
        }

        private static void WriteInfo(string message, CommonSubOptions cmdOptions, params object[] args)
        {
            if (cmdOptions.Verbose)
                WriteInfo(message, args);
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private static string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        internal static SemVersion GetAppVersion()
        {
            var attribute = (AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true).Single();
            return SemVersion.Parse(attribute.InformationalVersion);
        }
    }
}
