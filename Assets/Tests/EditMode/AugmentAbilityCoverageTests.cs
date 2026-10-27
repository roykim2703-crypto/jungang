using NUnit.Framework;
using UnityEngine;

public class AugmentAbilityCoverageTests
{
    [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
    [TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)][TestCase(12)][TestCase(13)]
    [TestCase(14)][TestCase(15)][TestCase(16)][TestCase(17)][TestCase(18)][TestCase(19)][TestCase(20)]
    public void EveryCatalogAbilityHasAnExecutableEffect(int id)
    {
        var b=new AugmentBoard();
        if(id==3||id==17)b.cells[9*19+9]=1;
        if(id==5)b.cells[0]=1;
        var random=new System.Random(12345);
        Assert.IsTrue(b.Ability(id,9,9,1,0,2,random.Next),"Ability "+id);
    }
    [Test]
    public void SeedAndInputsReproduceEveryEffect()
    {
        for(int id=0;id<21;id++)
        {
            var a=new AugmentBoard();var b=new AugmentBoard();
            a.cells[9*19+9]=b.cells[9*19+9]=id==3||id==17?1:0;
            a.cells[0]=b.cells[0]=1;
            var ra=new System.Random(42);var rb=new System.Random(42);
            Assert.AreEqual(a.Ability(id,9,9,1,0,2,ra.Next),b.Ability(id,9,9,1,0,2,rb.Next));
            Assert.AreEqual(JsonUtility.ToJson(a),JsonUtility.ToJson(b));
        }
    }
}
