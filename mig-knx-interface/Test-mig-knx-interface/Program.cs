using MIG;
using MIG.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Test_mig_knx_interface
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Mig Interface Skelton test APP");

            var migService = new MigService();

            // Load the configuration from systemconfig.xml file
            MigServiceConfiguration configuration;
            // Construct an instance of the XmlSerializer with the type
            // of object that is being deserialized.
            XmlSerializer mySerializer = new XmlSerializer(typeof(MigServiceConfiguration));
            // To read the file, create a FileStream.
            FileStream myFileStream = new FileStream("systemconfig.xml", FileMode.Open);
            // Call the Deserialize method and cast to the object type.
            configuration = (MigServiceConfiguration)mySerializer.Deserialize(myFileStream);

            // Set the configuration and start MIG Service
            migService.Configuration = configuration;
            migService.StartService();

            // Get a reference to the test interface
            var interfaceDomain = "Knx.KnxInterface";
            var migInterface = migService.GetInterface(interfaceDomain);
            migInterface.InterfacePropertyChanged += MigInterface_InterfacePropertyChanged;
            // Test an interface API command programmatically <module_domain>/<module_address>/<command>[/<option_0>[/../<option_n>]]
            // var response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/3/Greet.Hello/Username"));
            // MigService.Log.Debug(response);
            // <module_domain> ::= "Example.InterfaceSkelton"
            // <module_address> ::= "3"
            // <command> ::= "Greet.Hello"
            // <option_0> ::= "Username"
            // For more infos about MIG API see:
            //    http://genielabs.github.io/HomeGenie/api/mig/overview.html
            //    http://genielabs.github.io/HomeGenie/api/mig/mig_api_interfaces.html

            // The same command can be invoked though the WebGateway 
            // http://<server_address>:8080/api/Example.InterfaceSkelton/1/Greet.Hello/Username

            // Test some other interface API command
            /*
            var response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/1.0.0/Control.On"));
            MigService.Log.Debug(response);
            Thread.Sleep(3000);
            response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/1.0.0/Control.Off"));
            MigService.Log.Debug(response);
            */
            //response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/2/Temperature.Get"));
            //MigService.Log.Debug(response);

            //Console.WriteLine("\n[Press Enter to Quit]\n");
            Console.WriteLine("Test Commands : ");
            Console.WriteLine("Adress : Type : Command");
            Console.WriteLine("Adress Example 1.0.0");
            Console.WriteLine("Command On, Off, Up, Down");
            Console.WriteLine("Exit for End");

            while (true)
            {
                var value = Console.ReadLine();

                if (value.ToLower().Equals("exit")) break;

                var parts = value.Split(':');
                if(parts.Length == 2)
                {
                    var address = parts[0].Trim();
                    var command = "";
                    switch (parts[1].Trim())
                    {
                        case "On":
                            command = "Control.On";
                            break;
                        case "Up":
                            command = "Dimmer.Up";
                            break;
                        case "Down":
                            command = "Dimmer.Down";
                            break;
                        default:
                            command = "Control.Off";
                            break;
                    }

                    var response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/" + address + "/" + command));
                    Console.WriteLine(((MIG.ResponseText)response).ResponseValue);
                    MigService.Log.Debug(response);
                }
                else
                {
                    Console.WriteLine("Wrong Command");
                }

            }

        }

        private static void MigInterface_InterfacePropertyChanged(object sender, InterfacePropertyChangedEventArgs args)
        {
            Console.WriteLine(args.EventData.Source + " : " + args.EventData.Value);
        }
    }
}
