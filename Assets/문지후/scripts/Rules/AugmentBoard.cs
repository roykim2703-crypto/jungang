using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authoritative, serializable grid state. All randomness is supplied by the host.</summary>
[Serializable]
public sealed class AugmentBoard
{
    public int[] cells = new int[361], order = new int[361], hidden = new int[361];
    // Owner is stored only at the top-left anchor. The four cells remain occupied for rules,
    // while rendering treats them as one 2x2-sized stone.
    public int[] largeStone = new int[361];
    public int turn = 1, winner, moves, sequence;
    public int grayNext, convertNext, takeover, capture, replay, noSix;
    public int[] giant = { -1, -1, -1 }, giantTurns = new int[3];
    public int piece = -1, pieceOwner, pieceKind;
    public int lastAbility = -1;
    // 1: front (swap colors), 2: back (remove own stones). Chosen only by the host.
    public int lastFlipResult;
    public int[] blackPremoves = new int[0], whitePremoves = new int[0];
    public int[] premoveIndex = new int[3];
    public int premoveSetup;
    [NonSerialized] public List<PremovePlacement> automaticPlacements = new List<PremovePlacement>();
    [NonSerialized] public int blockedPremoves;
    [NonSerialized] public bool lastPlacementVanished;
    public struct PremovePlacement { public int cell,color; public bool vanished; }
    public int[] Premoves(int side)=>side==1?blackPremoves:whitePremoves;
    public bool HasPendingPremoves=>premoveSetup!=0||(blackPremoves.Length==6&&premoveIndex[1]<6)||(whitePremoves.Length==6&&premoveIndex[2]<6);
    public bool QueuePremove(int x,int y)
    {
        if(winner!=0||premoveSetup!=turn||!Inside(x,y))return false;
        int visible=VisibleAt(x,y,turn);if(visible!=0&&visible!=5)return false;
        int cell=y*19+x;var queue=Premoves(turn);
        if(queue.Length>=6||Array.IndexOf(queue,cell)>=0)return false;
        Array.Resize(ref queue,queue.Length+1);queue[queue.Length-1]=cell;
        if(turn==1)blackPremoves=queue;else whitePremoves=queue;
        if(queue.Length==6)premoveSetup=0;
        return true;
    }
    public static bool Inside(int x, int y) => x >= 0 && x < 19 && y >= 0 && y < 19;
    public int At(int x, int y) => Inside(x, y) ? cells[y * 19 + x] : 5;
    public int VisibleAt(int x,int y,int viewer)
    {
        if(!Inside(x,y))return 5;
        int p=y*19+x;return hidden[p]!=0&&hidden[p]!=viewer?0:cells[p];
    }
    private bool Mutable(int p) => p >= 0 && p < 361 && cells[p] != 3 && cells[p] != 5;
    private void Set(int p, int color)
    {
        if (!Mutable(p)) return;
        ClearLargeStoneAt(p);
        for(int side=1;side<=2;side++)if(giant[side]==p)giant[side]=-1;
        cells[p] = color; hidden[p] = 0; order[p] = ++sequence;
        if (piece == p && color != pieceOwner) piece = -1;
    }
    private void ClearLargeStoneAt(int p)
    {
        if(largeStone==null||largeStone.Length!=361)largeStone=new int[361];
        int px=p%19,py=p/19;
        for(int ay=Math.Max(0,py-1);ay<=py;ay++)for(int ax=Math.Max(0,px-1);ax<=px;ax++)
        {
            int anchor=ay*19+ax;if(largeStone[anchor]==0)continue;
            largeStone[anchor]=0;
            for(int oy=0;oy<2;oy++)for(int ox=0;ox<2;ox++)
            {
                int q=(ay+oy)*19+ax+ox;cells[q]=0;hidden[q]=0;order[q]=++sequence;
                if(piece==q)piece=-1;
                for(int side=1;side<=2;side++)if(giant[side]==q)giant[side]=-1;
            }
        }
    }
    private int Run(int x, int y, int dx, int dy, int color,int viewer=0)
    {
        int n = 0;
        for (x += dx, y += dy; Inside(x, y); x += dx, y += dy)
        {
            int stone=viewer==0?At(x,y):VisibleAt(x,y,viewer);
            if(stone!=color&&stone!=4)break;
            n++;
        }
        return n;
    }
    private static readonly Vector2Int[] Axes = { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,-1) };
    public bool Legal(int x,int y,int color,bool augmented)=>LegalForView(x,y,color,augmented,0);
    public bool PreviewLegal(int x,int y,int color,bool augmented,int viewer)=>LegalForView(x,y,color,augmented,viewer);
    private bool LegalForView(int x, int y, int color, bool augmented,int viewer)
    {
        if(!Inside(x,y)||winner!=0)return false;
        int target=viewer==0?At(x,y):VisibleAt(x,y,viewer);
        if(target!=0&&target!=5)return false;
        if (replay > 0)
        {
            foreach (var d in Axes)
            {
                int n = 1 + Run(x,y,d.x,d.y,color,viewer) + Run(x,y,-d.x,-d.y,color,viewer);
                if (n >= 6) return false;
            }
            return true;
        }
        int fours = 0, fives = 0;
        foreach (var d in Axes)
        {
            int n = 1 + Run(x,y,d.x,d.y,color,viewer) + Run(x,y,-d.x,-d.y,color,viewer);
            if (augmented && n >= 6) return false;
            if (n == 4) fours++; if (n == 5) fives++;
        }
        return !(color == 1 || augmented) || (fours < 2 && fives < 2 && (!augmented || fours == 0 || fives == 0));
    }
    public bool Place(int x,int y,bool augmented)=>Place(x,y,augmented,out _);
    public bool Place(int x, int y, bool augmented,out bool revealedHiddenStone)
    {
        revealedHiddenStone=false;
        automaticPlacements.Clear();
        blockedPremoves=0;
        lastPlacementVanished=false;
        if(!Inside(x,y)||winner!=0||premoveSetup!=0)return false;
        int target=y*19+x;
        if(hidden[target]!=0&&hidden[target]!=turn)
        {
            // The attempted placement is accepted as a lost turn, not a newly placed stone.
            hidden[target]=0;revealedHiddenStone=true;replay=0;
            if(capture==turn)capture=0;
            FinishTurn();return true;
        }
        if (!Legal(x,y,turn,augmented)) return false;
        int p = y * 19 + x;
        bool blackHole=cells[p]==5;
        PutStone(p);
        CheckWinner();
        // The placed stone participates in victory before a non-winning stone is swallowed.
        if(blackHole&&winner==0){RestoreBlackHole(p);lastPlacementVanished=true;}
        if (replay > 0) { replay--; if (replay > 0) return true; }
        FinishTurn(); return true;
    }
    private int PutStone(int p)
    {
        // Only an accepted placement may temporarily occupy the protected black-hole terrain.
        if(cells[p]==5)cells[p]=0;
        if(capture==turn)Capture(turn);
        int color = grayNext == turn ? 4 : convertNext == turn ? 3 - turn : turn;
        Set(p,color);
        if (grayNext == turn) { grayNext = 0; noSix = 3-turn; }
        if (convertNext == turn) convertNext = 0;
        if (capture == turn) { Capture(turn); capture = 0; }
        return color;
    }
    private void RestoreBlackHole(int p)
    {
        cells[p]=5;hidden[p]=0;order[p]=++sequence;
    }
    private void FinishTurn()
    {
        FinishOneTurn();
        // Two groups per player. Automatic turns also finish when a group is blocked.
        for(int round=0;round<4&&winner==0;round++)
        {
            var queue=Premoves(turn);int index=premoveIndex[turn];
            if(queue.Length!=6||index>=6||premoveSetup!=0)break;
            premoveIndex[turn]+=3;
            bool blocked=false;
            for(int i=index;i<index+3;i++)
            {
                int cell=queue[i];
                if(cell<0||cell>=361||(cells[cell]!=0&&cells[cell]!=5))blocked=true;
            }
            if(blocked)blockedPremoves++;
            else
            {
                int first=automaticPlacements.Count;
                for(int i=index;i<index+3;i++)
                {
                    int cell=queue[i];bool blackHole=cells[cell]==5;
                    automaticPlacements.Add(new PremovePlacement{cell=cell,color=PutStone(cell),vanished=blackHole});
                }
                // All three reservations land together, including those inside black holes.
                CheckWinner();
                for(int i=first;i<automaticPlacements.Count;i++)
                {
                    var placement=automaticPlacements[i];if(!placement.vanished)continue;
                    if(winner==0)RestoreBlackHole(placement.cell);
                    else {placement.vanished=false;automaticPlacements[i]=placement;}
                }
            }
            CheckWinner();FinishOneTurn();
        }
    }
    private void FinishOneTurn()
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
        // Gray stones connect either color, but never constitute an owner by themselves.
        // The one-turn gray penalty suppresses every six check, including ability/premove checks.
        for (int pass=0;pass<2&&winner==0;pass++)
        for (int p = 0; p < 361 && winner == 0; p++)
        {
            int color=pass==0?turn:3-turn;
            if(cells[p]!=color||noSix==color)continue;
            foreach (var d in Axes)
                if (1 + Run(p%19,p/19,d.x,d.y,color)+Run(p%19,p/19,-d.x,-d.y,color) >= 6) { winner = color; break; }
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
    private int StoneAnchor(int p)
    {
        int x=p%19,y=p/19;
        for(int ay=Math.Max(0,y-1);ay<=y;ay++)for(int ax=Math.Max(0,x-1);ax<=x;ax++)
            if(largeStone[ay*19+ax]!=0)return ay*19+ax;
        return p;
    }
    private int[] StoneCells(int p)
    {
        int anchor=StoneAnchor(p);
        return largeStone[anchor]!=0?new[]{anchor,anchor+1,anchor+19,anchor+20}:new[]{p};
    }
    private bool Move(int p,int q)
    {
        if(!Mutable(p)||!Mutable(q)||cells[p]==0)return false;
        int dx=q%19-p%19,dy=q/19-p/19,anchor=StoneAnchor(p);
        int[] source=StoneCells(p),destination=new int[source.Length];
        for(int i=0;i<source.Length;i++)
        {
            int x=source[i]%19+dx,y=source[i]/19+dy;if(!Inside(x,y))return false;
            int target=y*19+x;destination[i]=target;
            if(!Mutable(target)||(cells[target]!=0&&Array.IndexOf(source,target)<0))return false;
        }
        int[] colors=new int[source.Length],stamps=new int[source.Length],invisible=new int[source.Length];
        int largeOwner=largeStone[anchor],movedPiece=Array.IndexOf(source,piece);
        int blackGiant=Array.IndexOf(source,giant[1]),whiteGiant=Array.IndexOf(source,giant[2]);
        for(int i=0;i<source.Length;i++)
        {
            int cell=source[i];colors[i]=cells[cell];stamps[i]=order[cell];invisible[i]=hidden[cell];
            cells[cell]=0;hidden[cell]=0;order[cell]=++sequence;
        }
        largeStone[anchor]=0;
        for(int i=0;i<source.Length;i++)
        {
            int cell=destination[i];cells[cell]=colors[i];order[cell]=stamps[i];hidden[cell]=invisible[i];
        }
        if(largeOwner!=0)largeStone[destination[0]]=largeOwner;
        if(movedPiece>=0)piece=destination[movedPiece];
        if(blackGiant>=0)giant[1]=destination[blackGiant];
        if(whiteGiant>=0)giant[2]=destination[whiteGiant];
        return true;
    }
    private bool FlickStone(int start,int rawDx,int rawDy,int power)
    {
        Vector2 direction=new Vector2(rawDx,rawDy).normalized;
        if(direction.sqrMagnitude<.001f||cells[start]!=turn)return false;
        int current=start,lastCell=start;float energy=Math.Max(1,Math.Min(8,power));
        Vector2 origin=new Vector2(start%19,start/19);
        for(int sample=1;sample<=power*5;sample++)
        {
            Vector2 point=origin+direction*(sample*.2f);
            int x=Mathf.RoundToInt(point.x),y=Mathf.RoundToInt(point.y);
            if(!Inside(x,y))break;
            int target=y*19+x;if(target==lastCell)continue;lastCell=target;
            if(!Mutable(target))break;
            if(largeStone[StoneAnchor(current)]!=0)
            {
                var step=new Vector2Int(x-current%19,y-current/19);
                bool collision=false;
                int[] footprint=StoneCells(current);
                foreach(int cell in footprint)
                {
                    int nx=cell%19+step.x,ny=cell/19+step.y;
                    if(Inside(nx,ny)&&cells[ny*19+nx]!=0&&Array.IndexOf(footprint,ny*19+nx)<0)collision=true;
                }
                // Move the complete footprint, including when the pointer selected a covered cell.
                if(!PushOne(current,step,Mathf.FloorToInt(energy)))break;
                current=target;energy-=.55f+(collision?1.5f:0);
                if(energy<=.25f)break;
                continue;
            }
            if(cells[target]!=0)
            {
                Vector2Int step=new Vector2Int(Mathf.RoundToInt(direction.x),Mathf.RoundToInt(direction.y));
                if(step==Vector2Int.zero)step=Mathf.Abs(direction.x)>=Mathf.Abs(direction.y)?new Vector2Int(Math.Sign(direction.x),0):new Vector2Int(0,Math.Sign(direction.y));
                if(energy<1.5f||!PushOne(target,step,Mathf.FloorToInt(energy)))break;
                int pushed=target+step.y*19+step.x;
                int extraPushes=Mathf.Clamp(Mathf.FloorToInt((energy-2f)/2f),0,3);
                for(int i=0;i<extraPushes;i++)
                {
                    int nx=pushed%19+step.x,ny=pushed/19+step.y;if(!Inside(nx,ny))break;
                    int next=ny*19+nx;
                    if(cells[pushed]==0||cells[next]!=0||!Move(pushed,next))break;
                    pushed=next;energy-=.65f;
                }
                energy-=1.5f;
            }
            if(cells[target]!=0||!Move(current,target))break;
            current=target;energy-=.55f;
            if(energy<=.25f)break;
        }
        return current!=start;
    }
    private bool PushOne(int p,Vector2Int step,int energy)
    {
        var planned=new List<int>();var seen=new HashSet<int>();
        bool Plan(int cell,int strength)
        {
            int anchor=StoneAnchor(cell);if(seen.Contains(anchor))return true;
            if(!Mutable(anchor)||cells[anchor]==0)return false;
            int[] footprint=StoneCells(anchor);
            foreach(int source in footprint)
            {
                int x=source%19+step.x,y=source/19+step.y;if(!Inside(x,y))return false;
                int target=y*19+x;if(Array.IndexOf(footprint,target)>=0)continue;
                if(!Mutable(target))return false;
                if(cells[target]!=0&&(strength<2||!Plan(target,strength-2)))return false;
            }
            seen.Add(anchor);planned.Add(anchor);return true;
        }
        // Validate every leading cell before moving any member of the collision chain.
        if(!Plan(p,energy))return false;
        foreach(int source in planned)
            if(!Move(source,(source/19+step.y)*19+source%19+step.x))return false;
        return true;
    }
    public bool Ability(int id,int x,int y,int dx,int dy,int power,Func<int,int,int> random)
    {
        if(winner!=0||premoveSetup!=0||!Inside(x,y)) return false;
        int p=y*19+x, own=turn, enemy=3-turn;
        int rawDx=dx,rawDy=dy;
        dx=Math.Sign(dx);dy=Math.Sign(dy);power=Math.Max(1,Math.Min(id==3?8:id==9?AugmentAim.CatapultMaxPower:id==11?5:3,power));
        if(id==0)
        {
            // Try each candidate at most once, without applying a failed candidate's partial changes.
            var candidates = new List<int>(); for(int i=1;i<=20;i++)if(i!=7)candidates.Add(i);
            while(candidates.Count>0)
            {
                int index=random(0,candidates.Count),candidate=candidates[index];candidates.RemoveAt(index);
                var trial=JsonUtility.FromJson<AugmentBoard>(JsonUtility.ToJson(this));
                if(!trial.Ability(candidate,x,y,dx,dy,power,random))continue;
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(trial),this);return true;
            }
            return false;
        }
        switch(id)
        {
            case 1: // Clear every removable stone in the displayed 3x3 area; never push stones.
                for(int ay=-1;ay<=1;ay++)for(int ax=-1;ax<=1;ax++)
                    if(Inside(x+ax,y+ay))Set((y+ay)*19+x+ax,0);
                break;
            case 2: if(!Mutable(p))return false; Set(p,own); break;
            case 3:
                if(!FlickStone(p,rawDx,rawDy,power))return false;
                break;
            case 9:
                if(dx==0&&dy==0)return false;
                int catapultDistance=AugmentAim.CatapultDistance(power);
                int tx=x+dx*catapultDistance,ty=y+dy*catapultDistance;
                if(!Inside(tx,ty))return false;
                int dest=ty*19+tx;
                if(!Mutable(dest))return false;
                if(cells[dest]!=0) {int nx=tx+dx,ny=ty+dy;if(Inside(nx,ny)&&At(nx,ny)==0)Move(dest,ny*19+nx);else Set(dest,0);}
                Set(dest,own);
                break;
            case 4: capture=own; Capture(own); break;
            case 5:
                if(Premoves(own).Length>0&&premoveIndex[own]<6)return false;
                var recent=new List<int>();
                for(int i=0;i<361;i++)if(cells[i]==own&&StoneAnchor(i)==i)recent.Add(i);
                if(recent.Count<2)return false;
                recent.Sort((a,b)=>order[b].CompareTo(order[a]));
                for(int i=0;i<2;i++)Set(recent[i],0);
                if(own==1)blackPremoves=new int[0];else whitePremoves=new int[0];
                premoveIndex[own]=0;premoveSetup=own;replay=0;break;
            case 6:
                if(!Inside(x+1,y+1))return false;
                for(int ay=0;ay<2;ay++)for(int ax=0;ax<2;ax++)if(At(x+ax,y+ay)!=0)return false;
                for(int ay=0;ay<2;ay++)for(int ax=0;ax<2;ax++)Set((y+ay)*19+x+ax,own);
                largeStone[p]=own;
                break;
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
                lastFlipResult=random(0,2)==0?1:2;
                if(lastFlipResult==1) {for(int i=0;i<361;i++)if(cells[i]==1||cells[i]==2)Set(i,3-cells[i]);}
                else {var owned=new List<int>();for(int i=0;i<361;i++)if(cells[i]==own)owned.Add(i);for(int i=0;i<3&&owned.Count>0;i++){int n=random(0,owned.Count);Set(owned[n],0);owned.RemoveAt(n);}}break;
            default:return false;
        }
        lastAbility=id;CheckWinner();return true;
    }
    private void Sort(List<int> segment)
    {
        var stones=segment.FindAll(p=>cells[p]!=0);
        stones.Sort((a,b)=>order[a].CompareTo(order[b]));
        int[] colors=stones.ConvertAll(p=>cells[p]).ToArray();
        foreach(int p in segment)Set(p,0);
        for(int i=0;i<colors.Length;i++)Set(segment[i],colors[i]);
    }
    public bool CanMovePiece(int x,int y)
    {
        if(winner!=0||premoveSetup!=0||piece<0||pieceOwner!=turn||!Inside(x,y))return false;
        int dx=Math.Abs(x-piece%19),dy=Math.Abs(y-piece/19),q=y*19+x;
        bool valid=pieceKind==0?(dx+dy==1):pieceKind==1?(dx==1&&dy==2||dx==2&&dy==1):(Math.Max(dx,dy)==1);
        return valid&&Mutable(q)&&cells[q]!=turn;
    }
    public bool MovePiece(int x,int y)
    {
        automaticPlacements.Clear();
        blockedPremoves=0;
        if(!CanMovePiece(x,y))return false;
        int q=y*19+x;
        if(cells[q]!=0){Set(q,0);Set(piece,0);piece=-1;}
        else Move(piece,q);
        CheckWinner();FinishTurn();return true;
    }
}
