﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Reflection;
using System.IO;
using Table;

public class GameManager : Manager
{
    public bool IsGameStart = false;
    //---------------------------------------------------------
    bool created_ = false;
    
    //---------------------------------------------------------
    /// <summary>
    /// 热更完成，启动游戏
    /// </summary>

    void Update()
    {
        if (!IsGameStart)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGameConfirm();
        }
            
        // 日志中心
        //LogCenter.Instance().Update();
        LoadSceneManager.Instance().Update();
        
    }

    void FixedUpdate ()
    {
        if (Time.frameCount % 5 == 0)
            ObjectPoolManager.Tick();
    }
    //------------------------------------------
    public void StartGame()
    {
        if (IsGameStart)
        {
            return;
        }

        if (!IsCreated)
        {
            Debug.Log("初始化GameManager");
            Create();
        }

        IsGameStart = true;
        CanSyncMsg = true;
    }
    public bool CanSyncMsg { get; set; }

    private float gameSpeed = 1;
    public float GameSpeed
    {
        get {
            return gameSpeed;
        }
        set
        {
            if(value < 0)
            {
                value = 0;
            }
            if(value > 10)
            {
                value = 10;
            }
            gameSpeed = value;
           // Time.timeScale = gameSpeed;
        }
    }

    bool ExitGameConfirm()
    {
        Application.Quit();
        return true;
    }
    
    // 创建
    void Create()
    {
        if (IsCreated)
        {
            return;
        }
        IsCreated = true;

    }
    
    public bool IsCreated
    {
        get { return created_; }
        set { created_ = value; }
    }
    // 销毁
    public void Destroy()
    {
        //network_.Close();
    }


    public void LoadinScene(string ScnenName)
    {
        string SceneProperty = ScnenName + "TableInfo";
        SceneTableConfig sTConfig = (SceneTableConfig)typeof(TableData).GetProperty(SceneProperty).GetValue(null, null);
        PLGround.Instance.transform.localScale = new Vector3(sTConfig.Col / 10f, 1f, sTConfig.Row / 10f);
        PLGround.Instance.Init(sTConfig.Col, sTConfig.Row);
        PLGround.Instance.AddCellsGround(sTConfig.m_kMapDatas);
    }
}
