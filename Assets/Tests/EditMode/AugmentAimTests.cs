using NUnit.Framework;
using UnityEngine;

public class AugmentAimTests
{
    [TestCase(-3,0,1,0,3)]
    [TestCase(2,0,-1,0,2)]
    [TestCase(0,2,0,-1,2)]
    [TestCase(-2,-2,1,1,3)]
    public void FlickLaunchesOppositeToPull(float x,float y,int dx,int dy,int expectedPower)
    {
        Assert.IsTrue(AugmentAim.Drag(new Vector2(9,9),new Vector2(9+x,9+y),true,out var direction,out int power));
        Assert.AreEqual(new Vector2Int(dx,dy),direction);Assert.AreEqual(expectedPower,power);
    }
    [Test]
    public void ClickWithoutDragCannotConsumeDirectionalAbility()
    {
        Assert.IsFalse(AugmentAim.Drag(Vector2.zero,new Vector2(.1f,.1f),true,out _,out _));
    }
    [Test]
    public void NormalDragUsesPointerDirectionAndCapsStrength()
    {
        AugmentAim.Drag(Vector2.zero,new Vector2(30,1),false,out var direction,out int power);
        Assert.AreEqual(Vector2Int.right,direction);Assert.AreEqual(3,power);
    }
    [TestCase(1,9)][TestCase(15,9)][TestCase(19,9)][TestCase(6,4)]
    public void RangeMatchesEffectFootprint(int id,int expectedCells)
    {
        Assert.AreEqual(expectedCells,AugmentAim.Cells(id,new Vector2Int(9,9),Vector2Int.right,1).Count);
    }
    [TestCase(1,1)][TestCase(2,9)][TestCase(3,25)]
    public void MeteorPreviewTracksPower(int power,int expectedCells)
    {
        Assert.AreEqual(expectedCells,AugmentAim.Cells(11,new Vector2Int(9,9),Vector2Int.right,power).Count);
    }
    [Test]
    public void PreviewClipsAtBoardEdgeAndShowsFlickLandingCell()
    {
        Assert.AreEqual(4,AugmentAim.Cells(1,Vector2Int.zero,Vector2Int.right,1).Count);
        var landing=AugmentAim.Cells(3,new Vector2Int(9,9),Vector2Int.right,3);
        CollectionAssert.AreEqual(new[]{new Vector2Int(12,9)},landing);
    }
    [Test]
    public void RowPreviewRunsThroughSelectedPoint()
    {
        var row=AugmentAim.Cells(13,new Vector2Int(9,5),Vector2Int.right,1);
        Assert.AreEqual(19,row.Count);Assert.AreEqual(new Vector2Int(0,5),row[0]);Assert.AreEqual(new Vector2Int(18,5),row[18]);
    }
}
