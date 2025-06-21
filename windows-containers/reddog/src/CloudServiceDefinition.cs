using Microsoft.ServiceHosting.Tools.ServiceDescriptionSchema;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CloudServiceBootstrapper
{
    public static class CloudServiceDefinition
    {
        public static ServiceDefinition OpenServiceDefinitionFile(string csdefFile)
        {
            XmlSerializer serviceDefinitionSerializer = new ServiceDefinitionSerializer();
            using (var stream = File.OpenRead(csdefFile))
            {
                return (ServiceDefinition)serviceDefinitionSerializer.Deserialize(stream);
            }
        }

        public static Dictionary<string, IEnumerable<string>> GetRoleSettings(this ServiceDefinition csdef)
        {
            Dictionary<string, IEnumerable<string>> csdefSettings = new Dictionary<string, IEnumerable<string>>();

            foreach (var role in csdef.WebRole)
            {
                csdefSettings.Add(role.name, role.ConfigurationSettings.Select(setting => setting.name));
            }

            foreach (var role in csdef.WorkerRole)
            {
                csdefSettings.Add(role.name, role.ConfigurationSettings.Select(setting => setting.name));
            }

            return csdefSettings;
        }
    }
}

