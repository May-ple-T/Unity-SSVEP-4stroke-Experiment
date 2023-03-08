using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSettings : MonoBehaviour
{
    /// <summary>
    /// 合計試行回数を書き込む
    /// </summary>
    [SerializeField] private RecordRawData rrd;

    /// <summary>
    /// ターゲットの周波数を設定
    /// </summary>
    public float[] targetHzs = { 15f, 12f, 10f, 8.57f };

    /// <summary>
    /// ターゲットのID（上記のリストのインデックス）を保存
    /// </summary>
    [System.NonSerialized] public List<int> targetIds = new List<int>();

    /// <summary>
    /// 1ブロック当たりのセット数
    /// </summary>
    [SerializeField] private int setsPerBlock = 10;

    // Start is called before the first frame update
    void Start()
    {
        //var loopCount = targetHzs.Length * eachNum;
        //rrd.numberOfRun = loopCount;                      numberOfRun -> totalTrials
        //for (int i = 0; i < loopCount; i++)
        //{
        //    targetIds.Add(i % targetHzs.Length);
        //}
        //targetIds = targetIds.OrderBy(a => Guid.NewGuid()).ToList();

        rrd.totalTrials = targetHzs.Length * setsPerBlock;

        //  4回ごとにランダムでターゲットを生成
        List<int> targetIdsOfSet = new List<int>();
        for (int i = 0; i < setsPerBlock; i++)
        {
            targetIdsOfSet.Clear();
            for (int j = 0; j < targetHzs.Length; j++) targetIdsOfSet.Add(j);
            targetIdsOfSet = targetIdsOfSet.OrderBy(a => Guid.NewGuid()).ToList();
            targetIds.AddRange(targetIdsOfSet);
        }
    }

    /// <summary>
    /// Called when the next target is set
    /// </summary>
    /// <param name="index">
    /// 次の試行回数を代入
    /// </param>
    public string NextTargetArrow(int index)
    {
        int id = targetIds[index];
        switch (id)
        {
            case 0:
                return "←";
            case 1:
                return "↑";
            case 2:
                return "→";
            case 3:
                return "↓";
            default:
                return "×";
        }
    }

    public int GetNextTargetId(int index)
    {
        int id = targetIds[index];
        return id;
    }
}
