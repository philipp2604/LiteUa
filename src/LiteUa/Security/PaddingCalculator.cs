using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// TODO: Add unit tests

namespace LiteUa.Security
{
    /// <summary>
    /// A class for calculating padding sizes for encryption.
    /// </summary>
    public static class PaddingCalculator
    {
        /// <summary>
        /// Calculates the number of padding bytes needed to align data to the specified block size.
        /// </summary>
        /// <param name="plainTextSize">The plain text size.</param>
        /// <param name="signatureSize">The signature size.</param>
        /// <param name="blockSize">The block size.</param>
        /// <returns>The number of bytes needed as padding.</returns>
        public static int CalculatePaddingSize(int plainTextSize, int signatureSize, int blockSize)
        {
            if (blockSize <= 1) return 0;

            int dataSize = plainTextSize + signatureSize;

            int remainder = dataSize % blockSize;
            int paddingBytesNeeded = blockSize - remainder;

            return paddingBytesNeeded;
        }
    }
}
