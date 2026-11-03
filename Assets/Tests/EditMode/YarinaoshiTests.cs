using NUnit.Framework;
using UnityEngine;

public class YarinaoshiTests
{
    private static int First(int min,int max)=>min;
    private static AugmentBoard Activate(int side=1)
    {
        var b=new AugmentBoard{turn=side};
        b.cells[0]=b.cells[2]=b.cells[4]=side;b.order[0]=1;b.order[2]=2;b.order[4]=3;b.sequence=3;
        Assert.IsTrue(b.Ability(5,9,9,1,0,1,First));
        Assert.AreEqual(side,b.cells[0]);Assert.AreEqual(0,b.cells[2]);Assert.AreEqual(0,b.cells[4]);
        return b;
    }
    private static void Queue(AugmentBoard b)
    {
        for(int i=0;i<6;i++)Assert.IsTrue(b.QueuePremove(3+i*2,5));
    }
    [TestCase(1)][TestCase(2)]
    public void SixReservationsBecomeTwoAutomaticTurnsWithNoManualPlacement(int owner)
    {
        var b=Activate(owner);Queue(b);
        Assert.AreEqual(owner,b.turn);Assert.AreEqual(0,b.premoveIndex[owner]);
        Assert.IsTrue(b.Place(18,18,true));Assert.AreEqual(3-owner,b.turn);
        Assert.AreEqual(0,b.At(3,5));
        Assert.IsTrue(b.Place(17,18,false));
        Assert.AreEqual(3-owner,b.turn);Assert.AreEqual(3,b.premoveIndex[owner]);
        Assert.AreEqual(3,b.automaticPlacements.Count);Assert.AreEqual(3,b.moves);
        for(int i=0;i<3;i++)Assert.AreEqual(owner,b.At(3+i*2,5));
        Assert.IsTrue(b.Place(16,18,false));
        Assert.AreEqual(3-owner,b.turn);Assert.AreEqual(6,b.premoveIndex[owner]);
        for(int i=0;i<6;i++)Assert.AreEqual(owner,b.At(3+i*2,5));
        Assert.IsTrue(b.Place(15,18,false));Assert.AreEqual(owner,b.turn);
    }
    [Test]
    public void SetupRequiresSixDistinctPositionsAndCannotBeBypassed()
    {
        var b=Activate();Assert.IsFalse(b.Place(18,18,true));
        Assert.IsFalse(b.Ability(2,18,18,1,0,1,First));
        Assert.IsFalse(b.QueuePremove(-1,0));Assert.IsFalse(b.QueuePremove(0,0));
        Assert.IsTrue(b.QueuePremove(3,5));Assert.IsFalse(b.QueuePremove(3,5));
        for(int i=1;i<6;i++)Assert.IsTrue(b.QueuePremove(3+i*2,5));
        Assert.IsFalse(b.QueuePremove(18,18));Assert.IsTrue(b.Place(18,18,true));
    }
    [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
    public void OccupyingAnyPositionCancelsOnlyItsWholeGroupAndStillCostsTheTurn(int blocked)
    {
        var b=Activate();Queue(b);Assert.IsTrue(b.Place(18,18,true));
        b.cells[5*19+3+blocked*2]=2;
        Assert.IsTrue(b.Place(17,18,false));Assert.AreEqual(2,b.turn);
        Assert.IsTrue(b.Place(16,18,false));Assert.AreEqual(2,b.turn);
        for(int i=0;i<6;i++)
            Assert.AreEqual(i==blocked?2:i/3==blocked/3?0:1,b.At(3+i*2,5));
        Assert.AreEqual(6,b.premoveIndex[1]);
    }
    [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
    public void AnyOccupiedStoneTypeBlocksTheGroup(int occupied)
    {
        var b=Activate();Queue(b);b.Place(18,18,true);b.cells[5*19+5]=occupied;
        Assert.IsTrue(b.Place(17,18,false));Assert.AreEqual(2,b.turn);
        Assert.AreEqual(0,b.At(3,5));Assert.AreEqual(0,b.At(7,5));Assert.AreEqual(0,b.automaticPlacements.Count);
    }
    [Test]
    public void BlackHoleConsumesOnlyItsOwnPremoveWithoutBlockingTheGroup()
    {
        var b=Activate();b.cells[5*19+5]=5;Queue(b);b.Place(18,18,true);
        Assert.IsTrue(b.Place(17,18,false));Assert.AreEqual(2,b.turn);
        Assert.AreEqual(1,b.At(3,5));Assert.AreEqual(5,b.At(5,5));Assert.AreEqual(1,b.At(7,5));
        Assert.AreEqual(3,b.automaticPlacements.Count);Assert.IsTrue(b.automaticPlacements[1].vanished);
    }
    [TestCase(1)][TestCase(2)]
    public void ThreeBlackHolePremovesCanCompleteSixBeforeBeingSwallowed(int side)
    {
        var b=new AugmentBoard{turn=3-side};
        for(int x=0;x<3;x++)b.cells[9*19+x]=side;
        for(int x=3;x<6;x++)b.cells[9*19+x]=5;
        int[] queue={9*19+3,9*19+4,9*19+5,0,2,4};
        if(side==1)b.blackPremoves=queue;else b.whitePremoves=queue;
        Assert.IsTrue(b.Place(18,18,false));Assert.AreEqual(side,b.winner);
        Assert.AreEqual(3,b.automaticPlacements.Count);
        for(int x=0;x<6;x++)Assert.AreEqual(side,b.At(x,9));
        Assert.IsTrue(b.automaticPlacements.TrueForAll(p=>!p.vanished));
    }
    [Test]
    public void PremoveHasNoForbiddenMoveRestrictionAndCanWinWithSix()
    {
        var b=Activate();for(int x=0;x<3;x++)b.cells[9*19+x]=1;
        for(int i=0;i<3;i++)Assert.IsTrue(b.QueuePremove(3+i,9));
        for(int i=0;i<3;i++)Assert.IsTrue(b.QueuePremove(10+i*2,5));
        b.Place(18,18,true);Assert.IsTrue(b.Place(17,18,false));
        Assert.AreEqual(1,b.winner);for(int x=0;x<6;x++)Assert.AreEqual(1,b.At(x,9));
    }
    [Test]
    public void BothPlayersCanResolveTheirQueuesWithoutAllowingExtraManualMoves()
    {
        var b=Activate();Queue(b);b.Place(18,18,true);
        b.cells[10*19]=b.cells[10*19+2]=2;b.order[10*19]=50;b.order[10*19+2]=51;
        Assert.IsTrue(b.Ability(5,9,9,1,0,1,First));
        for(int i=0;i<6;i++)Assert.IsTrue(b.QueuePremove(3+i*2,12));
        Assert.IsTrue(b.Place(17,18,true));
        Assert.AreEqual(1,b.turn);Assert.AreEqual(6,b.premoveIndex[1]);Assert.AreEqual(6,b.premoveIndex[2]);
        Assert.AreEqual(12,b.automaticPlacements.Count);
    }
    [Test]
    public void SnapshotRoundTripKeepsQueueAndProgress()
    {
        var b=Activate();Queue(b);b.Place(18,18,true);b.Place(17,18,false);
        var copy=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(b));
        CollectionAssert.AreEqual(b.blackPremoves,copy.blackPremoves);Assert.AreEqual(3,copy.premoveIndex[1]);
        Assert.IsTrue(copy.Place(16,18,false));Assert.AreEqual(6,copy.premoveIndex[1]);
    }
    [Test]
    public void NotEnoughOwnedStonesDoesNotChangeTheBoard()
    {
        var b=new AugmentBoard();b.cells[0]=1;string before=JsonUtility.ToJson(b);
        Assert.IsFalse(b.Ability(5,9,9,1,0,1,First));Assert.AreEqual(before,JsonUtility.ToJson(b));
    }
}
