using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CriPakTools;

namespace XV2_Serializer.CPK
{
    /// <summary>
    /// Reads the CRIWARE CPK files for Xenoverse 2 and extracts specified files.
    /// </summary>
    public class CPK_Reader
    {
        public bool ValidCpkDirectory
        { get
            {
                //Returns true if atleast one of the CPKs specified in CPK_LOAD_ORDER exists
                foreach(string s in CPK_LOAD_ORDER)
                {
                    if (File.Exists(String.Format("{0}/{1}", CpkDirectory, s))) return true;
                }
                return false;
            }
        }
        private string CpkDirectory { get; set; }
        private bool RaiseExceptionIfCpkNotFound { get; set; }

        //Load order includes the XV1 Legend Patrol cpk (data_d4_5_xv1.cpk), but I dont know the exact load order for that...
        private string[] CPK_LOAD_ORDER = new string[12] { "data_d4_5_xv1.cpk", "data_d6_dlc.cpk", "data_d0_stv.cpk", "movie_d6_dlc.cpk", "movie_p4.cpk", "movie_p2.cpk", "movie.cpk", "movie0.cpk", "data2.cpk", "data1.cpk", "data0.cpk", "data.cpk" }; 
        private List<CriPakTools.CPK> CpkFiles { get; set; }
        private List<BinaryReader> binaryReader { get; set; }
        
        /// <summary>
        /// Reads the CRIWARE CPK files for Xenoverse 2 and extracts specified files.
        /// </summary>
        /// <param name="cpkDirectory">The directory where the CPK files are located.</param>
        /// <param name="raiseExceptionIfCpkNotFound">If true, then an exception will be raised if a cpk file specified in CPK_LOAD_ORDER is not found. If false, that cpk will be skipped.</param>
        /// <param name="_CPK_LOAD_ORDER">Defines the CPKs read and in what order. Optional: Xenoverse 2 CPKs are set by default.</param>
        public CPK_Reader(string cpkDirectory, bool raiseExceptionIfCpkNotFound, string[] _CPK_LOAD_ORDER = null)
        {
            if (_CPK_LOAD_ORDER != null) CPK_LOAD_ORDER = _CPK_LOAD_ORDER;
            CpkDirectory = cpkDirectory;
            RaiseExceptionIfCpkNotFound = raiseExceptionIfCpkNotFound;
            ValidateCpks();
            CpkFileInit();
        }

        private void ValidateCpks()
        {
            if (RaiseExceptionIfCpkNotFound)
            {
                foreach(string s in CPK_LOAD_ORDER)
                {
                    if (!File.Exists(GetFullCpkPath(s)))
                    {
                        throw new FileNotFoundException(String.Format("The following CPK could not be found: {0}", s));
                    }
                }
            }
        }

        private void CpkFileInit()
        {
            if (ValidCpkDirectory)
            {
                CpkFiles = new List<CriPakTools.CPK>();
                binaryReader = new List<BinaryReader>();

                foreach(string s in CPK_LOAD_ORDER)
                {
                    if (File.Exists(GetFullCpkPath(s)))
                    {
                        var cpk = new CriPakTools.CPK(new Tools());
                        cpk.ReadCPK(GetFullCpkPath(s));
                        CpkFiles.Add(cpk);
                        binaryReader.Add(new BinaryReader(File.OpenRead(GetFullCpkPath(s))));
                    }
                }
            }
        }

        private string GetFullCpkPath(string cpk)
        {
            return String.Format("{0}/{1}", CpkDirectory, cpk);
        }

        /// <summary>
        /// Get the specified file from the CPK files.
        /// </summary>
        /// <param name="fileName">The name of the file, including the path (starting from "data").</param>
        /// 

        //UNLEASHED: commenting this out for not using it
        //public bool GetFile(string fileName, string outputPath)
        //{
        //    if (!ValidCpkDirectory) throw new Exception("Invalid CPK Directory. Cannot retrieve the file.");

        //    for(int i = 0; i < CpkFiles.Count; i++)
        //    { 
        //        if (GetFileFromCpk(CpkFiles[i], binaryReader[i], fileName, outputPath)) return true;
        //    }

        //    return false;
        //}
        //UNLEASHED: commenting this out for not using it
        //private bool GetFileFromCpk(CriPakTools.CPK cpk, BinaryReader binaryReader, string file, string outputPath)
        //{
        //    try
        //    {
        //        foreach (var e in cpk.FileTable)
        //        {
        //            string dir = String.Format("{1}/{0}", (string)e.FileName, (string)e.DirName);
        //            if (dir == file)
        //            {
        //                if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
        //                {
        //                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        //                }

        //                binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);
        //                string isComp = Encoding.ASCII.GetString(binaryReader.ReadBytes(8)); 
        //                binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);

        //                byte[] chunk = binaryReader.ReadBytes(Int32.Parse(e.FileSize.ToString()));
        //                if (isComp == "CRILAYLA")
        //                {
        //                    int size = Int32.Parse((e.ExtractSize ?? e.FileSize).ToString());
        //                    chunk = cpk.DecompressCRILAYLA(chunk, size);
        //                }
        //                File.WriteAllBytes(outputPath, chunk);
        //                return true;
        //            }
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
            
        //}

        /// <summary>
        /// Get the specified file from the CPK files and returns it as a byte array.
        /// </summary>
        /// <param name="fileName">The name of the file, including the path (starting from "data").</param>
        /// UNLEASHED: this was modifed to both extract and return bytes
        public byte[] GetFile(string fileName,  string outputPath)
        {
            if (!ValidCpkDirectory) throw new Exception("Invalid CPK Directory. Cannot retrieve the file.");
            
            for (int i = 0; i < CpkFiles.Count; i++)
            {
                var bytes = GetFileFromCpkAsByteArray(CpkFiles[i], binaryReader[i], fileName, outputPath);

                if (bytes != null) return bytes;
            }

            return null;
        }
        // UNLEASHED: this was modifed to both extract and return bytes
        private byte[] GetFileFromCpkAsByteArray(CriPakTools.CPK cpk, BinaryReader binaryReader, string file, string outputPath)
        {
            try
            {
                foreach (var e in cpk.FileTable)
                {
                    string dir = String.Format("{1}/{0}", (string)e.FileName, (string)e.DirName);
                    if (dir == file)
                    {
                        binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);
                        string isComp = Encoding.ASCII.GetString(binaryReader.ReadBytes(8));
                        binaryReader.BaseStream.Seek((long)e.FileOffset, SeekOrigin.Begin);

                        byte[] chunk = binaryReader.ReadBytes(Int32.Parse(e.FileSize.ToString()));
                        if (isComp == "CRILAYLA")
                        {
                            int size = Int32.Parse((e.ExtractSize ?? e.FileSize).ToString());
                            chunk = cpk.DecompressCRILAYLA(chunk, size);
                        }

                    //Create directory will automatically check if directory exists
                        Directory.CreateDirectory(outputPath + @"/" + e.DirName.ToString());
                        File.WriteAllBytes(string.Format("{0}/{1}/{2}", outputPath, e.DirName.ToString(), e.FileName.ToString()), chunk);
                        return chunk;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }

        }


    }
}
