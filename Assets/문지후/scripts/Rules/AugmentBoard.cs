using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authoritative, serializable grid state. All randomness is supplied by the host.</summary>
[Serializable]
public sealed class AugmentBoard
{
    public int[] cells = new int[361], order = new int[361], hidden = new int[361];
    public int turn = 1, winner, moves, sequence;
    public int grayNext, convertNext, takeover, capture, replay, noSix;
    public int[] giant = { -1, -1, -1 }, giantTurns = new int[3];
    public int piece = -1, pieceOwner, pieceKind;
    public static bool Inside(int x, int y) => x >= 0 && x < 19 && y >= 0 && y < 19;
    public int At(int x, int y) => Inside(x, y) ? cells[y * 19 + x] : 5;
    private bool Mutable(int p) => p >= 0 && p < 361 && cells[p] != 3 && cells[p] != 5;
    private void Set(int p, int color)
    {
        if (!Mutable(p)) return;
        for(int side=1;side<=2;side++)if(giant[side]==p)giant[side]=-1;
        cells[p] = color; hidden[p] = 0; order[p] = ++sequence;
        if (piece == p && color != pieceOwner) piece = -1;
    }
    private int Run(int x, int y, int dx, int dy, int color)
    {
        int n = 0;
        for (x += dx, y += dy; Inside(x, y) && At(x, y) == color; x += dx, y += dy) n++;
        return n;
    }
    private static readonly Vector2Int[] Axes = { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,-1) };
    public bool Legal(int x, int y, int color, bool augmented)
    {
        if (!Inside(x,y) || At(x,y) != 0 || winner != 0) return false;
        int fours = 0, fives = 0;
        foreach (var d in Axes)
        {
            int n = 1 + Run(x,y,d.x,d.y,color) + Run(x,y,-d.x,-d.y,color);
            if ((augmented || noSix == color || replay > 0) && n >= 6) return false;
            if (replay > 0 && n == 5) return false;
            if (n == 4) fours++; if (n == 5) fives++;
        }
        return !(color == 1 || augmented) || (fours < 2 && fives < 2 && (!augmented || fours == 0 || fives == 0));
    }
    public bool Place(int x, int y, bool augmented)
    {
        if (!Legal(x,y,turn,augmented)) return false;
        if (capture == turn) Capture(turn);
        int p = y * 19 + x;
        int color = grayNext == turn ? 4 : convertNext == turn ? 3 - turn : turn;
        Set(p,color);
        if (grayNext == turn) { grayNext = 0; noSix = 3-turn; }
        if (convertNext == turn) convertNext = 0;
        if (capture == turn) { Capture(turn); capture = 0; }
        CheckWinner();
        if (replay > 0) { replay--; if (replay > 0) return true; }
        FinishTurn(); return true;
    }
    private void FinishTurn()
    {
        moves++; if(takeover==turn)takeover=0;
        if(noSix==turn)noSix=0;
        if (giant[turn] >= 0)
        {
            if (cells[giant[turn]] != turn) giant[turn] = -1;
            else if (++giantTurns[turn] >= 4) winner = turn;
        }
        turn = 3 - turn;
    }
    public void CheckWinner()
    {
        for (int p = 0; p < 361 && winner == 0; p++)
        {
            int color = cells[p]; if (color != 1 && color != 2) continue;
            foreach (var d in Axes)
                if (1 + Run(p%19,p/19,d.x,d.y,color) >= 6) { winner = color; break; }
        }
    }
    private void Capture(int owner)
    {
        var seen = new HashSet<int>();
        for (int p = 0; p < 361; p++)
        {
            if (cells[p] != 3-owner || !seen.Add(p)) continue;
            var group = new List<int> { p }; bool liberty = false;
            for (int i=0;i<group.Count;i++)
                foreach (var d in new[] {Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
                {
                    int x=group[i]%19+d.x,y=group[i]/19+d.y;
                    if (!Inside(x,y)) continue;
                    int q=y*19+x; if(cells[q]==0) liberty=true;
                    if(cells[q]==3-owner && seen.Add(q)) group.Add(q);
                }
            if(!liberty) foreach(int q in group) Set(q,0);
        }
    }
    private bool Move(int p,int q)
    {
        if (!Mutable(p)||!Mutable(q)||cells[p]==0||cells[q]!=0) return false;
        int color=cells[p], stamp=order[p], invisible=hidden[p];
        bool wasPiece=piece==p; int giantSide=giant[1]==p?1:giant[2]==p?2:0;
        Set(p,0);Set(q,color);order[q]=stamp;hidden[q]=invisible;
        if(wasPiece) piece=q;
        if(giantSide!=0)giant[giantSide]=q;
        return true;
    }
    public bool Ability(int id,int x,int y,int dx,int dy,int power,Func<int,int,int> random)
    {
        if(winner!=0||!Inside(x,y)) return false;
        int p=y*19+x, own=turn, enemy=3-turn;
        dx=Math.Sign(dx);dy=Math.Sign(dy);power=Math.Max(1,Math.Min(3,power));
        if(id==0) return Ability(random(1,21),x,y,dx,dy,power,random);
        switch(id)
        {
            case 1: // Shotgun: destroy center, push neighbours in host-random directions.
                if(!Mutable(p)) return false;
                Set(p,0);
                var nearby=new List<int>();
                for(int ay=-1;ay<=1;ay++)for(int ax=-1;ax<=1;ax++)if(Inside(x+ax,y+ay)&&At(x+ax,y+ay)!=0)nearby.Add((y+ay)*19+x+ax);
                foreach(int q in nearby) {int nx=q%19+random(-1,2),ny=q/19+random(-1,2);if(Inside(nx,ny))Move(q,ny*19+nx);}
                break;
            case 2: if(!Mutable(p))return false; Set(p,own); break;
            case 3: // Flick along a direction, knocking a hit stone one cell farther.
            case 9:
                if(dx==0&&dy==0)return false;
                if(id==3&&cells[p]!=own)return false;
                int tx=x+dx*power,ty=y+dy*power;
                if(!Inside(tx,ty))return false;
                int dest=ty*19+tx;
                if(!Mutable(dest))return false;
                if(cells[dest]!=0) {int nx=tx+dx,ny=ty+dy;if(Inside(nx,ny)&&At(nx,ny)==0)Move(dest,ny*19+nx);else Set(dest,0);}
                if(id==3)Move(p,dest);else Set(dest,own);
                break;
            case 4: capture=own; Capture(own); break;
            case 5:
                int count=0; for(int i=0;i<361;i++)if(cells[i]==own){Set(i,0);count++;}
                if(count==0)return false; replay=count; break;
            case 6:
                if(!Inside(x+1,y+1))return false;
                for(int ay=0;ay<2;ay++)for(int ax=0;ax<2;ax++)if(At(x+ax,y+ay)!=0)return false;
                for(int ay=0;ay<2;ay++)for(int ax=0;ax<2;ax++)Set((y+ay)*19+x+ax,own);
                break;
            case 7:
                if(cells[p]!=0)return false; Set(p,own);piece=p;pieceOwner=own;pieceKind=random(0,3);break;
            case 8: grayNext=enemy;break;
            case 10: if(cells[p]!=0)return false;Set(p,3);break;
            case 11:
                for(int ay=1-power;ay<power;ay++)for(int ax=1-power;ax<power;ax++)if(Inside(x+ax,y+ay))Set((y+ay)*19+x+ax,0);
                if(!Mutable(p))return false;Set(p,own);break;
            case 12: convertNext=enemy;break;
            case 13:
                if(dx==0&&dy==0)return false;
                var line=new List<int>();int lx=x,ly=y;
                while(Inside(lx-dx,ly-dy)){lx-=dx;ly-=dy;}
                for(;Inside(lx,ly);lx+=dx,ly+=dy)line.Add(ly*19+lx);
                // Anchored cells split the row; never sort across a permanent obstacle.
                var segment=new List<int>();
                foreach(int q in line) {if(!Mutable(q)){Sort(segment);segment.Clear();}else segment.Add(q);}Sort(segment);break;
            case 14:
                if(!Mutable(p))return false;
                for(int i=0;i<361;i++)if(cells[i]==own)Set(i,0);
                Set(p,own);giant[own]=p;giantTurns[own]=0;break;
            case 15:
                for(int ay=-1;ay<=1;ay++)for(int ax=-1;ax<=1;ax++)if(Inside(x+ax,y+ay))Set((y+ay)*19+x+ax,5);
                break;
            case 16: if(cells[p]!=0)return false;Set(p,own);hidden[p]=own;break;
            case 17:
                if((dx==0&&dy==0)||!Inside(x+dx,y+dy)||!Move(p,(y+dy)*19+x+dx))return false;break;
            case 18: takeover=enemy;break;
            case 19:
                for(int ay=-1;ay<=1;ay++)for(int ax=-1;ax<=1;ax++)if(Inside(x+ax,y+ay))Set((y+ay)*19+x+ax,random(1,3));break;
            case 20:
                if(random(0,2)==0) {for(int i=0;i<361;i++)if(cells[i]==1||cells[i]==2)Set(i,3-cells[i]);}
                else {var owned=new List<int>();for(int i=0;i<361;i++)if(cells[i]==own)owned.Add(i);for(int i=0;i<3&&owned.Count>0;i++){int n=random(0,owned.Count);Set(owned[n],0);owned.RemoveAt(n);}}break;
            default:return false;
        }
        CheckWinner();return true;
    }
    private void Sort(List<int> segment)
    {
        var stones=segment.FindAll(p=>cells[p]!=0);
        stones.Sort((a,b)=>order[a].CompareTo(order[b]));
        int[] colors=stones.ConvertAll(p=>cells[p]).ToArray();
        foreach(int p in segment)Set(p,0);
        for(int i=0;i<colors.Length;i++)Set(segment[i],colors[i]);
    }
    public bool MovePiece(int x,int y)
    {
        if(piece<0||pieceOwner!=turn||!Inside(x,y))return false;
        int dx=Math.Abs(x-piece%19),dy=Math.Abs(y-piece/19),q=y*19+x;
        bool valid=pieceKind==0?(dx+dy==1):pieceKind==1?(dx==1&&dy==2||dx==2&&dy==1):(Math.Max(dx,dy)==1);
        if(!valid||!Mutable(q)||cells[q]==turn)return false;
        if(cells[q]!=0){Set(q,0);Set(piece,0);piece=-1;}
        else Move(piece,q);
        CheckWinner();FinishTurn();return true;
    }
}
