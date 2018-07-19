using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace AmiBroker.Plugin
{
    class Log
    {
       
        public static void Write(string text, string fileName = "Errors.log")
        {
            if (text == null)
                return;

            FileStream file = new FileStream(fileName, FileMode.OpenOrCreate);
            StreamWriter stream = new StreamWriter(file);

            // запись в конец файла
            file.Seek(0, SeekOrigin.End);
            stream.WriteLine(text);

            stream.Close();
            file.Close();
        }
    }
}
