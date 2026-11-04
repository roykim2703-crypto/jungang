using NUnit.Framework;
using UnityEngine;

public class AugmentBoardTests
{
    private static int First(int min,int max)=>min;
    [TestCase(1)][TestCase(2)]
    public void ConversionCueRecordsThePlacedColorAndTheFinalColor(int side)
    {
        var b=new AugmentBoard{turn=side,convertNext=side};
        Assert.IsTrue(b.Place(9,9,false));Assert.AreEqual(3-side,b.At(9,9));
        Assert.AreEqual(1,b.stoneConversions.Count);var cue=b.stoneConversions[0];
        Assert.AreEqual(180,cue.cell);Assert.AreEqual(side,cue.from);Assert.AreEqual(3-side,cue.to);
        Assert.IsTrue(b.Place(8,8,false));Assert.IsEmpty(b.stoneConversions);
    }
    [Test]
    public void ConversionCueIsNotEmittedWhenGrayOverridesItOrAHiddenStoneBlocksPlacement()
    {
        var b=new AugmentBoard{grayNext=1,convertNext=1};Assert.IsTrue(b.Place(9,9,false));
        Assert.AreEqual(4,b.At(9,9));Assert.IsEmpty(b.stoneConversions);
        b=new AugmentBoard{convertNext=1};b.cells[180]=b.hidden[180]=2;
        Assert.IsTrue(b.Place(9,9,false,out bool revealed));Assert.IsTrue(revealed);Assert.IsEmpty(b.stoneConversions);
    }
    [Test]
    public void ConversionCueAlsoTracksAnAutomaticReservation()
    {
        var b=new AugmentBoard{convertNext=2,whitePremoves=new[]{10,11,12,13,14,15}};
        Assert.IsTrue(b.Place(0,0,false));Assert.AreEqual(1,b.stoneConversions.Count);
        Assert.AreEqual(10,b.stoneConversions[0].cell);Assert.AreEqual(2,b.stoneConversions[0].from);Assert.AreEqual(1,b.stoneConversions[0].to);
    }
    [Test]
    public void CaptureRecordsEachRemovedStoneOnceAndClearsTheNextActivation()
    {
        var b=new AugmentBoard();b.cells[0]=b.cells[1]=2;b.cells[2]=b.cells[19]=b.cells[20]=1;
        Assert.IsTrue(b.Ability(4,9,9,1,0,1,First));
        Assert.AreEqual(2,b.capturedStones.Count);
        CollectionAssert.AreEquivalent(new[]{0,1},b.capturedStones.ConvertAll(s=>s.cell));
        foreach(var stone in b.capturedStones){Assert.AreEqual(2,stone.color);Assert.AreEqual(0,b.cells[stone.cell]);}
        Assert.IsTrue(b.Ability(8,9,9,1,0,1,First));Assert.IsEmpty(b.capturedStones);
    }
    [Test]
    public void CaptureOnTheFollowingPlacementAlsoRecordsItsPresentation()
    {
        var b=new AugmentBoard();b.cells[0]=2;b.cells[1]=1;
        Assert.IsTrue(b.Ability(4,9,9,1,0,1,First));Assert.IsEmpty(b.capturedStones);
        Assert.IsTrue(b.Place(0,1,false));Assert.AreEqual(0,b.cells[0]);
        Assert.AreEqual(1,b.capturedStones.Count);Assert.AreEqual(0,b.capturedStones[0].cell);
        Assert.IsTrue(b.Place(8,8,false));Assert.IsEmpty(b.capturedStones);
    }
    [Test]
    public void CapturePresentationExcludesHiddenAndProtectedStones()
    {
        var b=new AugmentBoard();b.cells[0]=b.cells[1]=2;b.hidden[0]=2;b.anchored[1]=2;
        b.cells[2]=b.cells[19]=b.cells[20]=1;
        Assert.IsTrue(b.Ability(4,9,9,1,0,1,First));
        Assert.AreEqual(0,b.cells[0]);Assert.AreEqual(2,b.cells[1]);Assert.IsEmpty(b.capturedStones);
    }
    [Test]
    public void SortRecordsIdentityEvenWhenSameColoredStonesSwapPlaces()
    {
        var b=new AugmentBoard();b.cells[0]=b.cells[1]=1;b.order[0]=20;b.order[1]=10;
        Assert.IsTrue(b.Ability(13,0,0,1,0,1,First));
        Assert.AreEqual(2,b.sortMotions.Count);
        Assert.AreEqual(1,b.sortMotions[0].from);Assert.AreEqual(0,b.sortMotions[0].to);Assert.AreEqual(1,b.sortMotions[0].number);
        Assert.AreEqual(0,b.sortMotions[1].from);Assert.AreEqual(1,b.sortMotions[1].to);Assert.AreEqual(2,b.sortMotions[1].number);
        Assert.AreEqual(1,b.At(0,0));Assert.AreEqual(1,b.At(1,0));
        Assert.IsTrue(b.Ability(8,0,0,1,0,1,First));Assert.IsEmpty(b.sortMotions);
    }
    [TestCase(1,0)][TestCase(0,1)][TestCase(1,1)][TestCase(-1,1)]
    public void SortNumbersRestartAfterAnAnchoredObstacle(int dx,int dy)
    {
        var b=new AugmentBoard();int center=9*19+9;
        int step=dy*19+dx,first=center-step*2,last=center+step*2;
        b.cells[center]=2;b.anchored[center]=1;
        b.cells[first]=1;b.order[first]=20;b.cells[last]=2;b.order[last]=10;
        Assert.IsTrue(b.Ability(13,9,9,dx,dy,1,First));
        Assert.AreEqual(2,b.sortMotions.Count);
        Assert.AreEqual(1,b.sortMotions[0].number);Assert.AreEqual(1,b.sortMotions[1].number);
        foreach(var motion in b.sortMotions)
        {
            Assert.AreNotEqual(center,motion.from);Assert.AreNotEqual(center,motion.to);
            Assert.AreEqual(motion.color,b.cells[motion.to]);
            Assert.AreEqual(System.Math.Sign(motion.from-center),System.Math.Sign(motion.to-center));
        }
        Assert.AreEqual(2,b.cells[center]);
    }
    [Test]
    public void CatapultClearsTheCenterAndPushesAllEightNeighborsOutwardOnce()
    {
        var b=new AugmentBoard();b.cells[180]=2;
        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
        {
            if(dx==0&&dy==0)continue;int p=(9+dy)*19+9+dx;
            b.cells[p]=dx==0?4:dx<0?1:2;b.order[p]=p;
        }
        b.hidden[9*19+10]=2;b.piece=9*19+10;b.pieceOwner=2;b.pieceKind=1;
        Assert.IsTrue(b.Ability(9,9,13,0,-1000,1,First));
        Assert.IsTrue(b.IsAnchored(9,9));Assert.AreEqual(1,b.At(9,9));
        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
        {
            if(dx==0&&dy==0)continue;
            Assert.AreEqual(0,b.At(9+dx,9+dy));
            Assert.AreEqual(dx==0?4:dx<0?1:2,b.At(9+dx*2,9+dy*2));
        }
        Assert.AreEqual(2,b.hidden[9*19+11]);Assert.AreEqual(9*19+11,b.piece);
    }
    [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
    [TestCase(9)][TestCase(10)][TestCase(11)][TestCase(13)][TestCase(14)][TestCase(15)]
    [TestCase(16)][TestCase(17)][TestCase(19)][TestCase(20)]
    public void AbsoluteCatapultStoneCannotBeRemovedMovedOrRecolored(int ability)
    {
        var b=new AugmentBoard();Assert.IsTrue(b.Ability(9,9,13,0,-1,1,First));
        b.cells[0]=b.cells[2]=1;b.order[0]=1;b.order[2]=2;
        b.Ability(ability,9,9,1,0,3,First);
        Assert.AreEqual(1,b.At(9,9));Assert.IsTrue(b.IsAnchored(9,9));
        var copy=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(b));Assert.IsTrue(copy.IsAnchored(9,9));
    }
    [Test]
    public void CatapultPreservesProtectedCellsAndMovesLargeStonesAsOneObject()
    {
        var b=new AugmentBoard();b.cells[9*19+8]=2;b.cells[9*19+7]=3;
        Assert.IsTrue(b.Ability(6,10,9,1,0,1,First));
        Assert.IsTrue(b.Ability(9,9,13,0,-1,1,First));
        Assert.AreEqual(2,b.At(8,9));Assert.AreEqual(3,b.At(7,9));
        Assert.AreEqual(1,b.largeStone[10*19+11]);Assert.AreEqual(0,b.largeStone[9*19+10]);
        Assert.IsFalse(b.Ability(9,9,13,0,-1,1,First));
        var hole=new AugmentBoard();hole.cells[180]=5;Assert.IsTrue(hole.Ability(9,9,13,0,-1,1,First));Assert.IsTrue(hole.IsAnchored(9,9));
    }
    [Test]
    public void AbsoluteCatapultStoneStillCountsForItsOwnersSix()
    {
        var b=new AugmentBoard();for(int x=4;x<9;x++)b.cells[9*19+x]=1;
        Assert.IsTrue(b.Ability(9,9,13,0,-1,1,First));
        // The adjacent fifth stone is pushed away; complete a different axis through the anchor.
        for(int y=4;y<9;y++)b.cells[y*19+9]=1;b.CheckWinner();Assert.AreEqual(1,b.winner);
    }
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
    public void GolemIsAnIndependentThreeByThreeOccupantAndDoesNotConnectStones()
    {
        var b=new AugmentBoard();b.cells[0]=1;Assert.IsTrue(b.Ability(14,9,9,1,0,1,First));
        Assert.AreEqual(0,b.cells[0]);Assert.AreEqual(9,System.Array.FindAll(b.cells,c=>c==AugmentBoard.GolemCell).Length);
        for(int y=8;y<=10;y++)for(int x=8;x<=10;x++)Assert.IsFalse(b.Legal(x,y,1,false));
        Assert.IsFalse(b.Ability(2,9,9,1,0,1,First));Assert.IsFalse(b.Ability(17,9,9,1,0,1,First));
        for(int x=3;x<=7;x++)b.cells[9*19+x]=1;
        b.CheckWinner();Assert.AreEqual(0,b.winner);Assert.AreEqual(180,b.giant[1]);
    }
    [TestCase(-1,-1)][TestCase(0,-1)][TestCase(1,-1)]
    [TestCase(-1,0)][TestCase(0,0)][TestCase(1,0)]
    [TestCase(-1,1)][TestCase(0,1)][TestCase(1,1)]
    public void AOneCellExplosionAtAnyGolemCellDestroysTheWholeEntity(int ox,int oy)
    {
        var b=new AugmentBoard();Assert.IsTrue(b.Ability(14,9,9,1,0,1,First));b.turn=2;
        Assert.IsTrue(b.Ability(11,9+ox,9+oy,1,0,1,First));
        Assert.AreEqual(-1,b.giant[1]);Assert.AreEqual(0,b.giantTurns[1]);
        Assert.AreEqual(0,System.Array.FindAll(b.cells,c=>c==AugmentBoard.GolemCell).Length);
    }
    [TestCase(0,0)][TestCase(18,18)][TestCase(0,9)][TestCase(9,18)]
    public void GolemRequiresItsWholeFootprintInsideTheBoard(int x,int y)
    {
        var b=new AugmentBoard();b.cells[1]=1;
        Assert.IsFalse(b.Ability(14,x,y,1,0,1,First));Assert.AreEqual(1,b.cells[1]);Assert.AreEqual(-1,b.giant[1]);
    }
    [Test]
    public void GolemFootprintBlocksSummoningOnEnemiesAndSurvivesRecolorAndSort()
    {
        var b=new AugmentBoard();b.cells[8*19+8]=2;b.cells[0]=1;
        Assert.IsFalse(b.Ability(14,9,9,1,0,1,First));Assert.AreEqual(1,b.cells[0]);
        b.cells[8*19+8]=0;Assert.IsTrue(b.Ability(14,9,9,1,0,1,First));
        Assert.IsTrue(b.Ability(19,9,9,1,0,1,First));Assert.IsTrue(b.Ability(20,9,9,1,0,1,First));
        Assert.IsTrue(b.Ability(13,9,9,1,0,1,First));
        Assert.AreEqual(9,System.Array.FindAll(b.cells,c=>c==AugmentBoard.GolemCell).Length);Assert.AreEqual(180,b.giant[1]);
    }
    [Test]
    public void GolemSurvivalAndDestructionSurviveSnapshots()
    {
        var b=new AugmentBoard();Assert.IsTrue(b.Ability(14,9,9,1,0,1,First));
        b=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(b));
        for(int round=0;round<4;round++)
        {
            Assert.IsTrue(b.Place(0,round*2,false));
            if(round<3){Assert.AreEqual(0,b.winner);Assert.IsTrue(b.Place(18,round*2,false));}
        }
        Assert.AreEqual(1,b.winner);Assert.AreEqual(4,b.giantTurns[1]);
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
    [TestCase(16)][TestCase(7)][TestCase(8)]
    public void FlickKeepsSpecialPropertiesWhenPushingAStone(int kind)
    {
        var b=new AugmentBoard();int source=9*19+7;
        b.cells[9*19+5]=1;b.cells[source]=kind==8?4:2;
        if(kind==16)b.hidden[source]=2;
        if(kind==7){b.piece=source;b.pieceOwner=2;b.pieceKind=1;}
        Assert.IsTrue(b.Ability(3,5,9,1,0,8,First));
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
