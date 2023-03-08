using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SaveCsv : MonoBehaviour
{
    [SerializeField] private InputField nameField;
    [SerializeField] private InputField blockNumberField;

    [System.NonSerialized] public StreamWriter sw;
    FileInfo fi;

    public bool MakeCsv()
    {
        string getName = nameField.text;
        string getBlockNum = blockNumberField.text;
        bool writtenName = !getName.Equals(string.Empty);
        bool writtenBlock = !getBlockNum.Equals(string.Empty);
        bool writtenAll = writtenName & writtenBlock;
        //sw = new StreamWriter(string.Format(@"{0}.csv", participantName), true, Encoding.GetEncoding("Shift_JIS"));
        if (writtenAll)
        {
            fi = new FileInfo(Application.dataPath + "/Resources/" + getName + "_" + getBlockNum + ".csv");
            sw = fi.AppendText();
        }


        return writtenAll;
    }

    public void SaveData(params string[] text)
    {
        string s2 = string.Join(",", text);
        sw.WriteLine(s2);
    }
}
