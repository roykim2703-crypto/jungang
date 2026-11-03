using NUnit.Framework;
using UnityEngine;

public class AugmentBoardTests
{
    private static int First(int min,int max)=>min;
    [TestCase(1)][TestCase(2)]
    public void BlackHoleAcceptsPlacementConsumesTurnAndRemainsAvailable(int side)
    {
        var b=new AugmentBoard{turn=side};b.cells[180]=5;
        Assert.IsTrue(b.PreviewLegal(9,9,side,false,side));
        Assert.IsTrue(b.Place(9,9,false));Assert.AreEqual(5,b.At(9,9));
        Assert.IsTrue(b.lastPlacementVanished);Assert.AreEqual(0,b.winner);
        Assert.AreEqual(3-side,b.turn);Assert.AreEqual(1,b.moves);
        Assert.IsTrue(b.Place(9,9,false));Assert.AreEqual(5,b.At(9,9));Assert.AreEqual(2,b.moves);
    }
    [TestCase(1,1,0)][TestCase(2,1,0)][TestCase(1,0,1)][TestCase(2,0,1)]
    [TestCase(1,1,1)][TestCase(2,1,1)][TestCase(1,1,-1)][TestCase(2,1,-1)]
    public void WinningStoneInsideBlackHoleWinsBeforeItCanVanish(int side,int dx,int dy)
    {
        var b=new AugmentBoard{turn=side};
        for(int i=0;i<5;i++)b.cells[(9+dy*i)*19+4+dx*i]=side;
        int x=4+dx*5,y=9+dy*5;b.cells[y*19+x]=5;
        Assert.IsTrue(b.Place(x,y,false));Assert.AreEqual(side,b.winner);
        Assert.AreEqual(side,b.At(x,y));Assert.IsFalse(b.lastPlacementVanished);
    }
    [Test]
    public void BlackHoleStillHonorsAugmentSixRestrictionAndGrayPenalty()
    {
        var b=new AugmentBoard();for(int i=0;i<5;i++)b.cells[9*19+i]=1;b.cells[9*19+5]=5;
        Assert.IsFalse(b.Place(5,9,true));Assert.AreEqual(5,b.At(5,9));Assert.AreEqual(0,b.moves);
        b.noSix=1;Assert.IsTrue(b.Place(5,9,false));Assert.AreEqual(0,b.winner);
        Assert.AreEqual(5,b.At(5,9));Assert.IsTrue(b.lastPlacementVanished);
    }
    [TestCase(1)][TestCase(2)]
    public void HiddenStoneIsUnknownToOpponentUntilPlacementLosesTheirTurn(int owner)
    {
        var b=new AugmentBoard { turn=owner };
        Assert.IsTrue(b.Ability(16,9,9,1,0,1,First));
        Assert.AreEqual(owner,b.VisibleAt(9,9,owner));
        Assert.AreEqual(0,b.VisibleAt(9,9,3-owner));
        Assert.IsFalse(b.PreviewLegal(9,9,owner,false,owner));
        Assert.IsTrue(b.PreviewLegal(9,9,3-owner,false,3-owner));
        b.turn=3-owner;
        Assert.IsTrue(b.Place(9,9,false,out bool revealed));
        Assert.IsTrue(revealed);Assert.AreEqual(owner,b.At(9,9));
        Assert.AreEqual(0,b.hidden[9*19+9]);Assert.AreEqual(owner,b.turn);
        Assert.AreEqual(1,b.moves);
        Assert.IsFalse(b.Place(9,9,false));Assert.AreEqual(owner,b.turn);
    }
    [Test]
    public void OwnHiddenStoneCannotBeRevealedByPlacingOnIt()
    {
        var b=new AugmentBoard();b.Ability(16,9,9,1,0,1,First);
        Assert.IsFalse(b.Place(9,9,false,out bool revealed));
        Assert.IsFalse(revealed);Assert.AreEqual(1,b.hidden[9*19+9]);
        Assert.AreEqual(1,b.turn);Assert.AreEqual(0,b.moves);
    }
    [Test]
    public void HiddenStoneCollisionEndsRemainingReplayPlacements()
    {
        var b=new AugmentBoard();b.Ability(16,9,9,1,0,1,First);
        b.turn=2;b.replay=3;
        Assert.IsTrue(b.Place(9,9,true,out bool revealed));
        Assert.IsTrue(revealed);Assert.AreEqual(0,b.replay);Assert.AreEqual(1,b.turn);
    }
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
    public void GrayStoneHasNoOwnerAndSuppressesFollowingPlayersWinForOneTurn()
    {
        var b=new AugmentBoard();b.Ability(8,9,9,1,0,1,First);b.Place(0,0,true);b.Place(1,0,false);
        Assert.AreEqual(4,b.At(1,0));Assert.AreEqual(1,b.noSix);
        for(int x=0;x<5;x++)b.cells[9*19+x]=1;
        Assert.IsTrue(b.Place(5,9,false),"The penalty suppresses victory, not placement.");
        Assert.AreEqual(0,b.winner);Assert.AreEqual(0,b.noSix);
    }
    [TestCase(1,0,1)][TestCase(2,0,1)][TestCase(1,5,1)][TestCase(2,5,1)]
    [TestCase(1,2,1)][TestCase(2,2,1)][TestCase(1,2,0)][TestCase(2,2,0)]
    public void GrayStoneConnectsEitherSideAtStartMiddleOrEnd(int side,int grayIndex,int horizontal)
    {
        var b=new AugmentBoard{turn=side};
        for(int i=0;i<6;i++)b.cells[(horizontal==1?8:3+i)*19+(horizontal==1?3+i:8)]=i==grayIndex?4:side;
        b.CheckWinner();Assert.AreEqual(side,b.winner);
    }
    [Test]
    public void GrayCannotConnectMixedColorsOrWinAlone()
    {
        var b=new AugmentBoard();for(int i=0;i<6;i++)b.cells[i]=4;
        b.CheckWinner();Assert.AreEqual(0,b.winner);
        b.cells[0]=1;b.cells[5]=2;b.CheckWinner();Assert.AreEqual(0,b.winner);
    }
    [TestCase(1)][TestCase(2)]
    public void GrayPenaltyAlsoBlocksAbilityVictoryThenExpires(int side)
    {
        var b=new AugmentBoard{turn=side,noSix=side};
        for(int i=0;i<5;i++)b.cells[9*19+i]=i==2?4:side;
        Assert.IsTrue(b.Ability(2,5,9,1,0,1,First));Assert.AreEqual(0,b.winner);
        Assert.IsTrue(b.Place(18,18,false));Assert.AreEqual(0,b.noSix);
        b.CheckWinner();Assert.AreEqual(side,b.winner);
    }
    [TestCase(1)][TestCase(2)]
    public void GrayPlacerCanWinImmediatelyUsingTheNewGrayStone(int side)
    {
        var b=new AugmentBoard{turn=side,grayNext=side};
        for(int i=0;i<5;i++)b.cells[9*19+i]=side;
        Assert.IsTrue(b.Place(5,9,false));Assert.AreEqual(4,b.At(5,9));Assert.AreEqual(side,b.winner);
    }
    [Test]
    public void GrayPenaltySuppressesTheFollowingAutomaticPremoveTurn()
    {
        var b=new AugmentBoard{turn=2,grayNext=2,blackPremoves=new[]{0,2,4,38,40,42}};
        for(int i=0;i<5;i++)b.cells[9*19+i]=1;
        Assert.IsTrue(b.Place(5,9,false));
        Assert.AreEqual(4,b.At(5,9));Assert.AreEqual(3,b.premoveIndex[1]);
        Assert.AreEqual(2,b.turn);Assert.AreEqual(0,b.winner);Assert.AreEqual(0,b.noSix);
    }
    [Test]
    public void LegacyReplaySnapshotAllowsFiveAndKeepsTurnUntilAllStonesReplaced()
    {
        var b=new AugmentBoard {replay=2};
        b.Place(4,4,true);Assert.AreEqual(1,b.turn);Assert.AreEqual(1,b.replay);
        b.Place(5,4,true);Assert.AreEqual(2,b.turn);Assert.AreEqual(0,b.replay);
        b=new AugmentBoard { replay=2 };for(int x=0;x<4;x++)b.cells[9*19+x]=1;
        Assert.IsTrue(b.Place(4,9,true));
        Assert.IsFalse(b.Place(5,9,true));
    }
    [Test]
    public void ReplayAllowsDoubleFour()
    {
        var b=new AugmentBoard { replay=2 };
        b.cells[9*19+7]=b.cells[9*19+8]=b.cells[9*19+10]=1;
        b.cells[7*19+9]=b.cells[8*19+9]=b.cells[10*19+9]=1;
        Assert.IsTrue(b.Place(9,9,true));
    }
    [Test]
    public void DestroyedGiantCannotWinAfterReplacementOnSameCell()
    {
        var b=new AugmentBoard();b.Ability(14,9,9,1,0,1,First);
        b.turn=2;b.Ability(2,9,9,1,0,1,First);Assert.AreEqual(-1,b.giant[1]);
    }
    [Test]
    public void BigStoneUsesOneAnchorAndBreaksAsOneObject()
    {
        var b=new AugmentBoard();int anchor=8*19+8;
        Assert.IsTrue(b.Ability(6,8,8,1,0,1,First));
        Assert.AreEqual(1,b.largeStone[anchor]);
        Assert.AreEqual(4,System.Array.FindAll(b.cells,c=>c==1).Length);
        Assert.IsTrue(b.Ability(2,8,8,1,0,1,First));
        Assert.AreEqual(0,b.largeStone[anchor]);
        Assert.AreEqual(1,System.Array.FindAll(b.cells,c=>c==1).Length);
    }
    [Test]
    public void YarinaoshiRemovesRecentLargeStoneAsOneObject()
    {
        var b=new AugmentBoard();Assert.IsTrue(b.Ability(6,8,8,1,0,1,First));
        b.cells[0]=1;b.order[0]=++b.sequence;
        Assert.IsTrue(b.Ability(5,9,9,1,0,1,First));
        Assert.AreEqual(0,b.replay);Assert.AreEqual(0,System.Array.FindAll(b.cells,c=>c!=0).Length);
        Assert.AreEqual(0,b.largeStone[8*19+8]);Assert.AreEqual(1,b.premoveSetup);
    }
    [TestCase(1)][TestCase(2)]
    public void BigStoneOccupiesAllFourCellsForBothPlayers(int owner)
    {
        var b=new AugmentBoard { turn=owner };
        Assert.IsTrue(b.Ability(6,8,8,1,0,1,First));
        for(int y=8;y<=9;y++)for(int x=8;x<=9;x++)
        {
            Assert.AreEqual(owner,b.At(x,y));
            Assert.IsFalse(b.Legal(x,y,1,false));
            Assert.IsFalse(b.Legal(x,y,2,false));
            Assert.IsFalse(b.Ability(10,x,y,1,0,1,First));
            Assert.IsFalse(b.Ability(16,x,y,1,0,1,First));
        }
    }
    [TestCase(1,0,8)][TestCase(1,0,9)]
    [TestCase(0,1,8)][TestCase(0,1,9)]
    public void BothRowsAndColumnsOfBigStoneCountTowardsSix(int dx,int dy,int line)
    {
        var b=new AugmentBoard();
        for(int i=4;i<8;i++)b.cells[(dy==0?line:i)*19+(dx==0?line:i)]=1;
        Assert.IsTrue(b.Ability(6,8,8,1,0,1,First));
        Assert.AreEqual(1,b.winner);
    }
    [TestCase(1,1,0,0)][TestCase(2,1,0,0)]
    [TestCase(1,1,0,1)][TestCase(2,1,0,1)]
    [TestCase(1,0,1,0)][TestCase(2,0,1,0)]
    [TestCase(1,0,1,1)][TestCase(2,0,1,1)]
    [TestCase(1,1,1,0)][TestCase(2,1,1,0)]
    [TestCase(1,1,-1,0)][TestCase(2,1,-1,0)]
    public void PlacingFourMoreStonesAfterBigStoneWins(int owner,int dx,int dy,int offset)
    {
        var b=new AugmentBoard { turn=owner };
        Assert.IsTrue(b.Ability(6,8,8,1,0,1,First));
        int startX=dx==0?8+offset:10;
        int startY=dy==0?8+offset:dy==-1?7:10;
        for(int i=0;i<4;i++)
        {
            b.turn=owner;
            Assert.IsTrue(b.Place(startX+dx*i,startY+dy*i,false),"placement "+i);
            Assert.AreEqual(i==3?owner:0,b.winner,"winner after placement "+i);
        }
    }
    [Test]
    public void WeakFlickStopsBeforeAStoneItCannotReach()
    {
        var b=new AugmentBoard();b.cells[9*19+5]=1;b.cells[9*19+9]=2;
        Assert.IsTrue(b.Ability(3,5,9,1000,0,2,First));
        Assert.AreEqual(1,b.At(7,9));Assert.AreEqual(2,b.At(9,9));
    }
    [Test]
    public void StrongFlickPushesSeveralStonesAndKeepsTravelling()
    {
        var b=new AugmentBoard();b.cells[9*19+5]=1;b.cells[9*19+7]=2;b.cells[9*19+9]=2;
        Assert.IsTrue(b.Ability(3,5,9,1000,0,8,First));
        Assert.AreEqual(1,b.At(9,9));Assert.AreEqual(2,b.At(10,9));Assert.AreEqual(2,b.At(11,9));
    }
    [TestCase(0,0,1,0)][TestCase(1,0,1,0)][TestCase(0,1,0,1)][TestCase(1,1,-1,0)]
    public void FlickMovesLargeStoneAsOneObjectFromAnyCoveredCell(int ox,int oy,int dx,int dy)
    {
        var b=new AugmentBoard();Assert.IsTrue(b.Ability(6,5,8,1,0,1,First));
        Assert.IsTrue(b.Ability(3,5+ox,8+oy,dx,dy,2,First));
        int x=5+2*dx,y=8+2*dy;
        Assert.AreEqual(1,b.largeStone[y*19+x]);
        Assert.AreEqual(4,System.Array.FindAll(b.cells,c=>c==1).Length);
        Assert.AreEqual(1,System.Array.FindAll(b.largeStone,c=>c!=0).Length);
        for(int ay=0;ay<2;ay++)for(int ax=0;ax<2;ax++)Assert.AreEqual(1,b.At(x+ax,y+ay));
    }
    [Test]
    public void FlickPushesEnemyLargeStoneWithoutBreakingItsFootprint()
    {
        var b=new AugmentBoard {turn=2};Assert.IsTrue(b.Ability(6,7,8,1,0,1,First));
        b.turn=1;b.cells[8*19+5]=1;
        Assert.IsTrue(b.Ability(3,5,8,1,0,8,First));
        int anchor=System.Array.FindIndex(b.largeStone,c=>c==2);
        Assert.Greater(anchor%19,7);Assert.AreEqual(4,System.Array.FindAll(b.cells,c=>c==2).Length);
        for(int ay=0;ay<2;ay++)for(int ax=0;ax<2;ax++)Assert.AreEqual(2,b.cells[anchor+ay*19+ax]);
    }
    [Test]
    public void LargeStoneCannotMoveThroughAnObstacleOnItsOtherRow()
    {
        var b=new AugmentBoard();Assert.IsTrue(b.Ability(6,5,8,1,0,1,First));
        b.cells[9*19+7]=3;int[] before=(int[])b.cells.Clone();
        Assert.IsFalse(b.Ability(3,5,8,1,0,8,First));
        CollectionAssert.AreEqual(before,b.cells);Assert.AreEqual(1,b.largeStone[8*19+5]);
    }
    [TestCase(14)][TestCase(16)][TestCase(7)][TestCase(8)]
    public void FlickKeepsSpecialPropertiesWhenPushingAStone(int kind)
    {
        var b=new AugmentBoard();int source=9*19+7;
        b.cells[9*19+5]=1;b.cells[source]=kind==8?4:2;
        if(kind==14){b.giant[2]=source;b.giantTurns[2]=2;}
        if(kind==16)b.hidden[source]=2;
        if(kind==7){b.piece=source;b.pieceOwner=2;b.pieceKind=1;}
        Assert.IsTrue(b.Ability(3,5,9,1,0,8,First));
        if(kind==14){Assert.Greater(b.giant[2],source);Assert.AreEqual(2,b.cells[b.giant[2]]);Assert.AreEqual(2,b.giantTurns[2]);}
        if(kind==16){int target=System.Array.FindIndex(b.hidden,c=>c==2);Assert.Greater(target,source);Assert.AreEqual(2,b.cells[target]);}
        if(kind==7){Assert.Greater(b.piece,source);Assert.AreEqual(2,b.cells[b.piece]);Assert.AreEqual(2,b.pieceOwner);Assert.AreEqual(1,b.pieceKind);}
        if(kind==8)Assert.AreEqual(1,System.Array.FindAll(b.cells,c=>c==4).Length);
    }
    [Test]
    public void SnapshotRoundTripKeepsBoardAndPendingEffects()
    {
        var b=new AugmentBoard();b.Ability(16,9,9,1,0,1,First);b.takeover=2;b.grayNext=2;
        var copy=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(b));
        CollectionAssert.AreEqual(b.cells,copy.cells);CollectionAssert.AreEqual(b.hidden,copy.hidden);CollectionAssert.AreEqual(b.largeStone,copy.largeStone);
        Assert.AreEqual(2,copy.takeover);Assert.AreEqual(2,copy.grayNext);
    }
}
