using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reflection;


namespace RazorTransform.Util
{
    /// <summary>
    /// A utility class for assigning port numbers.  
    /// </summary>
    public static class PortUtility
    {
        public const string TYPE_SERVER_PORT = "ServerPort";
        public const string TYPE_WEB_PORT = "WebPort";
        /// <summary>
        /// the base path for app deployments
        /// </summary>
        private static string _deploy_base_path = @"..\..";
        /// <summary>
        /// sets the deploy path and rescans config.
        /// </summary>
        /// <param name="path"></param>
        public static void SetDeployBasePath( string path)
        {
            _deploy_base_path = path;
            Init();
        }
        /// <summary>
        /// synchronization primitive
        /// </summary>
        private static object synchRoot = new object();
        /// <summary>
        /// Initialize the PortUtility
        /// </summary>
        public static void Init()
        {


            ScanConfigForReservedPorts();
        }

        /// <summary>
        /// the lower bound for a web port
        /// </summary>
        public const int WEB_PORT_LOWER_THRESHOLD = 6000;
        /// <summary>
        /// the upper bound for a web port
        /// </summary>
        public const int WEB_PORT_UPPER_THRESHOLD = 9000;
        /// <summary>
        /// the lower bound for WCF port
        /// </summary>
        public const int APP_PORT_LOWER_THRESHOLD = 33335;
        /// <summary>
        /// the upper bound for a WCF port
        /// </summary>
        public const int APP_PORT_UPPER_THRESHOLD = 83335;
        /// <summary>
        /// Incrementally scans for the next available web port.  This method will also ensure that the port is not actively being used.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static int GetWebPort(ConfigInfo info = null)
        {
            return GetNextOpenPort(WEB_PORT_LOWER_THRESHOLD, WEB_PORT_UPPER_THRESHOLD, info);
        }
        /// <summary>
        /// Incrementally scans for the next available app server port.  This method will also ensure that the port is not actively being used.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static int GetAppServerPort(ConfigInfo info = null)
        {
            return GetNextOpenPort(APP_PORT_LOWER_THRESHOLD, APP_PORT_UPPER_THRESHOLD, info);
        }
        /// <summary>
        /// REturns a port assignment for config info
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static PortAssignment GetAssignmentForConfig(ConfigInfo info)
        {
           return  _currentPorts.FirstOrDefault(x => info.Arguments.Contains(x.Name));
        }
        /// <summary>
        /// Returns the next open port.  Resolves any port conflicts with the current port list.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        private static int GetNextOpenPort(int lower, int upper, ConfigInfo info = null)
        {
            var conflict = _GetConflicts(info).FirstOrDefault();
            if (conflict!=null)
            {

                
                 for (int i = lower; i <= upper; i++)
                 {
                     if (PortOpen(i))
                     {
                         conflict.Port = i;
                         return i;
                     }
                 }
                 throw new ArgumentOutOfRangeException("Could not locate a port");
            }
            else
            {
                var item = _currentPorts.FirstOrDefault(x => info.Arguments.Contains(x.Name));
                if (item != null)
                {
                    return item.Port;
                }
                else
                {
                    PortAssignment pa = new PortAssignment(info.PropertyName, portIterator(lower, upper));
                    _currentPorts.Add(pa);
                    return pa.Port;
                }
            }

           

        }

        /// <summary>
        /// validates a port for a given range
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        private static bool ValidatePort(int port, int lower, int upper, ConfigInfo info)
        {
            if( port >= lower && port <= upper)
            {
                return PortOpen(port);
            }
            return false;
        }
        /// <summary>
        /// Scans all active ports
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool PortOpen(int port)
        {

            return !ListUsedTCPPort().Any(x => x == port) && !_ports.Any(x=>x.Port==port);

        }
        /// <summary>
        /// List all ports actively in use.  
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<int> ListUsedTCPPort()
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            return ipGlobalProperties.GetActiveTcpListeners().Select(x => x.Port);

        }
        /// <summary>
        /// Determines if the supplied port number is within range and available.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool IsValid(int port, ConfigInfo info)
        {
            switch( info.Type)
            {
                case TYPE_SERVER_PORT :
                    return ValidatePort(port, APP_PORT_LOWER_THRESHOLD, APP_PORT_UPPER_THRESHOLD, info);
                case TYPE_WEB_PORT :
                    return ValidatePort(port, WEB_PORT_LOWER_THRESHOLD, WEB_PORT_UPPER_THRESHOLD, info);
                default: return true;

            }

        }

        public static string GetAppFolder( )
        {
            //the current directory of the executing app.
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); ;// @"\\hs1refapp01\c$\DEPLOY\2012R4REF";// Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        }

        /// <summary>
        /// Iterates over all deployment folders looking for a subset of transformed SetParameters xml files and ultimately extracting
        /// port information.
        /// </summary>
        public static void ScanConfigForReservedPorts()
        {

            _ports = new List<PortAssignment>();

            //the current directory of the executing app.
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); ;// @"\\hs1refapp01\c$\DEPLOY\2012R4REF";// Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


