﻿/********************************************************************************
** auth： yanwei
** date： 2017-07-08
** desc： 一个经典的A*算法。
*********************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarTile
{
    public int x;			
    public int y;			
    public int type;		// 是否有障碍 1表示有
    public AStarTile parent;		// 前一个路径点的网格
    public int open;		// 
    public int g;			// 权重值
    public int h;			// 权重值
    public int f;			// 权重值

    public AStarTile(int posx, int posy)
    {
        x = posx;
        y = posy;
        Reset(true);
    }

    public void Reset(bool SetBlank)
    {
        parent = null;
        open = -1;
        g = 0;
        h = -1;
        f = 0;

        if (SetBlank)
            SetType(0);
    }

    public void SetType(int _type)
    {
        type = _type;
    }
}

public class PLAStar : MonoBehaviour {
    private int width = 0;			
    private int height = 0;			
    private float tilesize = 1.0f;	
    public AStarTile[,] tiles = null;		
    private int[] dirX = null;		
    private int[] dirY = null;		
    private int[] Cost = null;		

    private AStarTile tileStart = null;		
    private AStarTile tileEnd = null;			
    private AStarTile tileInCheck = null;
    private List<AStarTile> tileExcept = new List<AStarTile>();	
    public List<Vector3> Path = new List<Vector3>();

    private bool PathFindSuccess = false;
    private bool PathFindCompleted = false;
    private int PathCount = 0;
    private int CheckStep = 0;

    public void Init(int w, int h, float _tilesize)
    {

        // 设置耗费值和网格方位
        dirX = new int[8] { -1, 0, 1, 1, 1, 0, -1, -1 };
        dirY = new int[8] { -1, -1, -1, 0, 1, 1, 1, 0 };
        Cost = new int[8] { 14, 10, 14, 10, 14, 10, 14, 10 };

        width = w;
        height = h;
        tilesize = _tilesize;

        tiles = new AStarTile[w, h];
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                tiles[x, y] = new AStarTile(x, y);
            }
        }
    }

    public AStarTile GetTile(int type)
    {
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (tiles[x, y].type == type)
                {
                    return tiles[x, y];
                }
            }
        }

        return null;
    }

    public AStarTile GetTile(Vector3 vPos)
    {
        int x = (int)((vPos.x + (float)(width - 1) * 0.5f * tilesize) / tilesize);
        int z = (int)((vPos.z + (float)(height - 1) * 0.5f * tilesize) / tilesize);
        x = Mathf.Clamp(x, 0, width - 1);
        z = Mathf.Clamp(z, 0, height - 1);
        return tiles[x, z];
    }

    public Vector3 GetTilePos(int x, int z)
    {
        return new Vector3(((float)x - (float)(width - 1) * 0.5f) * tilesize, 0, ((float)z - (float)(height - 1) * 0.5f) * tilesize);
    }

    public void Reset(bool SetBlank)
    {
        PathFindCompleted = false;
        if (SetBlank)
        {
            tileStart = null;
            tileEnd = null;
            tileInCheck = null;
        }

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                tiles[x, y].Reset(SetBlank);
            }
        }
    }

    public AStarTile FindEmpty(int ExceptX, int ExceptY)
    {
        int iCount = 0;
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                if ((tiles[x, y].type == 0) && (x != ExceptX) && (y != ExceptY))
                    iCount++;
            }
        }

        if (iCount == 0) return null;

        int iOffset = UnityEngine.Random.Range(0, iCount);
        iCount = 0;
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                if ((tiles[x, y].type == 0) && (x != ExceptX) && (y != ExceptY))
                {
                    if (iOffset == iCount) return tiles[x, y];
                    iCount++;
                }
            }
        }

        return null;
    }

    public void SetTileMode(int x, int y, int type)
    {
        AStarTile tile = tiles[x, y];

        //if((type == AStarTileType.Start) && (tileStart != null)) { tileStart.SetType(0); tileStart = null; }
        //if((type == AStarTileType.End  ) && (tileEnd   != null)) { tileEnd.SetType(0);   tileEnd = null; }

        tile.SetType(type);

        //if(type == AStarTileType.Start)	tileStart = tile;
        //if(type == AStarTileType.End  ) 	tileEnd = tile;
    }

    private int ManhattanDistance(AStarTile start, AStarTile end)
    {
        return (Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y)) * 10;
    }

    private int ChebyshevDistance(AStarTile start, AStarTile end)
    {
        return Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y)) * 10;
    }

    private void Check(AStarTile parent)
    {
        for (int i = 0; i < 8; ++i)
        {

            int newX = parent.x + dirX[i];
            int newY = parent.y + dirY[i];
            if ((newX < 0) || (width <= newX) || (newY < 0) || (height <= newY)) continue;

            AStarTile tile = tiles[newX, newY];

            if ((tile != tileEnd) && ((tile.open == 0) || (tile.type != 0))) continue;

            if (tileExcept != null)
            {
                bool bExcept = false;
                for (int j = 0; j < tileExcept.Count; ++j)
                {
                    if (tileExcept[j] == tile)
                    {
                        bExcept = true;
                        break;
                    }
                }

                if (bExcept) continue;
            }
            //if((newY-1 >= 0) && (tiles[newX,newY-1].eType != AStarTileType.Blank)) continue;

            // set tiles value
            if (tile.open == -1)
            {
                tile.g = parent.g + Cost[i];
                if (tile.h == -1) tile.h = ManhattanDistance(tile, tileEnd);
                tile.f = tile.g + tile.h;
                tile.open = 1;
                tile.parent = parent;
            }
            else
            { // (tile.open == 1)
                int gNew = parent.g + Cost[i];
                if (gNew < tile.g)
                {
                    tile.g = gNew;
                    tile.f = tile.g + tile.h;
                    tile.parent = parent;
                }
            }
        }
    }

    public bool PathFind(AStarTile start, AStarTile end, List<AStarTile> except)
    {
        Reset(false);

        Path.Clear();
        PathFindSuccess = false;
        PathFindCompleted = false;
        PathCount = 0;
        CheckStep = 0;
        tileStart = start;
        tileEnd = end;

        tileInCheck = tileStart;
        tileInCheck.open = 0;
        tileInCheck.g = 0;

        if (except == null) tileExcept = new List<AStarTile>();
        else tileExcept = except;

        PathCount++;

        while (!PathFindCompleted)
        {
            PathFindSub();
        }

        return PathFindSuccess;
    }

    public void PathFindSub()
    {
        CheckStep++;
        Check(tileInCheck);

        AStarTile tileMinF = null;
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (tiles[x, y].open != 1) continue;

                if ((tileMinF == null) || (tiles[x, y].f < tileMinF.f))
                    tileMinF = tiles[x, y];
            }
        }

        if (tileMinF == null)
        {
            PathFindCompleted = true;
            return;
        }

        tileInCheck = tileMinF;
        tileInCheck.open = 0;
        //Debug.Log ("A Star "+PathCount.ToString ()+" - "+tileInCheck.x.ToString()+","+tileInCheck.y.ToString()+",g:"+tileInCheck.g.ToString()+",h:"+tileInCheck.h.ToString()+",f:"+tileInCheck.f.ToString());
        PathCount++;

        if (tileInCheck == tileEnd)
        {
            PathFindCompleted = true;
            PathFindSuccess = true;

            AStarTile tileResult = tileEnd;
            while (tileResult != null)
            {
                Path.Insert(0, GetTilePos(tileResult.x, tileResult.y));
                tileResult = tileResult.parent;
            }
        }
    }
	
}
