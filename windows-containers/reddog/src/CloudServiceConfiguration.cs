using Microsoft.ServiceHosting.Tools.ServiceConfigurationSchema;
using Microsoft.ServiceHosting.Tools.ServiceDescriptionSchema;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Microsoft.Web.Hosting.CloudServices
{
    public static class CloudServiceConfiguration
    {
        public static ServiceConfiguration OpenServiceConfigurationFile(string configFile)
        {
            XmlSerializer serviceConfigurationSerializer = new ServiceConfigurationSerializer();
            using (var stream = File.OpenRead(configFile))
            {
                return (ServiceConfiguration)serviceConfigurationSerializer.Deserialize(stream);
            }
        }

        public static void SaveServiceConfigurationFile(ServiceConfiguration config, string configFile)
        {
            XmlSerializer serviceConfigurationSerializer = new ServiceConfigurationSerializer();
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }

            using (var stream = File.OpenWrite(configFile))
            {
                serviceConfigurationSerializer.Serialize(stream, config);
            }
        }

        public static ServiceConfiguration FromServiceDefinition(ServiceDefinition csdef, IDictionary<string, string> appSettings = null)
        {
            IDictionary<string, string> settings = appSettings ?? new Dictionary<string, string>();

            List<RoleSettings> cscfgRoles = new List<RoleSettings>();
            foreach (WebRole csdefWebRole in csdef.WebRole)
            {
                cscfgRoles.Add(new RoleSettings()
                {
                    name = csdefWebRole.name,
                    Instances = new TargetSetting() { count = 1 },
                    ConfigurationSettings = csdefWebRole.ConfigurationSettings.Select(item => new ServiceHosting.Tools.ServiceConfigurationSchema.ConfigurationSetting()
                    {
                        name = item.name,
                        value = settings.ContainsKey(item.name) ? settings[item.name] : string.Empty
                    }).ToArray()
                });
            }

            foreach (WorkerRole csdefWorkerRole in csdef.WorkerRole)
            {
                cscfgRoles.Add(new RoleSettings()
                {
                    name = csdefWorkerRole.name,
                    Instances = new TargetSetting() { count = 1 },
                    ConfigurationSettings = csdefWorkerRole.ConfigurationSettings.Select(item => new ServiceHosting.Tools.ServiceConfigurationSchema.ConfigurationSetting()
                    {
                        name = item.name,
                        value = settings.ContainsKey(item.name) ? settings[item.name] : string.Empty
                    }).ToArray()
                });
            }

            ServiceConfiguration cscfg = new ServiceConfiguration()
            {
                serviceName = csdef.name,
                Role = cscfgRoles.ToArray()
            };

            return cscfg;
        }
    }
}
