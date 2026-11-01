using NUnit.Framework;
using UnityEngine;

public class MeteorChargeTests
{
    [Test]
    public void ChargeLocksPositionAndAcceptsOnlyOwnersClicksBeforeDeadline()
    {
        var charge=new MeteorCharge();charge.Start(2,8,12,10);
        Assert.AreEqual(3,charge.Remaining(10));
        Assert.IsFalse(charge.Tap(1,11));Assert.IsTrue(charge.Tap(2,12.99));
        Assert.IsFalse(charge.Tap(2,13));Assert.AreEqual(1,charge.clicks);
        Assert.AreEqual(8,charge.x);Assert.AreEqual(12,charge.y);
    }
    [TestCase(0,3)][TestCase(12,3)][TestCase(13,5)][TestCase(20,5)]
    [TestCase(21,7)][TestCase(32,7)][TestCase(33,9)][TestCase(100,9)]
    public void MoreClicksIncreaseMeteorFootprint(int clicks,int size)
    {
        int power=MeteorCharge.Power(clicks);
        Assert.AreEqual(size*size,AugmentAim.Cells(11,new Vector2Int(9,9),Vector2Int.right,power).Count);
        var board=new AugmentBoard();for(int p=0;p<361;p++)board.cells[p]=2;
        Assert.IsTrue(board.Ability(11,9,9,1,0,power,(min,max)=>min));
        int removed=0;foreach(int c in board.cells)if(c==0)removed++;
        Assert.AreEqual(size*size-1,removed);
    }
    [Test]
    public void RemovedSupportCannotBeUsedOrDrawnByMysteryBox()
    {
        Assert.IsFalse(new AugmentBoard().Ability(7,9,9,1,0,1,(min,max)=>min));
        for(int seed=0;seed<100;seed++)
        {
            var board=new AugmentBoard();var random=new System.Random(seed);
            Assert.IsTrue(board.Ability(0,9,9,1,0,2,random.Next));Assert.AreNotEqual(7,board.lastAbility);
        }
    }
}
