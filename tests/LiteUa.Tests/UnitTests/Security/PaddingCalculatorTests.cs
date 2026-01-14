using LiteUa.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace LiteUa.Tests.UnitTests.Security
{
    [Trait("Category", "Unit")]
    public class PaddingCalculatorTests
    {
        [Theory]
        [InlineData(10, 0, 16, 6)]   // 10 + 0 = 10. 16 - (10 % 16) = 6
        [InlineData(100, 32, 16, 12)] // 100 + 32 = 132. 132 % 16 = 4. 16 - 4 = 12
        [InlineData(5, 5, 8, 6)]     // 5 + 5 = 10. 10 % 8 = 2. 8 - 2 = 6
        public void CalculatePaddingSize_PartialBlock_ReturnsRemainingBytes(int plainText, int sig, int block, int expected)
        {
            // Act
            int result = PaddingCalculator.CalculatePaddingSize(plainText, sig, block);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CalculatePaddingSize_AlreadyAligned_ReturnsFullBlock()
        {
            // Arrange
            int plainText = 10;
            int signature = 6;
            int blockSize = 16;
            // 10 + 6 = 16

            // Act
            int result = PaddingCalculator.CalculatePaddingSize(plainText, signature, blockSize);

            // Assert
            // 16 % 16 = 0. 16 - 0 = 16.
            Assert.Equal(16, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-5)]
        public void CalculatePaddingSize_BlockSizeOneOrLess_ReturnsZero(int blockSize)
        {
            // Act
            int result = PaddingCalculator.CalculatePaddingSize(10, 10, blockSize);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculatePaddingSize_ZeroDataSize_ReturnsFullBlock()
        {
            // Arrange
            int blockSize = 16;

            // Act
            int result = PaddingCalculator.CalculatePaddingSize(0, 0, blockSize);

            // Assert
            // 0 % 16 = 0. 16 - 0 = 16.
            Assert.Equal(16, result);
        }
    }
}
