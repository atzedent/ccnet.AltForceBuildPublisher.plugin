using System.Collections.Generic;
using Exortech.NetReflector;
using ThoughtWorks.CruiseControl.Core.Tasks;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;
using ThoughtWorks.CruiseControl.Core;

namespace CcNet.Publishers
{
	[ReflectorType("altforcebuild")]
	public class AltForceBuildPublisher : ITask
	{
        private readonly ICruiseServerClientFactory factory;
        private string BuildForcerName="BuildForcer";

        public AltForceBuildPublisher()
            : this(new CruiseServerClientFactory())
		{}

        public AltForceBuildPublisher(ICruiseServerClientFactory factory)
		{
			this.factory = factory;
		}

        [ReflectorProperty("project")]
        public string Project;


        [ReflectorProperty("enforcerName", Required = false)]
        public string EnforcerName
        {
            get { return BuildForcerName; }
            set { BuildForcerName = value; }
        }

		[ReflectorProperty("serverUri", Required=false)]
		public string ServerUri = string.Format("tcp://localhost:21234/{0}", RemoteCruiseServer.ManagerUri);
		
		[ReflectorProperty("integrationStatus", Required=false)]
		public IntegrationStatus IntegrationStatus = IntegrationStatus.Success;

        [ReflectorProperty("security", Required = false)]
        public NameValuePair[] SecurityCredentials { get; set; }

        [ReflectorProperty("parameters", Required = false)]
        public NameValuePair[] Parameters { get; set; }

        [ReflectorProperty("ignoreIntegrationStatus", Required = false)]
        public bool IgnoreIntegrationStatus { get; set; }

        /// <summary>
        /// The logger to use.
        /// </summary>
        public ILogger Logger { get; set; }

        public void Run(IIntegrationResult result)
        {
            if (IntegrationStatus != result.Status && !IgnoreIntegrationStatus) return;

            if (IgnoreIntegrationStatus && result.Status == IntegrationStatus.Cancelled) return;

            result.BuildProgressInformation.SignalStartRunTask("Running for build publisher");

            var logger = Logger ?? new DefaultLogger();
            var loggedIn = false;
            logger.Debug("Generating client for url '{0}'", ServerUri);
            var client = factory.GenerateClient(ServerUri);
            if ((SecurityCredentials != null) && (SecurityCredentials.Length > 0))
            {
                logger.Debug("Logging in");
                client.Login(new List<NameValuePair>(SecurityCredentials));
                loggedIn = true;
            }

            ProjectIntegratorState remoteProjectStatus = ProjectIntegratorState.Unknown;

            foreach (var projectStatus in client.GetProjectStatus())
                if (projectStatus.Name == Project) remoteProjectStatus = projectStatus.Status;

            if (remoteProjectStatus != ProjectIntegratorState.Running) return;

            logger.Info("Sending ForceBuild request to '{0}' on '{1}'", Project, ServerUri);
            client.ForceBuild(Project, new List<NameValuePair>(Parameters ?? new NameValuePair[0]));
            if (loggedIn)
            {
                logger.Debug("Logging out");
                client.Logout();
            }
        }
    }
}