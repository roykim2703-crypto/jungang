using System;
using UnityEngine;

public enum 돌_색
{
    없음,
    검정,
    흰색
}

public enum 착수_금지_사유
{
    없음,
    바둑판_밖,
    이미_놓인_자리,
    육목,
    오오,
    사오,
    사사
}

/// <summary>
/// 19x19 보드의 돌 상태와 육목 금수 판정을 소유한다.
/// </summary>
public sealed class 육목_규칙
{
    public const int 보드크기 = 19;

    private static readonly Vector2Int[] 검사방향 =
    {
        Vector2Int.right,
        Vector2Int.up,
        new Vector2Int(1, 1),
        new Vector2Int(1, -1)
    };

    private readonly 돌_색[,] 보드 = new 돌_색[보드크기, 보드크기];

    public int 완성된_사목_수 { get; private set; }
    public int 획득한_증강_수 { get; private set; } = 1;

    public event Action<int> 증강_획득;

    public 돌_색 돌가져오기(Vector2Int 좌표)
    {
        return 범위안(좌표) ? 보드[좌표.x, 좌표.y] : 돌_색.없음;
    }

    public 착수_금지_사유 착수검사(Vector2Int 좌표, 돌_색 색, bool 증강사용중, bool 증강효과로놓는돌 = false)
    {
        if (!범위안(좌표)) return 착수_금지_사유.바둑판_밖;
        if (보드[좌표.x, 좌표.y] != 돌_색.없음) return 착수_금지_사유.이미_놓인_자리;

        // 증강 효과가 직접 만드는 돌은 6목/55/45/44 예외다.
        if (증강효과로놓는돌) return 착수_금지_사유.없음;

        bool 평상시흑돌금수검사 = 색 == 돌_색.검정;
        if (!증강사용중 && !평상시흑돌금수검사) return 착수_금지_사유.없음;

        int 사목 = 0;
        int 오목 = 0;

        foreach (Vector2Int 방향 in 검사방향)
        {
            int 연속수 = 1 + 같은돌수(좌표, 방향, 색) + 같은돌수(좌표, -방향, 색);
            if (증강사용중 && 연속수 >= 6) return 착수_금지_사유.육목;
            if (연속수 == 5) 오목++;
            if (연속수 == 4) 사목++;
        }

        if (오목 >= 2) return 착수_금지_사유.오오;
        if (증강사용중 && 오목 >= 1 && 사목 >= 1) return 착수_금지_사유.사오;
        if (사목 >= 2) return 착수_금지_사유.사사;
        return 착수_금지_사유.없음;
    }

    public bool 돌놓기(Vector2Int 좌표, 돌_색 색, bool 증강사용중, bool 증강효과로놓는돌, out 착수_금지_사유 금지사유)
    {
        금지사유 = 착수검사(좌표, 색, 증강사용중, 증강효과로놓는돌);
        if (금지사유 != 착수_금지_사유.없음) return false;

        보드[좌표.x, 좌표.y] = 색;
        int 새사목수 = 방향별_정확한연속수(좌표, 색, 4);
        for (int i = 0; i < 새사목수; i++) 사목완성처리();
        return true;
    }

    private void 사목완성처리()
    {
        완성된_사목_수++;
        if (완성된_사목_수 != 5 && 완성된_사목_수 != 15) return;

        획득한_증강_수++;
        증강_획득?.Invoke(획득한_증강_수);
    }

    private int 방향별_정확한연속수(Vector2Int 좌표, 돌_색 색, int 목표수)
    {
        int 개수 = 0;
        foreach (Vector2Int 방향 in 검사방향)
        {
            int 연속수 = 1 + 같은돌수(좌표, 방향, 색) + 같은돌수(좌표, -방향, 색);
            if (연속수 == 목표수) 개수++;
        }
        return 개수;
    }

    private int 같은돌수(Vector2Int 시작, Vector2Int 방향, 돌_색 색)
    {
        int 개수 = 0;
        Vector2Int 좌표 = 시작 + 방향;
        while (범위안(좌표) && 보드[좌표.x, 좌표.y] == 색)
        {
            개수++;
            좌표 += 방향;
        }
        return 개수;
    }

    private static bool 범위안(Vector2Int 좌표)
    {
        return 좌표.x >= 0 && 좌표.x < 보드크기 && 좌표.y >= 0 && 좌표.y < 보드크기;
    }
}