            //get all ports from other instance deployments
            DirectoryInfo instanceDir = new DirectoryInfo(_deploy_base_path);// new DirectoryInfo(@"\\hs1refapp01\c$\DEPLOY\");
            if (instanceDir.Exists)
            {
                foreach (var directory in instanceDir.GetDirectories().Where(x => x.FullName != currentDirectory))
                {
                    AppendPorts(configFileIterator(directory.FullName));

                }
            }
            //load ports for the current deployment
            _currentPorts = new List<PortAssignment>(configFileIterator(currentDirectory));





        }
        /// <summary>
        /// Determines if the current deployment is attempting to reserve ports that have already been reserved by other instances.
        /// </summary>
        /// <returns></returns>
       
        private static IEnumerable<PortAssignment> _GetConflicts(ConfigInfo info)
        {
           
            return _currentPorts.Where(x=>info.Arguments.Contains(x.Name)).Intersect(_ports, new PortCompare());
        }
        /// <summary>
        /// A collection of setParameters files that will be searched for ports.
        /// </summary>
        private static List<string> _ConfigFiles = new List<string>
        {
            "ApplicationServer.SetParameters.xml",
            "Arthur.SetParameters.xml",
            "Bw.SetParameters.xml",
            "Excalibur.SetParameters.xml",
            "Studio.Web.SetParameters.xml",
            "WebService.SetParameters.xml",
            "WebServiceClassic.SetParameters.xml"
        };
        /// <summary>
        /// reserved ports from other instances
        /// </summary>
        private static List<PortAssignment> _ports = new List<PortAssignment>();
        /// <summary>
        /// reserved ports for this instance
        /// </summary>
        private static List<PortAssignment> _currentPorts = new List<PortAssignment>();

        /// <summary>
        /// iterates over a subset of config files looking for ports.
        /// </summary>
        private static Func<string, IEnumerable<PortAssignment>> configFileIterator = (directory) =>
            {
                List<PortAssignment> items = new List<PortAssignment>();
                foreach (var fileName in _ConfigFiles)
                {
                    var configFile = new FileInfo(Path.Combine(directory, fileName));
                    if (configFile.Exists)
                    {
                        var xdoc = XDocument.Load(configFile.FullName);

                        //select attributes containing InstanceName, *Url and *Port
                        var instanceName = xdoc.Root.Descendants("setParameter").FirstOrDefault(x => x.Attribute("name").Value == "InstanceName");
                        var urls = xdoc.Root.Descendants("setParameter")
                            .Where(x=> elementFilter(x));

                        items.AddRange(portFilter(urls.Select(x =>
                        {
                            var e = transformElement(x);
                            if (e == null)
                                return null;

                            if( instanceName != null)
                                e.InstanceName = instanceName.Attribute("value").Value;
                            return e;
                        })));


                    }
                }
                return items;
            };


        /// <summary>
        /// selects an XElement with matching attribute values
        /// </summary>
        private static Func<XElement, bool> elementFilter = (element) =>
            {
                var attribute =  element.Attribute("name");
                if( attribute != null)
                {
                    string val = attribute.Value;
                    if( !String.IsNullOrEmpty(val))
                    {
                        return  val.EndsWith("Url") ||
                            val.EndsWith("Port") ||
                            val == "WebSiteBinding";

                       
                    }
                }
              
                   return  false;
                
              
            };
        /// <summary>
        /// projects a distinct list of ports excluding null ports and port 80.
        /// </summary>
        private static Func<IEnumerable<PortAssignment>, IEnumerable<PortAssignment>> portFilter = (ports) =>
            {
                return ports.Where(x => x != null && x.Port != 80);
            };
        /// <summary>
        /// Transforms an XAttributes value into a UriBuilder.  This is used to extract the port.
        /// </summary>
        private static Func<XElement, PortAssignment> transformElement = (element) =>
            {
                try
                {
                    var name = element.Attribute("name");
                    var val = element.Attribute("value");
                    string url = val.Value.Replace("*", "localhost");
                    return new PortAssignment(name.Value, new UriBuilder(url).Port);
                }
                catch
                {
                    return null;
                }
            };
        /// <summary>
        /// generates a port number contrained by upper and lower.
        /// </summary>
        private static Func<int, int, int> portIterator = (upper, lower) =>
            {
                for (int i = lower; i <= upper; i++)
                {
                    if (!ListUsedTCPPort().Any(x => x == i) &&
                        !_ports.Any(x => x.Port == i))
                    {
                        return i;
                    }
                }
                return upper;
            };
        /// <summary>
        /// A thread safe method for appending a collection of ports to the static ports variable.  This method
        /// excludes duplicates.
        /// </summary>
        /// <param name="ports"></param>
        private static void AppendPorts(IEnumerable<PortAssignment> ports)
        {
            lock (synchRoot)
            {
                if (ports.Any())
                {
                    int count = ports.Count();
                    var diff = ports.Except(_ports,new PortCompare());
                    _ports.AddRange(diff);
                }
            }
        }
    }

    [Serializable()]
    public class PortAssignment
    {

        public string InstanceName { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("port")]
        public int Port { get; set; }

        public PortAssignment()
        {

        }

        public PortAssignment(string name, int port)
        {
            this.Name = name;
            this.Port = port;
        }

        public int GetInstanceNameAsPort()
        {
            StringBuilder b = new StringBuilder(this.InstanceName);
            StringBuilder port = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                if (Char.IsDigit(b[i]))
                {
                    port.Append(b[i]);
                }
            }
            return Int32.Parse(port.ToString());
        }
    }

    public class PortCompare : IEqualityComparer<PortAssignment>
    {
        public bool Equals(PortAssignment x, PortAssignment y)
        {

            return String.Compare(x.Name, y.Name, true) == 0 &&
                    x.Port == y.Port;
        }

        public int GetHashCode(PortAssignment obj)
        {
            return 1;
        }
    }
}
