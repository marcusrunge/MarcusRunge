using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace MarcusRunge.Toolbox.Network.Helper
{
    internal static class ComplementHelper
    {
        // A private record struct to represent a range of addresses as numeric values, with a start and end. This is used internally to simplify the calculations when determining the complement of excluded networks, allowing us to work with numeric ranges instead of IPNetwork instances directly.
        private readonly record struct AddressRange(BigInteger Start, BigInteger End);

        /// <summary>
        /// Returns the complement of the excluded networks inside one address family.
        /// For IPv4, pass AddressFamily.InterNetwork.
        /// For IPv6, pass AddressFamily.InterNetworkV6.
        /// </summary>
        public static List<IPNetwork> GetAllowedNetworks(IEnumerable<IPNetwork> excludedNetworks, AddressFamily addressFamily)
        {
            // Validate input
            ArgumentNullException.ThrowIfNull(excludedNetworks);
            // We will ignore any excluded networks that don't match the specified address family.
            int maxBits = GetMaxBits(addressFamily);
            // Define the full address space for the specified address family.
            BigInteger addressSpaceStart = BigInteger.Zero;
            BigInteger addressSpaceEnd = (BigInteger.One << maxBits) - BigInteger.One;
            // Convert excluded networks to address ranges, filter by address family, and sort them.
            List<AddressRange> excludedRanges = [.. excludedNetworks
                .Where(network => network.BaseAddress.AddressFamily == addressFamily)
                .Select(ToRange)
                .OrderBy(range => range.Start)
                .ThenBy(range => range.End)];
            // Merge overlapping or adjacent excluded ranges to simplify the complement calculation.
            List<AddressRange> mergedExcludedRanges = MergeRanges(excludedRanges);

            // Calculate the complement by finding the gaps between the merged excluded ranges.
            List<IPNetwork> result = [];
            // Start with the beginning of the address space and find the first gap until the first excluded range.
            BigInteger cursor = addressSpaceStart;
            // Iterate through the merged excluded ranges and find the gaps between them.
            foreach (AddressRange excluded in mergedExcludedRanges)
            {
                // If the excluded range ends before the cursor, it means it's already covered by a previous range, so we can skip it.
                if (excluded.End < cursor)
                {
                    continue;
                }
                // If the excluded range starts after the cursor, it means there is a gap between the cursor and the start of the excluded range, so we need to add that gap to the result.
                if (excluded.Start > cursor)
                {
                    AddCidrsForRange(
                        result,
                        cursor,
                        excluded.Start - BigInteger.One,
                        addressFamily);
                }
                // Move the cursor to the end of the excluded range, plus one, to start looking for the next gap.
                cursor = BigInteger.Max(cursor, excluded.End + BigInteger.One);
                // If the cursor has moved past the end of the address space, we can stop processing further ranges.
                if (cursor > addressSpaceEnd)
                {
                    break;
                }
            }
            // After processing all excluded ranges, if the cursor is still within the address space, it means there is a final gap from the cursor to the end of the address space that we need to add to the result.
            if (cursor <= addressSpaceEnd)
            {
                AddCidrsForRange(
                    result,
                    cursor,
                    addressSpaceEnd,
                    addressFamily);
            }

            return result;
        }

        // Adds CIDR blocks to the result list that cover the range of addresses from start to end. This method uses a greedy approach to find the largest possible CIDR block that fits within the remaining address space and is aligned with the start address, then adds that block to the result and moves the start pointer forward until it has covered the entire range.
        private static void AddCidrsForRange(List<IPNetwork> result, BigInteger start, BigInteger end, AddressFamily addressFamily)
        {
            // Get the maximum number of bits for the address family (32 for IPv4, 128 for IPv6).
            int maxBits = GetMaxBits(addressFamily);
            // Loop until we have covered the entire range from start to end.
            while (start <= end)
            {
                // Calculate the number of remaining addresses in the range from start to end.
                BigInteger remainingAddresses = end - start + BigInteger.One;
                // Calculate the number of bits we can use for the CIDR block based on the alignment of the start address and the number of remaining addresses. The alignment block bits are determined by counting the number of trailing zero bits in the start address, which indicates how large of a block we can create that is aligned with the start address. The remaining block bits are determined by calculating the floor of the log base 2 of the remaining addresses, which indicates how large of a block we can create based on how many addresses are left to cover. We take the minimum of these two values to ensure that we create a valid CIDR block that fits within both constraints.
                int alignmentBlockBits = start.IsZero
                    ? maxBits
                    : Math.Min(CountTrailingZeroBits(start), maxBits);
                // The number of bits for the CIDR block is the minimum of the alignment block bits and the remaining block bits, which ensures that we create the largest possible CIDR block that is properly aligned and does not exceed the remaining address space.
                int remainingBlockBits = Math.Min(
                    FloorLog2(remainingAddresses),
                    maxBits);
                // The prefix length for the CIDR block is calculated by subtracting the number of block bits from the maximum number of bits for the address family. This gives us the correct prefix length that corresponds to the size of the CIDR block we are creating.
                int blockBits = Math.Min(alignmentBlockBits, remainingBlockBits);
                int prefixLength = maxBits - blockBits;
                // Convert the numeric start address back to an IPAddress and create a new IPNetwork with the calculated prefix length, then add it to the result list.
                IPAddress baseAddress = ToIPAddress(start, addressFamily);
                // Add the new CIDR block to the result list.
                result.Add(new IPNetwork(baseAddress, prefixLength));
                // Move the start pointer forward by the size of the CIDR block we just created, which is 2 raised to the power of the number of block bits. This allows us to continue finding the next CIDR block for the remaining address space until we have covered the entire range.
                start += BigInteger.One << blockBits;
            }
        }

        // Counts the number of trailing zero bits in a BigInteger value, which is used to determine the alignment of an address when calculating CIDR blocks. This method iterates through the bytes of the BigInteger and counts how many bits are zero starting from the least significant bit until it encounters a non-zero bit, which indicates the point at which we can create a CIDR block that is properly aligned with the start address.
        private static int CountTrailingZeroBits(BigInteger value)
        {
            // If the value is zero, it means that all bits are zero, and we can consider it to have the maximum number of trailing zero bits (which is equal to the maximum number of bits for the address family). In this case, we return int.MaxValue to indicate that it has an effectively infinite number of trailing zero bits, which will allow us to create the largest possible CIDR block when this value is used as a start address.
            if (value.IsZero)
            {
                // Return int.MaxValue to indicate that a zero value has an effectively infinite number of trailing zero bits, which allows for the creation of the largest possible CIDR block when this value is used as a start address.
                return int.MaxValue;
            }
            // Get the byte representation of the BigInteger value, which will be in little-endian format (least significant byte first) and unsigned. This is important for correctly counting the trailing zero bits starting from the least significant bit.
            byte[] bytes = value.ToByteArray(
                isUnsigned: true,
                isBigEndian: false);
            // Initialize a count variable to keep track of the number of trailing zero bits we have encountered as we iterate through the bytes of the BigInteger.
            int count = 0;
            // Iterate through the bytes of the BigInteger starting from the least significant byte, and count how many bits are zero until we encounter a non-zero bit. For each byte that is zero, we can add 8 to the count since it contributes 8 trailing zero bits. Once we encounter a non-zero byte, we need to check each bit in that byte to count any additional trailing zero bits before we reach the first set bit.
            foreach (byte b in bytes)
            {
                // If the byte is zero, it contributes 8 trailing zero bits, so we add 8 to the count and continue to the next byte.
                if (b == 0)
                {
                    count += 8;
                    continue;
                }
                // If the byte is not zero, we need to check each bit in the byte to count any additional trailing zero bits before we reach the first set bit. We can do this by checking each bit from the least significant bit to the most significant bit until we find a bit that is set (not zero). For each bit that is zero, we add 1 to the count. Once we find a set bit, we can return the total count of trailing zero bits.
                for (int bit = 0; bit < 8; bit++)
                {
                    // Check if the current bit is set (not zero) by performing a bitwise AND operation with a mask that has only that bit set. If the result is not zero, it means we have found the first set bit, and we can return the total count of trailing zero bits.
                    if ((b & (1 << bit)) != 0)
                    {
                        // Return the total count of trailing zero bits, which includes the bits from the previous zero bytes and any additional zero bits in the current byte before the first set bit.
                        return count + bit;
                    }
                }
            }
            // If we have iterated through all bytes and found that they are all zero, we can return the total count of trailing zero bits, which in this case would be the maximum possible for the address family. However, since we already handle the case of a zero value at the beginning of the method, we should never reach this point with a non-zero value. If we do, it means there is an error in our logic, and we can throw an exception to indicate that we were unable to count the trailing zero bits.
            return count;
        }

        // Calculates the floor of the logarithm base 2 of a BigInteger value, which is used to determine how many bits are needed to represent a certain number of addresses when calculating CIDR blocks. This method works by finding the position of the most significant set bit in the BigInteger, which gives us the highest power of 2 that is less than or equal to the value, effectively giving us the floor of log base 2.
        private static int FloorLog2(BigInteger value)
        {
            // If the value is less than or equal to zero, it means that we cannot calculate a logarithm for it, and we should throw an exception to indicate that the input is invalid. Logarithms are only defined for positive numbers, so we need to ensure that the value is greater than zero before we proceed with the calculation.
            if (value <= BigInteger.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Value must be greater than zero.");
            }
            // Get the byte representation of the BigInteger value, which will be in big-endian format (most significant byte first) and unsigned. This is important for correctly finding the most significant set bit, which determines the floor of log base 2.
            byte[] bytes = value.ToByteArray(
                isUnsigned: true,
                isBigEndian: true);
            // The most significant byte is the first byte in the big-endian representation, and it contains the highest order bits of the value. We need to check this byte to find the position of the most significant set bit, which will give us the floor of log base 2.
            byte mostSignificantByte = bytes[0];
            // The initial result is calculated based on the number of bytes in the BigInteger, since each byte contributes 8 bits. We start with (bytes.Length - 1) * 8 because we will check the most significant byte separately to find the exact position of the highest set bit.
            int result = (bytes.Length - 1) * 8;
            // We need to check the bits in the most significant byte to find the position of the highest set bit. We can do this by iterating through the bits of the most significant byte from the most significant bit (bit 7) to the least significant bit (bit 0) and checking if each bit is set. The first bit that we find that is set will give us the exact position of the highest set bit, which we can add to our initial result to get the final floor of log base 2.
            for (int bit = 7; bit >= 0; bit--)
            {
                // Check if the current bit in the most significant byte is set by performing a bitwise AND operation with a mask that has only that bit set. If the result is not zero, it means we have found the highest set bit, and we can return the total number of bits needed to represent the value, which is the initial result plus the position of the set bit.
                if ((mostSignificantByte & (1 << bit)) != 0)
                {
                    // Return the total number of bits needed to represent the value, which is the initial result based on the number of bytes plus the position of the highest set bit in the most significant byte.
                    return result + bit;
                }
            }
            // If we have iterated through all bits of the most significant byte and found that they are all zero, it means there is an error in our logic, as we should have found a set bit since the value is greater than zero. In this case, we can throw an exception to indicate that we were unable to calculate the logarithm.
            throw new InvalidOperationException("Unable to calculate log2.");
        }

        // Gets the maximum number of bits for the specified address family, which is 32 for IPv4 and 128 for IPv6. This is used to determine the size of the address space and to calculate prefix lengths for CIDR blocks.
        private static int GetMaxBits(AddressFamily addressFamily)
        {
            // Return the maximum number of bits for the specified address family. This is a simple mapping based on the standard sizes of IPv4 and IPv6 addresses, and it ensures that we have the correct values for our calculations when working with different address families.
            return addressFamily switch
            {
                AddressFamily.InterNetwork => 32,
                AddressFamily.InterNetworkV6 => 128,
                _ => throw new ArgumentException(
                    "Only IPv4 and IPv6 are supported.",
                    nameof(addressFamily))
            };
        }

        // Merges overlapping or adjacent address ranges into a single range to simplify the complement calculation. This ensures that we have a minimal set of non-overlapping ranges to work with when calculating the allowed networks.
        private static List<AddressRange> MergeRanges(List<AddressRange> ranges)
        {
            // If there are no ranges to merge, return an empty list.
            List<AddressRange> merged = [];
            // Iterate through the sorted list of address ranges and merge them if they overlap or are adjacent.
            foreach (AddressRange range in ranges)
            {
                // If the merged list is empty, simply add the first range to it.
                if (merged.Count == 0)
                {
                    merged.Add(range);
                    continue;
                }
                // Get the last merged range to compare with the current range.
                AddressRange last = merged[^1];
                // Check if the current range overlaps with or is adjacent to the last merged range. If the start of the current range is less than or equal to the end of the last merged range plus one, it means they are overlapping or adjacent and should be merged into a single range.
                bool overlapsOrTouches = range.Start <= last.End + BigInteger.One;

                // If they overlap or are adjacent, merge them by creating a new range that starts at the start of the last merged range and ends at the maximum of the end of the last merged range and the end of the current range. This effectively combines the two ranges into one.
                if (overlapsOrTouches)
                {
                    // Update the last merged range with the new merged range.
                    merged[^1] = new AddressRange(
                        last.Start,
                        BigInteger.Max(last.End, range.End));
                }
                // If they do not overlap or are adjacent, simply add the current range to the merged list as a new separate range.
                else
                {
                    // Add the current range to the merged list as it does not overlap with the last merged range.
                    merged.Add(range);
                }
            }
            // Return the list of merged address ranges, which now contains non-overlapping and non-adjacent ranges that represent the excluded networks in a simplified form.
            return merged;
        }

        // Converts an IPAddress to a BigInteger for easier manipulation of the address as a numeric value. This allows us to perform arithmetic operations and comparisons on the addresses when calculating ranges and CIDR blocks.
        private static BigInteger ToBigInteger(IPAddress address)
        {
            // Get the byte representation of the IP address, which will be in big-endian format (most significant byte first). This is important for correctly converting the address to a numeric value.
            byte[] bytes = address.GetAddressBytes();

            // Convert the byte array to a BigInteger, specifying that it is unsigned and in big-endian format. This will give us a numeric representation of the IP address that we can use for calculations.
            return new BigInteger(
                bytes,
                isUnsigned: true,
                isBigEndian: true);
        }

        // Converts a BigInteger back to an IPAddress, ensuring that it fits within the specified address family (IPv4 or IPv6) and is properly formatted as a byte array. This is used when we need to convert numeric addresses back to their standard IP address format for creating IPNetwork instances.
        private static IPAddress ToIPAddress(BigInteger value, AddressFamily addressFamily)
        {
            // Determine the number of bytes required for the specified address family (4 bytes for IPv4, 16 bytes for IPv6). This is necessary to ensure that the byte array we create for the IP address has the correct length and format.
            int byteCount = addressFamily switch
            {
                AddressFamily.InterNetwork => 4,
                AddressFamily.InterNetworkV6 => 16,
                _ => throw new ArgumentException(
                    "Only IPv4 and IPv6 are supported.",
                    nameof(addressFamily))
            };
            // Get the byte representation of the BigInteger value, which will be in big-endian format (most significant byte first) and unsigned. This will give us the raw bytes that represent the numeric address.
            byte[] source = value.ToByteArray(
                isUnsigned: true,
                isBigEndian: true);
            // If the byte array is larger than the expected byte count for the address family, it means that the numeric address does not fit within the specified address family, and we should throw an exception to indicate this error.
            if (source.Length > byteCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The numeric address does not fit into the requested address family.");
            }
            // Create a destination byte array of the correct length for the address family, and copy the source bytes into it, aligning them to the right (least significant bytes) and padding with zeros on the left if necessary. This ensures that we have a properly formatted byte array that can be converted to an IPAddress.
            byte[] destination = new byte[byteCount];
            // Copy the source bytes into the destination array, starting from the end of the destination array and moving backwards, to ensure that the least significant bytes of the numeric address are correctly placed in the byte array for the IP address.
            Buffer.BlockCopy(
                source,
                0,
                destination,
                byteCount - source.Length,
                source.Length);
            // Create and return a new IPAddress instance using the destination byte array, which now contains the correct bytes for the specified address family.
            return new IPAddress(destination);
        }

        // Converts an IPNetwork to an AddressRange, which is a numeric representation of the start and end addresses of the network.
        private static AddressRange ToRange(IPNetwork network)
        {
            // Get the maximum number of bits for the address family (32 for IPv4, 128 for IPv6).
            int maxBits = GetMaxBits(network.BaseAddress.AddressFamily);
            // Calculate the numeric start and end addresses of the network based on the base address and prefix length.
            BigInteger start = ToBigInteger(network.BaseAddress);
            BigInteger size = BigInteger.One << (maxBits - network.PrefixLength);
            BigInteger end = start + size - BigInteger.One;
            // Return the address range as a struct containing the start and end numeric addresses.
            return new AddressRange(start, end);
        }
    }
}