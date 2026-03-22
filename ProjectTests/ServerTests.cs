using SRC_WebApp_Bc_Proj.Server;
using SRC_WebApp_Bc_Proj.Server.DataValidation;
using SRC_WebApp_Bc_Proj.Server.inputUtils;

namespace ProjectTests
{
    public class ServerTests
    {

        // Setup the instance once for the class
        private readonly inputUtils _utils = new inputUtils();

        [Theory]
        [InlineData("test", "test", 1.0)]       // Perfect match
        [InlineData("test", "tast", 0.75)]      // 1 substitution out of 4 chars
        [InlineData("hello", "", 0.0)]          // Total deletion
        [InlineData("", "", 1.0)]               // Both empty (handled by your maxLen == 0 check)
        public void CalculateSimilarity_ReturnsExpectedRatio(string s1, string s2, double expected)
        {
            // Act
            double result = _utils.CalculateSimilarity(s1, s2);

            // Assert
            Assert.Equal(expected, result, precision: 2);
        }

        [Fact]
        public void NormalizeAndCompare_ShouldMatch_MayDayVariations()
        {
            // Arrange
            string input = "may day";
            string target = "mayday";

            // Act
            // We must normalize first because CalculateSimilarity doesn't do it internally
            string normalizedInput = _utils.NormalizeText(input);
            double score = _utils.CalculateSimilarity(normalizedInput, target);

            // Assert
            Assert.Equal(1.0, score);
        }

        [Fact]
        public void CalculateSimilarity_IsCaseSensitive()
        {
            // Your current code compares chars directly (s[i-1] == t[j-1])
            // Therefore, 'A' and 'a' will count as a substitution.
            double score = _utils.CalculateSimilarity("MAYDAY", "mayday");

            Assert.NotEqual(1.0, score);
        }
    }
}
