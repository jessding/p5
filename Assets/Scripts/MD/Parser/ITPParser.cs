using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Application = UnityEngine.Application;
using RET = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>>;


public static class ITPParser 
{
    enum LineType
    {
        SECTION, HEADER, VALUES
    }

    public static RET read(string filename)
    {
        RET itp = new();

        bool start = false;
        Dictionary<string, List<string>> prevSection = null;
        List<string> prevColumns = null;
        
        //Debug.Log(Application.dataPath + filename);
        var reader = new StreamReader(Application.dataPath + filename);
        foreach (string line in reader.ReadToEnd().Split('\n'))
        {
            if (!start && line.Contains("[mol")) start = true;
            if (!start) continue;

            if (parseLine(line) is var parsedLine && parsedLine is null) continue;
            var (type, values) = parsedLine.Value;
            if (values.Length < 1) continue;
            
            var working = values.ToList();
            foreach (var val in values)
            {
                if (String.IsNullOrWhiteSpace(val)) working.Remove(val);
            }

            values = working.ToArray();

            switch (type)
            {
                case LineType.SECTION: 
                    itp[values[0]] = new();
                    prevSection = itp[values[0]];
                    break;
                case LineType.HEADER:
                    foreach (var val in values) prevSection[val] = new();
                    prevColumns = values.ToList();
                    break;
                case LineType.VALUES:
                    for (var i = 0; i < prevColumns.Count; i++)
                    {
                        if (!prevSection.ContainsKey(prevColumns[i])) prevSection[prevColumns[i]] = new();
                        var work = prevSection[prevColumns[i]];
                        work.Add(values[i]);
                    }
                    break;
            }
        }

        // List<String> itpKeys = itp.Keys.ToList();

        // for (int i = 0; i < itp.Keys.Count; i++) {
        //     if (itpKeys[i] == "atoms") {
        //         List<String> atomKeys = itp["atoms"].Keys.ToList();
        //         for (int j = 0; j < itp["atoms"].Keys.Count; j++) {
        //             Debug.Log(atomKeys[j]);
        //             Debug.Log(itp["atoms"][atomKeys[j]]);
        //         }
        //     }
        // }
        return itp;
    }

    /// <summary>
    /// Parse line and returns its type and its values in string.
    /// Sections provide section name as its only element
    /// Headers provide the column names as its elements
    /// Values provide strings of the values
    /// </summary>
    /// <param name="line">input line from file</param>
    /// <returns>returns tuple of LineType and string[], if not valid, returns null</returns>
    private static (LineType, string[])? parseLine(string line)
    {
        var splitLine = line.Split(new char[] { ' ', '\t' }).ToList();
        var working = splitLine.ToList();
        foreach (var val in splitLine)
        {
            if (String.IsNullOrWhiteSpace(val)) working.Remove(val);
        }
        if (working.Count < 1) return null;
        var initial = working[0][0];
        switch (initial)
        {
            case ';':
                return (LineType.HEADER, line.Split(new char[] { ' ', ';' }));
            case '[': 
                return (LineType.SECTION, line.Split(new char[] {'[',']'}));
            default:
                return (LineType.VALUES, line.Split(new char[] { ' ', ';' }));
        }
    }
}
