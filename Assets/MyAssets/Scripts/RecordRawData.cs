using UnityEngine;
using LSL;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;

// Don't forget the Namespace import
using Assets.LSL4Unity.Scripts.AbstractInlets;
using System.Collections.Generic;

public class RecordRawData : ADoubleInlet
{
    /// <summary>
    /// set active false during runs
    /// </summary>
    public GameObject InformationUI;

    /// <summary>
    /// number of channels
    /// </summary>
    public Text NumChans;

    /// <summary>
    /// unique id of sender
    /// </summary>
    public Text DeviceID;

    /// <summary>
    /// header of data
    /// </summary>
    public Text DataHeaderTxt;

    /// <summary>
    /// data stream
    /// </summary>
    public Text DataStreamTxt;

    /// <summary>
    /// list of lsl streams
    /// </summary>
    public Dropdown DropdownStreams;

    /// <summary>
    /// connect button
    /// </summary>
    public Button ConnectButton;

    /// <summary>
    /// Quit button
    /// </summary>
    public Button QuitButton;

    /// <summary>
    /// load a stimulus video
    /// </summary>
    public VideoPlayer videoPlayer;

    /// <summary>
    /// save csv file
    /// </summary>
    public SaveCsv saveCsv;

    /// <summary>
    /// show target direction
    /// </summary>
    public Text DirectionArrow; 

    /// <summary>
    /// get target data
    /// </summary>
    public TargetSettings tSettings;

    private string _currStreamName = "";
    private float _waitTime = 2.0f; // 2 seconds
    private float timer = 0.0f; // timer to query streams
    private List<string> listStreams = new List<string>() { };
    private bool _inletCreated = false;

    // Related to Experiment
    [System.NonSerialized] public int totalTrials;     // 1ブロック当たりの合計試行回数
    private bool madeCsv = false;   // csvファイルが作成済みか
    private bool onStartClicked = false;    // Startが押されたかの判定
    private bool duringRun = false;     // 試行中かどうか
    private int currentTotalTrials = 0;    // 現在の試行回数
    private int unresetableCTT = 0;     // 合計試行回数を記録
    private int targetId = 0;    // ターゲットのIDを記録
    private float targetHz = 0f;    // ターゲットの周波数を記録

    void Start()
    {
        Debug.Log("DataReceiver: Start ");
        Init();
        videoPlayer.loopPointReached += LoopPointReached;
    }

