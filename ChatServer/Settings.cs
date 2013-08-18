﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ChatServer
{
    partial class Program
    {
        public static Dictionary<string, dynamic> Settings { get; set; }

        public static Dictionary<string, dynamic> LoadSettings()
        {
            var temp = new Dictionary<string, dynamic>(25);
            using (var reader = new StreamReader("server.ini"))
            {
                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if ((line = line.Trim()) == string.Empty || line.StartsWith("##") || line.StartsWith("'")) continue;
                    var data = line.Split('=');
                    if (data[1].StartsWith("^pair("))
                        temp.Add(data[0], ParsePair(data[1]));
                    else if (data[1].StartsWith("^tuple("))
                        temp.Add(data[0], ParseTuple(data[1]));
                    else
                        temp.Add(data[0], ParseValue(data[1]));
                }
            }

            return temp;
        }

        static dynamic ParseValue(string value)
        {
            dynamic item;
            bool _bool;
            int _int;
            if (!int.TryParse(value, out _int))
                if (!bool.TryParse(value, out _bool))
                    item = value;
                else item = _bool;
            else item = _int;
            return item;
        }

        static dynamic ParsePair(string expression)
        {
            dynamic item1, item2;
            bool _bool1, _bool2;
            int _int1, _int2;
            var split = expression.Replace("^pair(", "").Replace(")", "").Split(',');
            if (!int.TryParse(split[0], out _int1))
                if (!bool.TryParse(split[0], out _bool1))
                    item1 = split[0];
                else item1 = _bool1;
            else item1 = _int1;

            if (!int.TryParse(split[1], out _int2))
                if (!bool.TryParse(split[1], out _bool2))
                    item2 = split[1];
                else item2 = _bool2;
            else item2 = _int2;

            return Tuple.Create(item1, item2);
        }

        static dynamic ParseTuple(string expression)
        {
            dynamic item1, item2, item3;
            bool b1, b2, b3;
            int i1, i2, i3;
            var split = expression.Replace("^tuple(", "").Replace(")", "").Split(',');

            if (!int.TryParse(split[0], out i1))
                if (!bool.TryParse(split[0], out b1))
                    item1 = split[0];
                else item1 = b1;
            else item1 = i1;

            if (!int.TryParse(split[1], out i2))
                if (!bool.TryParse(split[1], out b2))
                    item2 = split[1];
                else item2 = b2;
            else item2 = i2;

            if (!int.TryParse(split[2], out i3))
                if (!bool.TryParse(split[2], out b3))
                    item3 = split[2];
                else item3 = b3;
            else item3 = i3;

            return Tuple.Create(item1, item2, item3);
        }
    }
}
