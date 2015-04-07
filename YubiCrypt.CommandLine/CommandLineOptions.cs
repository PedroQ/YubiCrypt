using CommandLine;
using CommandLine.Text;

namespace YubiCrypt.CommandLineTool
{
    class CommonSubOptions
    {
        [Option('v', "verbose")]
        public bool Verbose { get; set; }
    }

    class ProvisionSubOptions : CommonSubOptions
    {

    }

    class EraseSubOptions : CommonSubOptions
    {
        [Option('s', "slot", Required = true)]
        public int Slot { get; set; }
    }

    class HMACTestSubOptions : CommonSubOptions
    {
        [Option('s', "slot", Required = true)]
        public int Slot { get; set; }

        [Option('e', "erase")]
        public bool EraseAfterCompleted { get; set; }
    }

    class CommandLineOptions
    {
        [VerbOption("provision", HelpText = "Enable the Connected Yubikey to be used with the YubiCrypt environment.")]
        public ProvisionSubOptions ProvisionVerb { get; set; }

        [VerbOption("erase", HelpText = "Erase the configuration stored in the specified slot.")]
        public EraseSubOptions EraseVerb { get; set; }

        [VerbOption("hmac-test", HelpText = "Configure the Yubikey with a dummy HMAC key and test it.")]
        public HMACTestSubOptions HMACTestVerb { get; set; }


        [HelpVerbOption]
        public string GetUsage(string verbName)
        {
            var help = HelpText.AutoBuild(this, verbName);
            help.Heading = new HeadingInfo("YubiCrypt Command Line Tools", Program.GetAppVersion().ToString());
            help.Copyright = new CopyrightInfo("Pedro Querido", 2014);
            help.AddPreOptionsLine("RELEASE Lab | Universidade da Beira Interior");
            return help;
        }

    }
}