    void Update()
    {
        if (_inletCreated == false)
        {
            // query streams each 2 seconds if have not connected streams
            timer += Time.deltaTime;
            if (timer > _waitTime)
            {
                timer = timer - _waitTime;
                StartCoroutine(QueryStreams());
            }
        }
        else if(onStartClicked)
        {
            if (Input.GetKeyDown(KeyCode.Space) & totalTrials > currentTotalTrials)
            {
                if (!videoPlayer.isPlaying)
                {
                    targetId = tSettings.GetNextTargetId(currentTotalTrials);
                    targetHz = tSettings.targetHzs[targetId];
                    DirectionArrow.text = tSettings.NextTargetArrow(currentTotalTrials);

                    StartCoroutine(DelayCoroutine(4f, () =>
                    {
                        duringRun = true;
                    }));
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitRun();
            }
        }

        if (duringRun & !videoPlayer.isPlaying) 
        {
            videoPlayer.Play();
        }
    }

    /// <summary>
    /// Delay for starting
    /// </summary>
    public IEnumerator DelayCoroutine(float seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }

    /// <summary>
    /// Query LSL Streams
    /// </summary>
    public IEnumerator QueryStreams()
    {
        DropdownStreams.ClearOptions();
        listStreams.Clear();
        foreach (var stream in QueryAvailStreams())
        {
            string streamName = stream.name();
            if (!listStreams.Contains(streamName))
                listStreams.Add(streamName);
        }
        // add to drop down
        DropdownStreams.AddOptions(listStreams);

        // check the current stream removed from list streams
        if (!string.IsNullOrEmpty(_currStreamName) && !listStreams.Contains(_currStreamName))
        {
            // the current stream is not available
            Debug.Log(" The current stream is not available.");
            _currStreamName = "";
            // TODO: reset button
        }
        yield return null;
    }

    /// <summary>
    /// Handle when choose stream name
    /// </summary>
    public void Dropdown_IndexChanged(int index)
    {
        if (listStreams.Count > 0)
        {
            _currStreamName = listStreams[index];
            foreach (var stream in QueryAvailStreams())
            {
                string name = stream.name();
                if (name == _currStreamName)
                {
                    DeviceID.text = stream.source_id();
                    NumChans.text = stream.channel_count().ToString();
                    return;
                }
            }
        }

    }

    /// <summary>
    /// Connect to a LSL streams
    /// </summary>
    public void OnConnectClick()
    {
        if (listStreams.Count == 0)
        {
            Debug.LogWarning(" No stream available.");
            return;
        }
        // selected streams
        string streamName = listStreams[DropdownStreams.value];
        // create a inlet
        liblsl.StreamInfo info = GetStreamInfo(streamName);
        if (!string.IsNullOrEmpty(info.name()))
        {
            _inletCreated = CreateInlet(info);

            if (_inletCreated)
            {
                Debug.Log("Create an inlet successfully for stream: " + streamName);
                DeviceID.text = info.source_id();
                NumChans.text = info.channel_count().ToString();
                //DataHeaderTxt.text = string.Join("; ", GetChannelsList().ToArray());
                string[] dht = { 
                    GetChannelsList().ToArray()[1],
                    GetChannelsList().ToArray()[3], GetChannelsList().ToArray()[4], GetChannelsList().ToArray()[5], GetChannelsList().ToArray()[6],
                    GetChannelsList().ToArray()[7], GetChannelsList().ToArray()[8], GetChannelsList().ToArray()[9], GetChannelsList().ToArray()[10]
                };
                DataHeaderTxt.text = string.Join("; ", dht);

                // disable dropdown
                DropdownStreams.enabled = false;
                // disable button
                //gameObject.GetComponent<Button>().interactable = false;
                //Button button = GameObject.Find("Canvas/btnConnect").GetComponent<Button>();
                //if (button != null)
                //{
                //    button.interactable = false;
                //}
                ConnectButton.interactable = false;
                QuitButton.interactable = false;
            }

        }
        else
            Debug.Log(" Can not get stream information of stream " + streamName);
    }

    /// <summary>
    /// Handle disconnect button clicked
    /// </summary>
    public void OnDisconnectClick()
    {
        if (_inletCreated)
        {
            CloseInlet();
            _inletCreated = false;
            DataHeaderTxt.text = "";
            DataStreamTxt.text = "";
            _currStreamName = "";
            DropdownStreams.enabled = true;
            //Button button = GameObject.Find("Canvas/btnConnect").GetComponent<Button>();
            //if (button != null)
            //{
            //    button.interactable = true;
            //}
            ConnectButton.interactable = true;
            QuitButton.interactable = true;
        }
    }

    /// <summary>
    /// Start experiment
    /// </summary>
    public void OnStartClick()
    {
        if (_inletCreated)
        {
            bool fillNameField = saveCsv.MakeCsv();
            if (fillNameField)
            {
                onStartClicked = true;
                InformationUI.SetActive(false);
                currentTotalTrials = 0;
            }
        }
    }

    /// <summary>
    /// stop run and display UI
    /// </summary>
    public void ExitRun()
    {
        duringRun = false;
        onStartClicked = false;
        InformationUI.SetActive(true);
        saveCsv.sw.Flush();
        saveCsv.sw.Close();
    }

    /// <summary>
    /// Spaceキーをトリガーにする
    /// </summary>
    public void Recording(string totalNofS, string NumOfSets, string rawData, string tId, string tHz)
    {
        // csvに書き込む処理
        saveCsv.SaveData(totalNofS, NumOfSets, rawData, tId, tHz);
    }

    /// <summary>
    /// Process when movie ends
    /// </summary>
    /// <param name="vp"></param>
    public void LoopPointReached(VideoPlayer vp)
    {
        // 動画再生完了時の処理
        vp.Stop();
        duringRun = false;
        currentTotalTrials++;
        unresetableCTT++;
    }

    /// <summary>
    /// Process data. Show data on UI
    /// </summary>
    protected override void Process(double[] newSample, double timeStamp)
    {
        double[] nsPick = { 
            newSample[1], newSample[3], newSample[4], newSample[5], newSample[6], newSample[7], newSample[8], newSample[9], newSample[10]
        };

        // Show data on UI
        if (!onStartClicked)
        {
            DataStreamTxt.text = string.Join("; ", nsPick);
        }
        else if (duringRun)
        {
            // csvにデータを出力する
            int totalSetNum = unresetableCTT / tSettings.targetHzs.Length;
            int setNum = currentTotalTrials / tSettings.targetHzs.Length;
            Recording(totalSetNum.ToString(), setNum.ToString(), string.Join(",", nsPick), targetId.ToString(), targetHz.ToString());
        }
        
    }

    /// <summary>
    /// Get stream information from stream name
    /// </summary>
    private liblsl.StreamInfo GetStreamInfo(string streamName)
    {
        foreach (var stream in QueryAvailStreams())
        {
            string name = stream.name();
            if (name == streamName)
                return stream;
        }
        return new liblsl.StreamInfo("", "");
    }
}