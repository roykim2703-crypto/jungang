using NUnit.Framework;
using UnityEngine;

public class AugmentBoardTests
{
    private static int First(int min,int max)=>min;
    [TestCase(1)][TestCase(2)][TestCase(6)][TestCase(10)][TestCase(11)][TestCase(14)][TestCase(15)][TestCase(16)][TestCase(19)]
    public void AreaEffectsNeverAlterPermanentStones(int ability)
    {
        var b=new AugmentBoard();b.cells[9*19+9]=3;
        b.Ability(ability,9,9,1,0,3,First);
        Assert.AreEqual(3,b.cells[9*19+9]);
    }
    [Test]
    public void TakingOverSurvivesActivationTurnAndExpiresAfterVictimTurn()
    {
        var b=new AugmentBoard();Assert.IsTrue(b.Ability(18,9,9,1,0,1,First));
        b.Place(0,0,true);Assert.AreEqual(2,b.takeover);Assert.AreEqual(2,b.turn);
        b.Place(1,0,false);Assert.AreEqual(0,b.takeover);Assert.AreEqual(1,b.turn);
    }
    [Test]
    public void GrayStoneHasNoOwnerAndPreventsFollowingPlayersSix()
    {
        var b=new AugmentBoard();b.Ability(8,9,9,1,0,1,First);b.Place(0,0,true);b.Place(1,0,false);
        Assert.AreEqual(4,b.At(1,0));Assert.AreEqual(1,b.noSix);
        for(int x=0;x<5;x++)b.cells[9*19+x]=1;
        Assert.IsFalse(b.Place(5,9,false));
    }
    [Test]
    public void ReplayCannotCompleteFiveAndKeepsTurnUntilAllStonesReplaced()
    {
        var b=new AugmentBoard();b.cells[0]=b.cells[1]=1;
        Assert.IsTrue(b.Ability(5,9,9,1,0,1,First));
        b.Place(4,4,true);Assert.AreEqual(1,b.turn);Assert.AreEqual(1,b.replay);
        b.Place(5,4,true);Assert.AreEqual(2,b.turn);Assert.AreEqual(0,b.replay);
        b=new AugmentBoard { replay=2 };for(int x=0;x<4;x++)b.cells[9*19+x]=1;
        Assert.IsFalse(b.Place(4,9,true));
    }
    [Test]
    public void PieceCanMoveMoreThanOnceAndCaptureConsumesIt()
    {
        var b=new AugmentBoard();b.Ability(7,9,9,1,0,1,First);
        Assert.IsTrue(b.MovePiece(10,9));Assert.AreEqual(9*19+10,b.piece);
        b.turn=1;b.cells[9*19+11]=2;
        Assert.IsTrue(b.MovePiece(11,9));Assert.AreEqual(-1,b.piece);Assert.AreEqual(0,b.At(11,9));
    }
    [Test]
    public void DestroyedGiantCannotWinAfterReplacementOnSameCell()
    {
        var b=new AugmentBoard();b.Ability(14,9,9,1,0,1,First);
        b.turn=2;b.Ability(2,9,9,1,0,1,First);Assert.AreEqual(-1,b.giant[1]);
    }
    [Test]
    public void SnapshotRoundTripKeepsBoardAndPendingEffects()
    {
        var b=new AugmentBoard();b.Ability(16,9,9,1,0,1,First);b.takeover=2;b.grayNext=2;
        var copy=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(b));
        CollectionAssert.AreEqual(b.cells,copy.cells);CollectionAssert.AreEqual(b.hidden,copy.hidden);
        Assert.AreEqual(2,copy.takeover);Assert.AreEqual(2,copy.grayNext);
    }
}
