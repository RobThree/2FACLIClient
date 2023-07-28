using Microsoft.Extensions.Options;

namespace TwoFA.Tests;

[TestClass]
public class TwoFACalculatorTests
{
    [TestMethod]
    public void TwoFATestVectors_SHA1_6_30()
    {
        var c = new TwoFACalculator(Options.Create(new TwoFACalculatorOptions
        {
            // All "defaults"
            Algorithm = Algorithm.SHA1,
            Digits = 6,
            Period = TimeSpan.FromSeconds(30)
        }
        ));
        Assert.AreEqual("543160", c.GetCode("VMR466AB62ZBOKHE", DateTimeOffset.FromUnixTimeSeconds(1426847190)));
    }

    [TestMethod]
    public void TwoFATestVectors_SHA1_6_60()
    {
        var c = new TwoFACalculator(Options.Create(new TwoFACalculatorOptions
        {
            // Non-default period
            Algorithm = Algorithm.SHA1,
            Digits = 6,
            Period = TimeSpan.FromSeconds(60)
        }
        ));
        Assert.AreEqual("538476", c.GetCode("VMR466AB62ZBOKHE", DateTimeOffset.FromUnixTimeSeconds(1426847190)));
    }

    [TestMethod]
    public void TwoFATestVectors_SHA1_8_30()
    {
        var c = new TwoFACalculator(Options.Create(new TwoFACalculatorOptions
        {
            // Non-default digits
            Algorithm = Algorithm.SHA1,
            Digits = 8,
            Period = TimeSpan.FromSeconds(30)
        }
        ));
        Assert.AreEqual("89005924", c.GetCode("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", DateTimeOffset.FromUnixTimeSeconds(1234567890)));
    }

    [TestMethod]
    public void TwoFATestVectors_SHA256_6_30()
    {
        var c = new TwoFACalculator(Options.Create(new TwoFACalculatorOptions
        {
            // Non-default algorithm
            Algorithm = Algorithm.SHA256,
            Digits = 6,
            Period = TimeSpan.FromSeconds(30)
        }
        ));
        Assert.AreEqual("819424", c.GetCode("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA", DateTimeOffset.FromUnixTimeSeconds(1234567890)));
    }
}
