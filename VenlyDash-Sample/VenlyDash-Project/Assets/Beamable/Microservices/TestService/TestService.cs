using Beamable.Server;

namespace Beamable.Microservices
{
	[Microservice("TestService")]
	public class TestService : Microservice
	{
		[ClientCallable]
		public void ServerCall()
		{
			// This code executes on the server.
		}
	}
}
