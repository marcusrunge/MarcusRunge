using System.Net;

namespace MarcusRunge.Toolbox.Network.Test
{
    /// <summary>
    /// Tests for the complement of excluded IPv4 networks.
    /// </summary>
    public class IPv4ComplementTest
    {
        /// <summary>
        /// Gets the allowed networks result does not contain excluded boundary addresses.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_ResultDoesNotContainExcludedBoundaryAddresses()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [IPNetwork.Parse("10.0.0.0/8")]);

            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("10.0.0.0")));
            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("10.255.255.255")));

            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("9.255.255.255")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("11.0.0.0")));
        }

        /// <summary>
        /// Gets the allowed networks with adjacent excluded networks merges them for complement calculation.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithAdjacentExcludedNetworks_MergesThemForComplementCalculation()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("10.0.0.0/8"),
                    IPNetwork.Parse("11.0.0.0/8")
                ]);

            AssertNetworksEqual(
                [
                    "0.0.0.0/5",
                    "8.0.0.0/7",
                    "12.0.0.0/6",
                    "16.0.0.0/4",
                    "32.0.0.0/3",
                    "64.0.0.0/2",
                    "128.0.0.0/1"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with duplicate excluded networks ignores duplicates.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithDuplicateExcludedNetworks_IgnoresDuplicates()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("10.0.0.0/8"),
                    IPNetwork.Parse("10.0.0.0/8")
                ]);

            AssertNetworksEqual(
                [
                    "0.0.0.0/5",
                    "8.0.0.0/7",
                    "11.0.0.0/8",
                    "12.0.0.0/6",
                    "16.0.0.0/4",
                    "32.0.0.0/3",
                    "64.0.0.0/2",
                    "128.0.0.0/1"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with the entire IPv4 address space excluded returns no networks.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithEntireIPv4AddressSpaceExcluded_ReturnsNoNetworks()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [IPNetwork.Parse("0.0.0.0/0")]);

            Assert.Empty(actual);
        }

        /// <summary>
        /// Gets the allowed networks with first half excluded returns second half.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithFirstHalfExcluded_ReturnsSecondHalf()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [IPNetwork.Parse("0.0.0.0/1")]);

            AssertNetworksEqual(
                ["128.0.0.0/1"],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with i PV6 excluded network ignores it.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithIPv6ExcludedNetwork_IgnoresIt()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [IPNetwork.Parse("fc00::/7")]);

            AssertNetworksEqual(
                ["0.0.0.0/0"],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with mixed i PV4 and i PV6 excluded networks uses only i PV4 exclusions.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithMixedIPv4AndIPv6ExcludedNetworks_UsesOnlyIPv4Exclusions()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("10.0.0.0/8"),
                    IPNetwork.Parse("fc00::/7")
                ]);

            AssertNetworksEqual(
                [
                    "0.0.0.0/5",
                    "8.0.0.0/7",
                    "11.0.0.0/8",
                    "12.0.0.0/6",
                    "16.0.0.0/4",
                    "32.0.0.0/3",
                    "64.0.0.0/2",
                    "128.0.0.0/1"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with nested excluded network does not add parent network back.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithNestedExcludedNetwork_DoesNotAddParentNetworkBack()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("10.0.0.0/8"),
                    IPNetwork.Parse("10.1.0.0/16")
                ]);

            AssertNetworksEqual(
                [
                    "0.0.0.0/5",
                    "8.0.0.0/7",
                    "11.0.0.0/8",
                    "12.0.0.0/6",
                    "16.0.0.0/4",
                    "32.0.0.0/3",
                    "64.0.0.0/2",
                    "128.0.0.0/1"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with no excluded networks returns entire i PV4 address space.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithNoExcludedNetworks_ReturnsEntireIPv4AddressSpace()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks([]);

            AssertNetworksEqual(
                ["0.0.0.0/0"],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with second half excluded returns first half.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithSecondHalfExcluded_ReturnsFirstHalf()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [IPNetwork.Parse("128.0.0.0/1")]);

            AssertNetworksEqual(
                ["0.0.0.0/1"],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with single host excluded does not return network containing that host.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithSingleHostExcluded_DoesNotReturnNetworkContainingThatHost()
        {
            IPAddress excludedHost = IPAddress.Parse("192.0.2.1");

            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [IPNetwork.Parse("192.0.2.1/32")]);

            Assert.DoesNotContain(actual, network => network.Contains(excludedHost));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("192.0.2.0")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("192.0.2.2")));
        }

        /// <summary>
        /// Gets the allowed networks with single slash8 excluded returns minimal complement networks.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithSingleSlash8Excluded_ReturnsMinimalComplementNetworks()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [IPNetwork.Parse("10.0.0.0/8")]);

            AssertNetworksEqual(
                [
                    "0.0.0.0/5",
                    "8.0.0.0/7",
                    "11.0.0.0/8",
                    "12.0.0.0/6",
                    "16.0.0.0/4",
                    "32.0.0.0/3",
                    "64.0.0.0/2",
                    "128.0.0.0/1"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with unsorted excluded networks returns correct complement.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithUnsortedExcludedNetworks_ReturnsCorrectComplement()
        {
            List<IPNetwork> actual = IPv4.Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("192.168.0.0/16"),
                    IPNetwork.Parse("10.0.0.0/8"),
                    IPNetwork.Parse("172.16.0.0/12")
                ]);

            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("10.0.0.1")));
            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("172.16.0.1")));
            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("192.168.0.1")));

            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("8.8.8.8")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("11.0.0.1")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("172.15.255.255")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("172.32.0.0")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("192.167.255.255")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("192.169.0.0")));
        }

        private static void AssertNetworksEqual(
            string[] expectedNetworks,
            IReadOnlyCollection<IPNetwork> actualNetworks)
        {
            string[] actual = [.. actualNetworks.Select(network => network.ToString())];

            Assert.Equal(expectedNetworks, actual);
        }
    }
}