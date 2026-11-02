using NUnit.Framework;

public class MatchClockTests
{
    [Test]
    public void OnlyActiveSideLosesTime()
    {
        var clock=new MatchClock();clock.Reset(300f);
        Assert.IsFalse(clock.Tick(1,12.5f));
        Assert.AreEqual(287.5f,clock.blackSeconds,.001f);
        Assert.AreEqual(300f,clock.whiteSeconds,.001f);
    }

    [Test]
    public void ReachingZeroReportsTimeoutAndNeverGoesNegative()
    {
        var clock=new MatchClock();clock.Reset(2f);
        Assert.IsTrue(clock.Tick(2,3f));
        Assert.AreEqual(0f,clock.whiteSeconds);
    }
}
