using SpireSense.SpireSenseCode.Categories;
using Xunit;

namespace SpireSense.Tests;

public class AttritionTests
{
    private static RunStats WithFight(FightKind kind, string key, double hpLost)
    {
        var stats = new RunStats();
        stats.NoteFight(key, kind);
        stats.AddHpLost(hpLost);
        return stats;
    }

    [Fact]
    public void HealthLostIsChargedToTheFightInProgress()
    {
        var stats = new RunStats();

        stats.NoteFight("a", FightKind.Normal);
        stats.AddHpLost(10);
        stats.NoteFight("b", FightKind.Elite);
        stats.AddHpLost(30);
        stats.NoteFight("c", FightKind.Normal);
        stats.AddHpLost(6);

        Assert.Equal(2, stats.FightsSeen(FightKind.Normal));
        Assert.Equal(1, stats.FightsSeen(FightKind.Elite));
        Assert.Equal(8, stats.HpLostPerFight(FightKind.Normal));
        Assert.Equal(30, stats.HpLostPerFight(FightKind.Elite));
    }

    [Fact]
    public void TheSameFightIsCountedOnceHoweverOftenItIsNoted()
    {
        // NoteFight runs every frame while a combat is on screen.
        var stats = new RunStats();
        for (var i = 0; i < 50; i++)
        {
            stats.NoteFight("one-combat", FightKind.Elite);
        }
        stats.AddHpLost(24);

        Assert.Equal(1, stats.FightsSeen(FightKind.Elite));
        Assert.Equal(24, stats.HpLostPerFight(FightKind.Elite));
    }

    [Fact]
    public void AKindNeverFoughtHasNoAverageRatherThanZero()
    {
        // "No elite yet" and "elites are free" are opposite facts and must not share a value.
        var stats = WithFight(FightKind.Normal, "a", 10);

        Assert.True(double.IsNaN(stats.HpLostPerFight(FightKind.Elite)));
        Assert.Equal("—", Attrition.Format(stats.HpLostPerFight(FightKind.Elite)));
    }

    [Fact]
    public void TheForecastPrefersTheHardestFightItHasEvidenceFor()
    {
        var stats = new RunStats();
        stats.NoteFight("a", FightKind.Normal);
        stats.AddHpLost(8);

        Assert.Equal(FightKind.Normal, Attrition.Forecast(stats, 60).Kind);

        stats.NoteFight("b", FightKind.Elite);
        stats.AddHpLost(24);

        Assert.Equal(FightKind.Elite, Attrition.Forecast(stats, 60).Kind);

        stats.NoteFight("c", FightKind.Boss);
        stats.AddHpLost(31);

        Assert.Equal(FightKind.Boss, Attrition.Forecast(stats, 60).Kind);
    }

    [Fact]
    public void ThereIsNoForecastBeforeTheFirstFight()
    {
        Assert.False(Attrition.Forecast(new RunStats(), 80).HasData);
    }

    [Fact]
    public void WarnsWhenTheNextFightWouldCostMoreThanYouHave()
    {
        var stats = WithFight(FightKind.Elite, "a", 40);

        Assert.True(Attrition.Forecast(stats, 30).WouldNotSurvive);
        Assert.False(Attrition.Forecast(stats, 90).WouldNotSurvive);
    }

    [Fact]
    public void FlagsTheCaseWhereYouCouldTakeOneMoreButNotTwo()
    {
        var stats = WithFight(FightKind.Elite, "a", 40);

        var marginal = Attrition.Forecast(stats, 60);
        Assert.True(marginal.Marginal);
        Assert.False(marginal.WouldNotSurvive);
        Assert.Equal(1.5, marginal.FightsRemaining, 3);

        Assert.False(Attrition.Forecast(stats, 200).Marginal);
    }

    [Fact]
    public void HealthFiguresAreWholeNumbers()
    {
        Assert.Equal("24", Attrition.Format(23.6));
        Assert.Equal("8", Attrition.Format(7.5));
    }
}
