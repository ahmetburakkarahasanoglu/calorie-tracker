using KaloriTakip.Domain;

namespace KaloriTakip.Domain.Tests;

public class CalorieCalculatorTests
{
    [Fact]
    public void CalculateBmr_UsesMifflinStJeorEquationForMale()
    {
        var result = CalorieCalculator.CalculateBmr(80, 180, 30, Gender.Male);

        Assert.Equal(1780, result);
    }

    [Fact]
    public void CalculateDailyLimit_AppliesActivityAndWeeklyLossTarget()
    {
        var result = CalorieCalculator.CalculateDailyLimit(
            80, 180, 30, Gender.Male, ActivityLevel.Moderate, 0.5);

        Assert.Equal(2209, result);
    }

    [Fact]
    public void CalculateDailyLimit_DoesNotGoBelowTheConfiguredMinimum()
    {
        var result = CalorieCalculator.CalculateDailyLimit(
            35, 145, 80, Gender.Female, ActivityLevel.Sedentary, 1);

        Assert.Equal(1200, result);
    }

    [Fact]
    public void CalculateDailyLimit_RejectsProfilesUnder18()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalorieCalculator.CalculateDailyLimit(
                60, 165, 17, Gender.Female, ActivityLevel.Light, 0.5));

        Assert.Equal("age", exception.ParamName);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void CalculateDailyLimit_RejectsWeeklyLossTargetsOutsideSupportedRange(double weeklyLossKg)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalorieCalculator.CalculateDailyLimit(
                80, 180, 30, Gender.Male, ActivityLevel.Moderate, weeklyLossKg));

        Assert.Equal("weeklyLossKg", exception.ParamName);
    }

    [Fact]
    public void CalculateDailyLimit_RejectsNonPositiveWeight()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalorieCalculator.CalculateDailyLimit(
                0, 180, 30, Gender.Male, ActivityLevel.Moderate, 0.5));

        Assert.Equal("weightKg", exception.ParamName);
    }
}
