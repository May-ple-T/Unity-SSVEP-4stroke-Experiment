using UnityEngine;
using LSL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

// Don't forget the Namespace import
using Assets.LSL4Unity.Scripts.AbstractInlets;
using System.Collections.Generic;

public class DataReceiverWithFFT : ADoubleInlet
{

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

    private string _currStreamName = "";
    private float _waitTime = 2.0f; // 2 seconds
    private float timer = 0.0f; // timer to query streams
    private List<string> listStreams = new List<string>() { };
    private bool _inletCreated = false;

    public int sampleRate = 256;

    Queue<double> q_O1 = new Queue<double>() { };
    Queue<double> q_O2 = new Queue<double>() { };

    void Start()
    {
        Debug.Log("DataReceiver: Start ");
        Init();

        for (int i = 0; i < 256; i++)
        {
            q_O1.Enqueue(0);
            q_O2.Enqueue(0);
        }
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
                DataHeaderTxt.text = string.Join("; ", GetChannelsList().ToArray());

                // disable dropdown
                DropdownStreams.enabled = false;
                // disable button
                //gameObject.GetComponent<Button>().interactable = false;
                Button button = GameObject.Find("Canvas/btnConnect").GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }

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
            Button button = GameObject.Find("Canvas/btnConnect").GetComponent<Button>();
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }

    /// <summary>
    /// Process data. Show data on UI
    /// </summary>
    protected override void Process(double[] newSample, double timeStamp)
    {
        // Show data on UI
        DataStreamTxt.text = string.Join("; ", newSample);
        //Debug.Log(string.Join("; ", newSample));
        q_O1.Enqueue(newSample[18]);
        q_O2.Enqueue(newSample[21]);
        q_O1.Dequeue();
        q_O2.Dequeue();
        double[] O1_array = q_O1.ToArray();
        double[] O2_array = q_O2.ToArray();

        //Debug.Log(string.Join("; ", O1_array));
        //Debug.Log(string.Join("; ", O2_array));

        var O1 = new Complex[sampleRate];
        var O2 = new Complex[sampleRate];

        for (int i = 0; i < sampleRate; i++)
        {
            O1[i] = new Complex(O1_array[i], 0);
            O2[i] = new Complex(O2_array[i], 0);
        }

        Debug.Log(string.Join("; ", O1));

        // FFTを実行
        Fourier.Forward(O1, FourierOptions.Default);
        Fourier.Forward(O2, FourierOptions.Default);

        Debug.Log(string.Join("; ", O1));
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
