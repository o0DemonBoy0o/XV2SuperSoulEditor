//created by MugenAttack
//for reading msg files from xenoverse into any program

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace Msgfile
{
    public struct msg
    {
        public int type;
        public msgData[] data;
    }

    public struct msgData
    {
        public string NameID;
        public int ID;
        public string[] Lines;
    }

    struct msgbyte
    {

    }

    static class msgStream
    {
        public static msg Load(byte[] filedata)
        {
            msg file = new msg();
            file.type = BitConverter.ToInt16(filedata,4);
            file.data = new msgData[BitConverter.ToInt32(filedata,8)];

            //read NameID
            int startaddress = BitConverter.ToInt32(filedata, 12);
            int readpoint;
            for (int i = 0; i < file.data.Length; i++)
            {
                readpoint = startaddress + (i * 16);
                int textaddress = BitConverter.ToInt32(filedata, readpoint);
                int charCount = BitConverter.ToInt32(filedata, readpoint + 4);
                if (file.type == 256)
                    file.data[i].NameID = new string(ReadChars(filedata, textaddress, charCount));
                else
                    file.data[i].NameID = new string(ReadChars2(filedata, textaddress, charCount));
            }


            //read ID
            startaddress = BitConverter.ToInt32(filedata, 16);
            for (int i = 0; i < file.data.Length; i++)
                file.data[i].ID = BitConverter.ToInt32(filedata, startaddress + (i * 4));

            //read line/s
            startaddress = BitConverter.ToInt32(filedata, 20);
            int address2,address3;
            for (int i = 0; i < file.data.Length; i++)
            {
                readpoint = startaddress + (i * 8);
                file.data[i].Lines = new string[BitConverter.ToInt32(filedata, readpoint)];
                address2 = BitConverter.ToInt32(filedata, readpoint + 4);
                for (int j = 0; j < file.data[i].Lines.Length; j++)
                {
                    address3 = BitConverter.ToInt32(filedata, address2);
                    int charCount = BitConverter.ToInt32(filedata, address2 + 4);
                    file.data[i].Lines[j] = new string(ReadChars(filedata, address3, charCount));
                }
            }

            for (int i = 0; i < file.data.Length; i++)
            {
                for (int j = 0; j < file.data[i].Lines.Length; j++)
                {
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace(@"&apos;", @"'");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace(@"&quot;", "\"");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace(@" & amp;", @"&");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace("\n", "\r\n");
                }
            }

            return file;
        }

        public static void Save(msg file,string FileName)
        {
            for (int i = 0; i < file.data.Length; i++)
            {
                for (int j = 0; j < file.data[i].Lines.Length; j++)
                {
                    //DEMON: Took me longer than it should have to figure out that order of how it replaces these is important when saving.
                    //Gotta replace all instances of "&" to "&amp;" first else you start messing up the apostrophies and whatnot.
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace(@"&", @"&amp;");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace(@"'", @"&apos;");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace("\"", @"&quot;");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace("\r\n", "\n");
                }
            }

            //MessageBox.Show("setup");
            int byteCount = 0;
            int TopLength = 32;
            int Mid1Length = file.data.Length * 16;
            int Mid2Length = file.data.Length * 4;
            int Mid3Length = file.data.Length * 8;
            int lineCount = 0;

            for (int i = 0; i < file.data.Length; i++)
                lineCount += file.data[i].Lines.Length;

            int Mid4Length = lineCount * 16;

            byteCount = TopLength + Mid1Length + Mid2Length + Mid3Length + Mid4Length;

            byte[] fileData1 = new byte[byteCount];
            List<byte> fileDataText = new List<byte>();
            //int TopStart = 0;
            int Mid1Start = 32;
            int Mid2Start = Mid1Start + Mid1Length;
            int Mid3Start = Mid2Start + Mid2Length;
            int Mid4Start = Mid3Start + Mid3Length;
            int LastStart = Mid4Start + Mid4Length;
            //MessageBox.Show("setup");
            //generate top
            fileData1[0] = 0x23; fileData1[1] = 0x4D; fileData1[2] = 0x53; fileData1[3] = 0x47;
            if (file.type == 256)
            { fileData1[4] = 0x00; fileData1[5] = 0x01; fileData1[6] = 0x01; fileData1[7] = 0x00; }
            else
            { fileData1[4] = 0x00; fileData1[5] = 0x00; fileData1[6] = 0x01; fileData1[7] = 0x00; }

            byte[] pass;
            pass = BitConverter.GetBytes(file.data.Length);
            Applybyte(ref fileData1, pass, 8, 4);
            pass = BitConverter.GetBytes(32);
            Applybyte(ref fileData1, pass, 12, 4);
            pass = BitConverter.GetBytes(Mid2Start);
            Applybyte(ref fileData1, pass, 16, 4);
            pass = BitConverter.GetBytes(Mid3Start);
            Applybyte(ref fileData1, pass, 20, 4);
            pass = BitConverter.GetBytes(file.data.Length);
            Applybyte(ref fileData1, pass, 24, 4);
            pass = BitConverter.GetBytes(Mid4Start);
            Applybyte(ref fileData1, pass, 28, 4);
            //MessageBox.Show("setup 1");
            //generate Mid section 1
            for (int i = 0; i < file.data.Length; i++)
            {
                Applybyte(ref fileData1,
                          GenWordsBytes(file.data[i].NameID,file.type == 256,ref fileDataText,LastStart),
                          Mid1Start + (i * 16),
                          16);
            }
            //MessageBox.Show("setup 2");
            //generate Mid section 2
            for (int i = 0; i < file.data.Length; i++)
            {
                Applybyte(ref fileData1, BitConverter.GetBytes(file.data[i].ID), Mid2Start + (i * 4), 4);
            }
            //MessageBox.Show("setup 3 4");
            //generate Mid section 3 & 4
            int ListCount = 0;
            int address3;
            for (int i = 0; i < file.data.Length; i++)
            {
                address3 = Mid4Start + (ListCount * 16);
                for (int j = 0; j < file.data[i].Lines.Length; j++)
                {
                    Applybyte(ref fileData1,
                          GenWordsBytes(file.data[i].Lines[j], true, ref fileDataText, LastStart),
                          Mid4Start + (ListCount * 16),
                          16);
                    ListCount++;
                }
                Applybyte(ref fileData1, BitConverter.GetBytes(file.data[i].Lines.Length), Mid3Start + (i * 8), 4);
                Applybyte(ref fileData1, BitConverter.GetBytes(address3), Mid3Start + (i * 8) + 4, 4);
            }
            //MessageBox.Show("setup final");
            List<byte> finalize = new List<byte>();
            finalize.AddRange(fileData1);
            finalize.AddRange(fileDataText);

           FileStream fs = new FileStream(FileName, FileMode.Create);
           fs.Write(finalize.ToArray(), 0, finalize.Count);
           for (int i = 0; i < file.data.Length; i++)

            //DEMON: This part exists here so that the too will re-replace the ampersands and apostrophies after saving.
            //Why do it like this? Cause I dunno how else to do it. It just works, ok

           {
               for (int j = 0; j < file.data[i].Lines.Length; j++)
               {
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace(@"&apos;", @"'");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace(@"&quot;", "\"");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace(@" & amp;", @"&");
                    file.data[i].Lines[j] = file.data[i].Lines[j].Replace("\n", "\r\n");
                }
           }
            fs.Close();
        }

        public static char[] ReadChars(byte[] file, int startindex, int Count)
        {
            char[] charArray = new char[Count];
            for (int i = 0; i < Count; i++)
            {
                charArray[i] = BitConverter.ToChar(file, startindex + (i * 2));
            }
            return charArray;
        }

        public static char[] ReadChars2(byte[] file, int startindex, int Count)
        {
            //get and apply bytes needed
            byte[] CharBytes = new byte[Count * 2];
            
            for (int i = 0; i < Count; i++)
            {
                CharBytes[i * 2] = file[startindex + i];
                CharBytes[(i * 2) + 1] = 0x00;
            }

            //convert to chars
            char[] charArray = new char[Count];
            for (int i = 0; i < Count; i++)
            {
                charArray[i] = BitConverter.ToChar(CharBytes,i * 2);
            }
            return charArray;
        }

        public static void Applybyte(ref byte[] file, byte[] data, int pos, int count)
        {
           
            
            
                
                for (int i= 0; i < count; i++)
                    file[pos + i] = data[i];
             
            
         
        }

        public static void Addbyte(ref List<byte> file, byte[] data, int pos, int count)
        {
            for (int i = 0; i < count; i++)
                file.Add(data[i]);
        }

        public static byte[] GenWordsBytes(string Line, bool type256, ref List<byte> text, int bCount)
        {
            
            byte[] charArray;
            if (type256)
            {
                charArray = new byte[(Line.Length + 1) * 2];
                for (int i = 0; i < Line.Length; i++)
                {
                    Applybyte(ref charArray, BitConverter.GetBytes(Line[i]), i * 2, 2);
                }
                charArray[charArray.Length - 2] = 0x00;
                charArray[charArray.Length - 1] = 0x00;
            }
            else
            {
                charArray = new byte[Line.Length + 1];
                for (int i = 0; i < Line.Length; i++)
                {
                    //UNLEASHED: this should be WITHOUT times 2 for non-unicode names.
                    //Applybyte(ref charArray, BitConverter.GetBytes(Line[i]), i * 2, 1);
                    Applybyte(ref charArray, BitConverter.GetBytes(Line[i]), i , 1);
                }
                charArray[charArray.Length - 1] = 0x00;
            }

            byte[] AddressInfo = { 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00 };
            Applybyte(ref AddressInfo, BitConverter.GetBytes(bCount + text.Count), 0, 4); //address of text
            Applybyte(ref AddressInfo, BitConverter.GetBytes(Line.Length), 4, 4);
            Applybyte(ref AddressInfo, BitConverter.GetBytes(charArray.Length), 8, 4);

            text.AddRange(charArray);

            return AddressInfo;
        }
    }
}
