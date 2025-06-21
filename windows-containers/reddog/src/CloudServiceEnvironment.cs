using Microsoft.ServiceHosting.Tools.DevelopmentFabric;
using Microsoft.SqlServer.Server;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace CloudServiceBootstrapper
{
    internal class CloudServiceEnvironment
    {
        private static object _lock = new object();
        private static CloudServiceEnvironment _singleton;

        private DevFabric _fabric;

        private CloudServiceEnvironment()
        {
            _fabric = new DevFabric();
            _fabric.ValidationError += OnFabricValidationError;
        }

        private void OnFabricValidationError(object sender, ValidationEventArgs e)
        {
            if (e.Severity == ValidationSeverity.Error)
            {
                Trace.TraceError($"{e.ErrorId} : {e.Message}");
            }
            else
            {
                Trace.TraceWarning($"{e.ErrorId} : {e.Message}");
            }
        }

        public static CloudServiceEnvironment Instance 
        { 
            get
            {
                if (_singleton == null)
                {
                    lock (_lock)
                    {
                        if (_singleton == null)
                        {
                            _singleton = new CloudServiceEnvironment();
                        }
                    }
                }

                return _singleton;
            }
        }

        public bool TryGetExistingDeployment(out Deployment deployment)
        {
            deployment = null;
            try
            {
                if (!_fabric.IsDevFabricRunning())
                {
                    Trace.TraceWarning("The compute emulator is not running.");

                    return false;
                }

                deployment = _fabric.GetDeployments().FirstOrDefault();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetEndpointByPort(Deployment deployment, int port, out Uri endpoint)
        {
            endpoint = null;
            try
            {
                foreach (Uri uri in deployment.GetExportedInterfaces())
                {
                    if ((uri.Scheme.Equals("http") || uri.Scheme.Equals("https")) )
                    {
                         endpoint = uri;

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

        public Deployment Deploy(string csxPath, string cscfgFilePath)
        {
            return Run(
                dir: new DirectoryInfo(csxPath),
                serviceConfiguration: new FileInfo(cscfgFilePath),
                launchBrowser: false, 
                paused: false, 
                debugger: "", 
                useIISExpress: false,
                portOverrides: new List<EndPointPortOverride>());
        }

        private Deployment Run(DirectoryInfo dir, FileInfo serviceConfiguration, bool launchBrowser, bool paused, string debugger, bool useIISExpress, List<EndPointPortOverride> portOverrides)
        {
            DeploymentOptions options = new DeploymentOptions(
                autoRestart: true,
                restartAttempts: 10,
                startSuspended: paused,
                clean: false,
                strictContracts: true,
                portOverrides: portOverrides.ToArray());

            if (!string.IsNullOrEmpty(debugger))
            {
                options.RuntimeSimulationProperties.RoleHostDebugger = debugger;
            }

            if (useIISExpress)
            {
                options.RuntimeSimulationProperties.EnableIISExpress();
            }

            Deployment deployment = _fabric.CreateDeployment(dir.FullName, serviceConfiguration.FullName, options);
            Trace.TraceInformation("Created: {0}", deployment);

            deployment.Start();

            return deployment;
        }
    }
}
