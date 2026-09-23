using NUnit.Framework;
using UnityEngine;

public class 육목_규칙_테스트
{
    [Test]
    public void 증강을_쓰지_않으면_육목을_허용한다()
    {
        육목_규칙 규칙 = 새규칙((2, 9), (3, 9), (4, 9), (5, 9), (6, 9));

        Assert.AreEqual(착수_금지_사유.없음, 규칙.착수검사(new Vector2Int(7, 9), 돌_색.검정, false));
    }

    [Test]
    public void 증강을_쓴_턴에는_육목을_금지한다()
    {
        육목_규칙 규칙 = 새규칙((2, 9), (3, 9), (4, 9), (5, 9), (6, 9));

        Assert.AreEqual(착수_금지_사유.육목, 규칙.착수검사(new Vector2Int(7, 9), 돌_색.검정, true));
    }

    [Test]
    public void 증강효과가_만드는_육목은_예외다()
    {
        육목_규칙 규칙 = 새규칙((2, 9), (3, 9), (4, 9), (5, 9), (6, 9));

        Assert.AreEqual(착수_금지_사유.없음, 규칙.착수검사(new Vector2Int(7, 9), 돌_색.검정, true, true));
    }

    [Test]
    public void 증강을_쓴_턴에는_사사를_금지한다()
    {
        육목_규칙 규칙 = 새규칙(
            (6, 9), (7, 9), (8, 9),
            (9, 6), (9, 7), (9, 8));

        Assert.AreEqual(착수_금지_사유.사사, 규칙.착수검사(new Vector2Int(9, 9), 돌_색.검정, true));
    }

    [Test]
    public void 평상시에도_흑돌_사사를_금지한다()
    {
        육목_규칙 규칙 = 새규칙(
            (6, 9), (7, 9), (8, 9),
            (9, 6), (9, 7), (9, 8));

        Assert.AreEqual(착수_금지_사유.사사, 규칙.착수검사(new Vector2Int(9, 9), 돌_색.검정, false));
    }

    [Test]
    public void 증강을_쓴_턴에는_오오를_금지한다()
    {
        육목_규칙 규칙 = 새규칙(
            (5, 9), (6, 9), (7, 9), (8, 9),
            (9, 5), (9, 6), (9, 7), (9, 8));

        Assert.AreEqual(착수_금지_사유.오오, 규칙.착수검사(new Vector2Int(9, 9), 돌_색.검정, true));
    }

    [Test]
    public void 평상시에도_흑돌_오오를_금지한다()
    {
        육목_규칙 규칙 = 새규칙(
            (5, 9), (6, 9), (7, 9), (8, 9),
            (9, 5), (9, 6), (9, 7), (9, 8));

        Assert.AreEqual(착수_금지_사유.오오, 규칙.착수검사(new Vector2Int(9, 9), 돌_색.검정, false));
    }

    [Test]
    public void 평상시_백돌은_사사와_오오를_허용한다()
    {
        육목_규칙 사사규칙 = 새규칙(돌_색.흰색,
            (6, 9), (7, 9), (8, 9),
            (9, 6), (9, 7), (9, 8));
        육목_규칙 오오규칙 = 새규칙(돌_색.흰색,
            (5, 9), (6, 9), (7, 9), (8, 9),
            (9, 5), (9, 6), (9, 7), (9, 8));

        Assert.AreEqual(착수_금지_사유.없음, 사사규칙.착수검사(new Vector2Int(9, 9), 돌_색.흰색, false));
        Assert.AreEqual(착수_금지_사유.없음, 오오규칙.착수검사(new Vector2Int(9, 9), 돌_색.흰색, false));
    }

    [Test]
    public void 증강을_쓴_턴에는_사오를_금지한다()
    {
        육목_규칙 규칙 = 새규칙(
            (5, 9), (6, 9), (7, 9), (8, 9),
            (9, 6), (9, 7), (9, 8));

        Assert.AreEqual(착수_금지_사유.사오, 규칙.착수검사(new Vector2Int(9, 9), 돌_색.검정, true));
    }

    [Test]
    public void 이미_놓인_자리는_항상_금지한다()
    {
        육목_규칙 규칙 = 새규칙((9, 9));

        Assert.AreEqual(착수_금지_사유.이미_놓인_자리, 규칙.착수검사(new Vector2Int(9, 9), 돌_색.흰색, false));
    }

    private static 육목_규칙 새규칙(params (int x, int y)[] 좌표들)
    {
        return 새규칙(돌_색.검정, 좌표들);
    }

    private static 육목_규칙 새규칙(돌_색 색, params (int x, int y)[] 좌표들)
    {
        육목_규칙 규칙 = new 육목_규칙();
        foreach ((int x, int y) in 좌표들)
        {
            bool 성공 = 규칙.돌놓기(new Vector2Int(x, y), 색, false, false, out _);
            Assert.IsTrue(성공);
        }
        return 규칙;
    }
}
