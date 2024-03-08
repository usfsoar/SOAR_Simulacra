using System;
using System.Collections.Generic;
using System.IO;

public class CSVDataWriter
{
    private string csvFileName;
    private List<string> headers;
    private string label;

    public CSVDataWriter(string label, params string[] headers)
    {
        this.headers = new List<string>(headers);
        this.label = label;
        CreateCSVFile();
    }

    private void CreateCSVFile()
    {
        string dateTimeStr = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
        csvFileName = "Assets/"+label+"_" + dateTimeStr + ".csv";

        using (StreamWriter sw = new StreamWriter(csvFileName))
        {
            if (headers.Count > 0)
            {
                string headerStr = string.Join(",", headers);
                sw.WriteLine(headerStr);
            }

            // Add any additional setup logic here
        }
    }

    public void WriteData(List<float> dataList)
    {
        string dataStr = string.Join(",", dataList);

        using (StreamWriter sw = new StreamWriter(csvFileName, true))
        {
            sw.WriteLine(dataStr);
            sw.Flush();
        }
    }
    public void WriteData(List<int> dataList)
    {
        string dataStr = string.Join(",", dataList);

        using (StreamWriter sw = new StreamWriter(csvFileName, true))
        {
            sw.WriteLine(dataStr);
            sw.Flush();
        }
    }
    public void WriteData(List<string> dataList)
    {
        string dataStr = string.Join(",", dataList);

        using (StreamWriter sw = new StreamWriter(csvFileName, true))
        {
            sw.WriteLine(dataStr);
            sw.Flush();
        }
    }
}
