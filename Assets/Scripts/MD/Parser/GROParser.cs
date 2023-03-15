using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;
using File = System.IO.File;

namespace MD.Parser
{
    public static class GROParser
    {
        public static List<Vector3> read(string filename)
        {
            var L = new List<Vector3>();
            
            var reader = new StreamReader(Application.dataPath + filename);
            foreach (string line in reader.ReadToEnd().Split('\n'))
            {
                var elements = line.Split(' ');
                var working = elements.ToList();
                foreach (var val in elements)
                {
                    if (String.IsNullOrWhiteSpace(val)) working.Remove(val);
                }

                elements = working.ToArray();
                if (elements.Length < 5) continue;
                var (posx, posy, posz) = (elements[^3], elements[^2], elements[^1]);
                L.Add(new Vector3(float.Parse(posx), float.Parse(posy), float.Parse(posz)) * 10f);
            }

            return L;
        }
    }
}