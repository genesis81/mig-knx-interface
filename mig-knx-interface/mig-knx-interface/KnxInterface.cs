using KNXLib;
using MIG;
using MIG.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MIG.Interfaces.Knx
{
    public class KnxInterface : MigInterface
    {

        public enum Commands
        {
            NotSet,
            Control_On,
            Control_Off,
            Dimmer_Up,
            Dimmer_Down
        }

        private static KnxConnection _connection;
        private bool _connected = false;
        private List<InterfaceModule> _modules;


        public KnxInterface()
        {
            _modules = new List<InterfaceModule>();
            XmlDocument doc = new XmlDocument();
            doc.Load("groupaddresses.xml");

            var addresses = doc.GetElementsByTagName("GroupAddress");

            foreach(XmlNode item in addresses)
            {
                var attributes = item.Attributes;
                
                var address = attributes["Address"].Value.ToString();
                var name = attributes["Name"].Value.ToString();
                if (attributes["DPTs"] == null) continue;
                var dpt = attributes["DPTs"].Value.ToString().Split('-');
                var maintype = int.Parse(dpt[1]);
                var subtype = int.Parse(dpt[2]);

                var module = new InterfaceModule();
                module.Domain = this.GetDomain();
                module.Address = address.ToString().Replace("/",".");
                module.Description = name;

                switch (maintype)
                {
                    case 1:
                        switch(subtype)
                        {
                            case 1:
                            case 2:
                            case 3:
                                module.ModuleType = ModuleTypes.Switch;
                                break;
                        }

                        break;
                    case 2:
                        module.ModuleType = ModuleTypes.Generic;
                        break;
                    case 3:
                        module.ModuleType = ModuleTypes.Dimmer;
                        break;
                    case 4:
                        module.ModuleType = ModuleTypes.Generic;
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        module.ModuleType = ModuleTypes.Sensor;
                        break;
                    case 9:
                        switch (subtype)
                        {
                            case 1:
                            case 2:
                            case 3:
                                module.ModuleType = ModuleTypes.Temperature;
                                break;
                            default:
                                module.ModuleType = ModuleTypes.Sensor;
                                break;
                        }
                        break;
                    default:
                        module.ModuleType = ModuleTypes.Generic;
                        break;
                }
                _modules.Add(module);
            }

        }


        #region MIG Interface members
        public bool IsConnected
        {
            get
            {
                return _connected;
            }
        }

        public bool IsEnabled { get; set; }

        public List<Option> Options { get; set; }

        public event InterfaceModulesChangedEventHandler InterfaceModulesChanged;
        public event InterfacePropertyChangedEventHandler InterfacePropertyChanged;

        public bool Connect()
        {
            _connection = new KnxConnectionTunneling("192.168.10.101", 3671, "192.168.10.140", 3671) { Debug = false };
            _connection.KnxConnectedDelegate += Connected;
            _connection.KnxDisconnectedDelegate += Disconnected;
            _connection.KnxEventDelegate += Event;
            _connection.KnxStatusDelegate += Status;
            _connection.Connect();
            OnInterfaceModulesChanged(this.GetDomain());
            return true;
        }

        public void Disconnect()
        {
            if (_connection != null)
            {
                _connection.Disconnect();
                _connection.KnxConnectedDelegate -= Connected;
                _connection.KnxDisconnectedDelegate -= Disconnected;
                _connection.KnxEventDelegate -= Event;
                _connection.KnxStatusDelegate -= Status;
                _connected = false;
            }

        }

        public List<InterfaceModule> GetModules()
        {
            return _modules;
        }

        public object InterfaceControl(MigInterfaceCommand request)
        {
            var response = new ResponseText("OK"); //default success value

            Commands command;
            Enum.TryParse(request.Command.Replace(".", "_"), out command);

            var module = _modules.Find(m => m.Address.Equals(request.Address));

            if (module != null)
            {
                switch (command)
                {
                    case Commands.Control_On:
                        _connection.Action(module.Address.Replace(".", "/"), true);
                        break;
                    case Commands.Control_Off:
                        _connection.Action(module.Address.Replace(".", "/"), false);
                        // TODO: ...
                        break;
                    case Commands.Dimmer_Up:
                        _connection.Action(module.Address.Replace(".", "/"),  _connection.ToDataPoint("3.008", 3));
                        // TODO: ...
                        break;
                    case Commands.Dimmer_Down:
                        _connection.Action(module.Address.Replace(".", "/"), _connection.ToDataPoint("3.008", -3));
                        // TODO: ...
                        break;
                }
            }
            else
            {
                response = new ResponseText("ERROR: invalid module address");
            }

            return response;

        }

        public bool IsDevicePresent()
        {
            //TODO: Anzeigen wenn das Gateway nicht vorhanden ist.
            return true;
        }

        public void OnSetOption(Option option)
        {

        }

        #endregion

        #region KNX.Net Members

        private void Event(string address, string state)
        {
            var module = _modules.Find(m => m.Address.Equals(address.Replace("/", ".")));

            if (module != null)
            {
                switch (module.ModuleType)
                {
                    case ModuleTypes.Light:
                        OnInterfacePropertyChanged(module.Domain, module.Address, "KNX Interface", "Status.Level", state);
                        break;
                    case ModuleTypes.Dimmer:
                        OnInterfacePropertyChanged(module.Domain, module.Address, "KNX Interface", "Status.Level", _connection.FromDataPoint("3.008", state));
                        break;
                    case ModuleTypes.Temperature:
                        OnInterfacePropertyChanged(module.Domain, module.Address, "KNX Interface", "Sensor.Temperature", _connection.FromDataPoint("9.001", state));
                        break;
                    case ModuleTypes.Sensor:
                        OnInterfacePropertyChanged(module.Domain, module.Address, "KNX Interface", "Sensor.Level", _connection.FromDataPoint("9.001", state));
                        break;
                }
            }

        }

        private void Status(string address, string state)
        {

        }

        private void Connected()
        {
            _connected = true;
        }

        private void Disconnected()
        {
            _connected = false;
        }

        #endregion

        #region events

        protected virtual void OnInterfaceModulesChanged(string domain)
        {
            if (InterfaceModulesChanged != null)
            {
                var args = new InterfaceModulesChangedEventArgs(domain);
                InterfaceModulesChanged(this, args);
            }
        }

        protected virtual void OnInterfacePropertyChanged(string domain, string source, string description, string propertyPath, object propertyValue)
        {
            if (InterfacePropertyChanged != null)
            {
                var args = new InterfacePropertyChangedEventArgs(domain, source, description, propertyPath, propertyValue);
                InterfacePropertyChanged(this, args);
            }
        }

        #endregion
    }
}
