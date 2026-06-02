using MarcusRunge.Toolbox.Network.IPv6;
using System.Net;

namespace MarcusRunge.Toolbox.Network.Test
{
    /// <summary>
    /// Tests for the complement of excluded IPv6 networks.
    /// </summary>
    public class IPv6ComplementTest
    {
        /// <summary>
        /// Gets the allowed networks result does not contain excluded boundary addresses.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_ResultDoesNotContainExcludedBoundaryAddresses()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [IPNetwork.Parse("fc00::/7")]);

            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("fc00::")));
            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")));

            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("fbff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("fe00::")));
        }

        /// <summary>
        /// Gets the allowed networks with adjacent excluded networks merges them for complement calculation.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithAdjacentExcludedNetworks_MergesThemForComplementCalculation()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("fc00::/8"),
                    IPNetwork.Parse("fd00::/8")
                ]);

            AssertNetworksEqual(
                [
                    "::/1",
                    "8000::/2",
                    "c000::/3",
                    "e000::/4",
                    "f000::/5",
                    "f800::/6",
                    "fe00::/7"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with duplicate excluded networks ignores duplicates.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithDuplicateExcludedNetworks_IgnoresDuplicates()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("fc00::/7"),
                    IPNetwork.Parse("fc00::/7")
                ]);

            AssertNetworksEqual(
                [
                    "::/1",
                    "8000::/2",
                    "c000::/3",
                    "e000::/4",
                    "f000::/5",
                    "f800::/6",
                    "fe00::/7"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with entire i PV6 address space excluded returns no networks.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithEntireIPv6AddressSpaceExcluded_ReturnsNoNetworks()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [IPNetwork.Parse("::/0")]);

            Assert.Empty(actual);
        }

        /// <summary>
        /// Gets the allowed networks with first half excluded returns second half.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithFirstHalfExcluded_ReturnsSecondHalf()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [IPNetwork.Parse("::/1")]);

            AssertNetworksEqual(
                ["8000::/1"],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with i PV4 excluded network ignores it.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithIPv4ExcludedNetwork_IgnoresIt()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [IPNetwork.Parse("10.0.0.0/8")]);

            AssertNetworksEqual(
                ["::/0"],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with mixed i PV4 and i PV6 excluded networks uses only i PV6 exclusions.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithMixedIPv4AndIPv6ExcludedNetworks_UsesOnlyIPv6Exclusions()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("10.0.0.0/8"),
                    IPNetwork.Parse("fc00::/7")
                ]);

            AssertNetworksEqual(
                [
                    "::/1",
                    "8000::/2",
                    "c000::/3",
                    "e000::/4",
                    "f000::/5",
                    "f800::/6",
                    "fe00::/7"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with nested excluded network does not add parent network back.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithNestedExcludedNetwork_DoesNotAddParentNetworkBack()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("fc00::/7"),
                    IPNetwork.Parse("fd00::/8")
                ]);

            AssertNetworksEqual(
                [
                    "::/1",
                    "8000::/2",
                    "c000::/3",
                    "e000::/4",
                    "f000::/5",
                    "f800::/6",
                    "fe00::/7"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with no excluded networks returns entire i PV6 address space.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithNoExcludedNetworks_ReturnsEntireIPv6AddressSpace()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks([]);

            AssertNetworksEqual(
                ["::/0"],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with second half excluded returns first half.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithSecondHalfExcluded_ReturnsFirstHalf()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [IPNetwork.Parse("8000::/1")]);

            AssertNetworksEqual(
                ["::/1"],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with single host excluded does not return network containing that host.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithSingleHostExcluded_DoesNotReturnNetworkContainingThatHost()
        {
            IPAddress excludedHost = IPAddress.Parse("2001:db8::1");

            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [IPNetwork.Parse("2001:db8::1/128")]);

            Assert.DoesNotContain(actual, network => network.Contains(excludedHost));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("2001:db8::")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("2001:db8::2")));
        }

        /// <summary>
        /// Gets the allowed networks with unique local address space excluded returns minimal complement networks.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithUniqueLocalAddressSpaceExcluded_ReturnsMinimalComplementNetworks()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [IPNetwork.Parse("fc00::/7")]);

            AssertNetworksEqual(
                [
                    "::/1",
                    "8000::/2",
                    "c000::/3",
                    "e000::/4",
                    "f000::/5",
                    "f800::/6",
                    "fe00::/7"
                ],
                actual);
        }

        /// <summary>
        /// Gets the allowed networks with unsorted excluded networks returns correct complement.
        /// </summary>
        [Fact]
        public void GetAllowedNetworks_WithUnsortedExcludedNetworks_ReturnsCorrectComplement()
        {
            List<IPNetwork> actual = Complement.GetAllowedNetworks(
                [
                    IPNetwork.Parse("fe80::/10"),
                    IPNetwork.Parse("2001:db8::/32"),
                    IPNetwork.Parse("fc00::/7")
                ]);

            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("2001:db8::1")));
            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("fc00::")));
            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")));
            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("fe80::1")));
            Assert.DoesNotContain(actual, network => network.Contains(IPAddress.Parse("febf:ffff:ffff:ffff:ffff:ffff:ffff:ffff")));

            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("2001:db7:ffff:ffff:ffff:ffff:ffff:ffff")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("2001:db9::")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("fbff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("fe00::")));
            Assert.Contains(actual, network => network.Contains(IPAddress.Parse("fec0::")));
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