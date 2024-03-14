using Beamable.Server;
using System.Threading.Tasks;
using Venly;
using Venly.Companion;

namespace Beamable.VenlyService
{
	[Microservice("VenlyService")]
	public class VenlyService : VenlyMicroservice
	{
        [ClientCallable]
        public async Task<string> Execute(string request) //Entry Function
        {
            //(optional) Here you can also configure the Endpoint Guard
            //Changing the default behavior of what endpoints are allowed/denied
            VenlyAPI.Backend.AllowEndpoint(VenlyAPI.Wallet.IDs.ExecuteCryptoTokenTransfer);
            VenlyAPI.Backend.AllowEndpoint(VenlyAPI.Wallet.IDs.ExecuteMultiTokenTransfer);
            VenlyAPI.Backend.AllowEndpoint(VenlyAPI.Wallet.IDs.ExecuteNativeTokenTransfer);
            //...

            return await HandleRequest(request); //Handles requests
        }
    }
}
