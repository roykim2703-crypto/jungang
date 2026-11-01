using NUnit.Framework;

public class ShotgunTests
{
    [Test]
    public void ClearsAllNineStonesWithoutMovingOrUsingRandomness()
    {
        var board=new AugmentBoard();
        for(int y=8;y<=10;y++)for(int x=8;x<=10;x++)board.cells[y*19+x]=(x+y)%2+1;
        board.cells[9*19+9]=4;board.cells[9*19+11]=2;
        Assert.IsTrue(board.Ability(1,9,9,1,0,1,(min,max)=>throw new System.Exception("Shotgun must not use randomness")));
        for(int y=8;y<=10;y++)for(int x=8;x<=10;x++)Assert.AreEqual(0,board.At(x,y));
        Assert.AreEqual(2,board.At(11,9));
    }
    [Test]
    public void ClearsNeighboursEvenWhenCenterIsPermanent()
    {
        var board=new AugmentBoard();board.cells[9*19+9]=3;board.cells[9*19+10]=1;board.cells[8*19+9]=5;
        Assert.IsTrue(board.Ability(1,9,9,1,0,1,(min,max)=>min));
        Assert.AreEqual(3,board.At(9,9));Assert.AreEqual(5,board.At(9,8));Assert.AreEqual(0,board.At(10,9));
    }
    [Test]
    public void EdgeShotClipsToBoard()
    {
        var board=new AugmentBoard();board.cells[0]=1;board.cells[1]=2;board.cells[19]=2;board.cells[20]=1;board.cells[2]=1;
        Assert.IsTrue(board.Ability(1,0,0,1,0,1,(min,max)=>min));
        Assert.AreEqual(0,board.At(0,0));Assert.AreEqual(0,board.At(1,0));Assert.AreEqual(0,board.At(0,1));Assert.AreEqual(0,board.At(1,1));
        Assert.AreEqual(1,board.At(2,0));
    }
}
