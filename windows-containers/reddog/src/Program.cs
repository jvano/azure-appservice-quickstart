using Microsoft.ServiceHosting.Tools.DevelopmentFabric;
using Microsoft.ServiceHosting.Tools.ServiceConfigurationSchema;
using Microsoft.ServiceHosting.Tools.ServiceDescriptionSchema;
using Microsoft.Web.Hosting.CloudServices;
using System;
using System.Diagnostics;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace CloudServiceBootstrapper
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(listener: new ConsoleTraceListener());

            Trace.TraceInformation("Cloud Service Bootstrapper by Joaquin Vano");

            // Register handlers to detect gracefull shutdown.
            Handlers.SetHandler();

            string localPath = @"C:\.local\reddog";
            if (Directory.Exists(localPath))
            {
                Directory.Delete(localPath, true);
            }

            Directory.CreateDirectory(localPath);

            string localCsPkgFilePath = Path.Combine(localPath, "app.cskg");
            string remoteCsPkgFilePath = Directory.GetFiles(Environment.ExpandEnvironmentVariables(@"%home%\site\wwwroot"), "*.cspkg", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (remoteCsPkgFilePath == null)
            {
                Trace.TraceInformation(@"Unable to find a cspkg file in %home%\site\wwwroot");

                while(!Handlers.HasBeenSignaled)
                {
                    // Block this thread to avoid the container to exit as we will display the parking page
                    Thread.Sleep(5);
                }

                return 0;
            }

            File.Copy(remoteCsPkgFilePath, localCsPkgFilePath);

            Trace.TraceInformation($"CsPkg has been successfully copied to {localCsPkgFilePath}");

            CloudServicePackage.ExtractFromCspkg(localCsPkgFilePath, localPath);

            Trace.TraceInformation($"CsPkg has been successfully extracted to {localPath}");

            // Read csdef
            ServiceDefinition csdef = CloudServiceDefinition.OpenServiceDefinitionFile(csdefFile: Path.Combine(localPath, "ServiceDefinition.csdef"));

            foreach (var webrole in csdef.WebRole)
            {
                Trace.TraceInformation($"WebRole {webrole.name} detected!");
                foreach(var endpoint in webrole.Endpoints.InputEndpoint)
                {
                    Trace.TraceInformation($"InputEndpoint port: {endpoint.port}");
                }
            }

            foreach (var workerrole in csdef.WorkerRole)
            {
                Trace.TraceInformation($"WebRole {workerrole.name} detected!");
            }

            // Generate cscfg from csdef and override values using appsettings
            // NOTE: Non-matching keys won't be added as this will break the csdef if keys were not previously defined.
            IDictionary<string, string> appSettings = FilterEnvironmentVariables(prefix: "APPSETTING_");
            ServiceConfiguration cscfg = CloudServiceConfiguration.FromServiceDefinition(csdef, appSettings);

            string localCsCfgFilePath = Path.Combine(localPath, "app.cfg");
            CloudServiceConfiguration.SaveServiceConfigurationFile(cscfg, configFile: Path.Combine(localPath, "app.cfg"));

            // Run deployment
            Deployment deployment = CloudServiceEnvironment.Instance.Deploy(localPath, localCsCfgFilePath);

            // While stabilizing the deployment
            while (!Handlers.HasBeenSignaled && !deployment.WaitForState(RoleInstanceStatus.Started, System.TimeSpan.FromSeconds(10)))
            {
                Trace.TraceWarning("Cloud Service is still provisioning...");

                var roles = deployment.GetRoles();
                Trace.TraceInformation("Status:");

                foreach (var role in roles)
                {
                    Trace.TraceInformation("  {0}", role.Name);

                    foreach (var ri in role.GetRoleInstances())
                    {
                        Trace.TraceInformation("    {0} {1} (ProcessId {2})", ri.InstanceId, ri.State, ri.ProcessId);
                    }
                }
            }

            // If started, let's finish the provisioning
            if (!Handlers.HasBeenSignaled)
            {
                // If PORT has been configured in App Service, then let's remove the default web site
                int port;
                if (TryGetEnvironmentVariable<int>("PORT", out port))
                {
                    Trace.TraceInformation($"PORT: {port}");
                    Process.Start(@"C:\Windows\system32\inetsrv\appcmd.exe", "delete site \"Default Web Site\"").WaitForExit();                    
                    Process.Start(@"C:\Windows\System32\iisreset.exe").WaitForExit();
                }

                foreach(var info in deployment.GetExportedInterfacesInformation())
                {
                    Trace.TraceInformation($"Exported interface for role {info.RoleName}. Endpoint: {info.RoleEndpointName}: {info.Uri}");
                }

                // If PORT has been configured in App Service
                if (port > 0)
                {
                    Trace.TraceInformation($"Cloud Service Input Endpoint Port: {port}");

                    Uri endpoint;
                    if (CloudServiceEnvironment.Instance.TryGetEndpointByPort(deployment, port, out endpoint))
                    {
                        IPAddress ip = GetLocalIPAddress();

                        Trace.TraceInformation("Configuring network forwarding...");
                        Process.Start(@"C:\Windows\System32\netsh.exe", $"interface portproxy add v4tov4 listenaddress={ip} listenport={port} connectaddress={endpoint.Host} connectport={endpoint.Port}").WaitForExit();                   
                        Trace.TraceInformation("Network forwarding has been configured!");
                    }
                }

                Trace.TraceInformation("Cloud Service has started successfully");
            }

            // Block this thread to avoid the container to exit.
            while(!Handlers.HasBeenSignaled)
            {                
                Thread.Sleep(5);
            }

            // Try to gracefully shutdonw the cloud service
            if (CloudServiceEnvironment.Instance.TryGetExistingDeployment(out deployment) && deployment != null)
            {
                Trace.TraceInformation("Stopping Cloud Service instance...");

                deployment.Stop();

                while (!deployment.WaitForAnyState(System.TimeSpan.FromSeconds(5),
                    RoleInstanceStatus.Stopped,
                    RoleInstanceStatus.Aborted,
                    RoleInstanceStatus.Destroyed,
                    RoleInstanceStatus.Suspended))
                {
                    Trace.TraceWarning("Cloud Service is stopping...");
                }

                Trace.TraceInformation("Cloud Service has stopped!");
            }

            Trace.TraceInformation("Cloud Service Bootstrapper is exiting...");

            return 0;
        }

        private static IDictionary<string, string> FilterEnvironmentVariables(string prefix)
        {
            Dictionary<string, string> filteredVariables = new Dictionary<string, string>();

            IDictionary environmentVariables = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry environmentVariable in environmentVariables)
            {
                string key = environmentVariable.Key.ToString();
                if (key.StartsWith(prefix))
                {
                    string newKey = key.Substring(prefix.Length);
                    filteredVariables[newKey] = environmentVariable.Value.ToString();
                }
            }

            return filteredVariables;
        }

        private static bool TryGetEnvironmentVariable<T>(string name, out T value)
        {
            value = default;
            try
            {
                IDictionary environmentVariables = Environment.GetEnvironmentVariables();
                if (environmentVariables.Contains(name))
                {
                    object obj = environmentVariables[name];
                    if (obj != null)
                    {
                        value = (T)Convert.ChangeType(obj, typeof(T));

                        return true;
                    }
                }
            }
            catch
            {
                // DO NOTHING
            }

            return false;
        }

        private static IPAddress GetLocalIPAddress()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return address;
                }
            }

            return null;
        }
    }
}
