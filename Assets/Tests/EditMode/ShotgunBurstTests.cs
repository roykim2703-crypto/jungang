using NUnit.Framework;
using UnityEngine;

public class ShotgunBurstTests
{
    [Test]
    public void EveryClickWithinThreeSecondsCountsIncludingRepeatedTimestamps()
    {
        var burst=new ShotgunBurst();burst.Start(1,10);
        for(int i=0;i<100;i++)Assert.IsTrue(burst.Hit(1,10+i*.01));
        Assert.AreEqual(100,burst.hits);Assert.AreEqual(13,burst.deadline);
        Assert.IsTrue(burst.Hit(1,12.999));Assert.IsFalse(burst.Hit(1,13));
        Assert.AreEqual(101,burst.hits);Assert.AreEqual(0,burst.Remaining(13));
    }
    [Test]
    public void InactiveOrOtherPlayerCannotHitOrExtendTheWindow()
    {
        var burst=new ShotgunBurst();Assert.IsFalse(burst.Hit(1,0));
        burst.Start(2,4);Assert.IsFalse(burst.Hit(1,5));Assert.AreEqual(0,burst.hits);
        Assert.IsTrue(burst.Hit(2,6));Assert.AreEqual(7,burst.deadline);
        burst.active=false;Assert.IsFalse(burst.Hit(2,6.5));
    }
    [Test]
    public void SnapshotPreservesBurstOwnerAndHitCount()
    {
        var burst=new ShotgunBurst();burst.Start(2,10);burst.Hit(2,10.5);
        var copy=JsonUtility.FromJson<ShotgunBurst>(JsonUtility.ToJson(burst));
        Assert.IsTrue(copy.active);Assert.AreEqual(2,copy.side);Assert.AreEqual(1,copy.hits);
        Assert.IsTrue(copy.Hit(2,11));Assert.AreEqual(1,burst.hits);
    }
}
