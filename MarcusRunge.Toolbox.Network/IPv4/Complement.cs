using MarcusRunge.Toolbox.Network.Helper;
using System.Net;
using System.Net.Sockets;

namespace MarcusRunge.Toolbox.Network.IPv4
{
    /// <summary>
    /// Provides methods for calculating the complement of excluded IPv4 networks.
    /// </summary>
    public static class Complement
    {
        /// <summary>
        /// Gets the allowed networks.
        /// </summary>
        /// <param name="excludedNetworks">The excluded networks.</param>
        /// <returns>A list of allowed networks.</returns>
        public static List<IPNetwork> GetAllowedNetworks(IEnumerable<IPNetwork> excludedNetworks) => ComplementHelper.GetAllowedNetworks(excludedNetworks, AddressFamily.InterNetwork);
    }
}