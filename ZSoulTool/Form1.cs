using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using Msgfile;
using XV2_Serializer.Resource;

namespace XV2SSEdit
{
    public partial class Form1 : Form
    {
        #region Data_types
        string[] ListNames = {"Max Health","Max Ki","Ki Restoration Rate","Max Stamina",
            "Stamina Resoration Rate","Enemy Stamina Damage","User Stamina Damage ","Ground Speed","Air Speed","Boost Speed","Dash Speed",
            "Basic Melee Damage","Basic Ki Blast Damage","Strike Skill Damage","Ki Skill Damage","Basic Melee Damage Taken",
            "Basic Ki Blast Damage Taken","Strike Skill Damage Taken","Ki Skill Damage Taken","Transform Skill Duration",
            "Reinforcement Skill Duration","Unknown 1","User Revival HP Restored","Revival Speed on User","Revival Speed on Ally","Unknown 2",
            "Assist Effect 1","Assist Effect 2","Assist Effect 3","Assist Effect 4","Assist Effect 5","Assist Effect 6"};

        string FileName;
        EffectList eList;
        ActivationList aList;
        TargetList tList;
        ColorList cList;
        KitypeList kList;
        CheckboxList bList;
        string FileNameMsgN;
        string FileNameMsgD;
        string FileNameMsgB;
        string FileNameMsgB_BTLHUD;
        string FileNameMsgB_Pause;
        bool NamesLoaded = false;
        bool DescsLoaded = false;
        bool BurstLoaded = false;
        bool lockMod = false;
        //int copy;

        //UNLEASHED: made this public to help with export.
        public idbItem[] Items;

        private msg Names;
        private msg Descs;
        private msg Burst;
        private msg BurstBTLHUD;
        private msg BurstPause;

        private List<GenericMsgFile> genericMsgListNames = new List<GenericMsgFile>();
        private List<GenericMsgFile> genericMsgLisDescs = new List<GenericMsgFile>();
        private List<GenericMsgFile> genericMsgListBurst = new List<GenericMsgFile>();
        private List<GenericMsgFile> genericMsgListNameBurstBTLHUD = new List<GenericMsgFile>();
        private List<GenericMsgFile> genericMsgListNameBurstPause = new List<GenericMsgFile>();

        private struct GenericMsgFile
        {
            public string msgPath_m;
            public msg msgFile_m;
            public GenericMsgFile(string msgPath, msg msgFile)
            {
                msgPath_m = msgPath;
                msgFile_m = msgFile;
            }
        }

        private ToolSettings settings { get; set; }
        private Xv2FileIO fileIO { get; set; }
        //private bool Initialized = false;
        //UNLEASHED: helper vars for searching
        private int lastFoundIndex = 0;
        private string lastInputString = "";
        //UNLEASHED: helper var for deleting
        private int currentSuperSoulIndex = -1;
        //UNLEASHED remind user to save changes.
        private bool hasSavedChanges = true;
        //UNLEASHED copy and paste
        private byte[] clipboardData = null;

        #endregion

        public Form1()
        {
            InitializeComponent();

            foreach (string str in ListNames)
            {
                var Item = new ListViewItem(new[] { str, "0.0" });
                var Item1 = new ListViewItem(new[] { str, "0.0" });
                var Item2 = new ListViewItem(new[] { str, "0.0" });
                lstvBasic.Items.Add(Item);
                lstvEffect1.Items.Add(Item1);
                lstvEffect2.Items.Add(Item2);
            }
        }

        private void installSSPFromArgs(string[] sspArgsPath)
        {
          
                if (Path.GetExtension(sspArgsPath[1].ToLower()) == ".ssp")
                {
                    if (!importSSP(sspArgsPath[1]))
                    {
                        MessageBox.Show("Error occurred when installing SSP file.");
                        Application.Exit();

                    }
                  

                        itemList.Items.Clear();
                        for (int i = 0; i < Items.Length; i++)
                        {
                            if (NamesLoaded)
                                itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() +  " - " + Names.data[Items[i].msgIndexName].Lines[0]);
                            else
                                itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString());
                        }
    
                        SaveXV2SSEdit();
                        MessageBox.Show("SSP imported successfully");
                        Application.Exit();
                    

                }
            
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            settings = ToolSettings.Load();
            InitDirectory();
            InitFileIO();
            LoadFiles();

            string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                    installSSPFromArgs(args);
 
        }

        private void InitDirectory()
        {
            if (!settings.IsValidGameDir)
            {
                using(Forms.Settings settingsForm = new Forms.Settings(settings))
                {
                    settingsForm.ShowDialog();

                    if (settingsForm.Finished)
                    {
                        settings = settingsForm.settings;
                        settings.Save();
                    }
                    else
                    {
                        MessageBox.Show(this, "The setttings were not set. The application will now close.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        Environment.Exit(0);
                    }
                }
            }
        }
        
        private void InitFileIO()
        {
            if (settings.IsValidGameDir)
            {
            
                fileIO = new Xv2FileIO(settings.GameDir, false, new string[2] { "data1.cpk", "data2.cpk" });
            }
            else
            {
                throw new Exception("Cannot init Xv2FileIO because the set game directory is invalid.");
            }
        }

        private void LoadFiles()
        {
            byte[] idbfile = new byte[1];
            eList = new EffectList();
            aList = new ActivationList();
            tList = new TargetList();
            cList = new ColorList();
            kList = new KitypeList();
            bList = new CheckboxList();

            //720
            //load talisman
            int count = 0;
            FileName = String.Format("{0}/data/system/item/talisman_item.idb", settings.GameDir);
            idbfile = fileIO.GetFileFromGame("system/item/talisman_item.idb");
            count = BitConverter.ToInt32(idbfile, 8);

            //UNLEASHED: Load the generic Msg files, and ignore current language suffix
            
            List<string> langsSuffix = ToolSettings.LanguageSuffix.ToList<string>();

            //UNLEASHED: loop through and find our current language's index
            for (int i = 0; i < ToolSettings.LanguageSuffix.Length; i++)
            {
                if (ToolSettings.LanguageSuffix[i] == settings.LanguagePrefix)
                {
                    //UNLEASHED: remove the language suffix that is used for the current activate language
                    langsSuffix.RemoveAt(i);
                    break;
                }
            }
                for (int i = 0; i < langsSuffix.Count; i++)
                {

                    genericMsgListNames.Add(new GenericMsgFile(String.Format("{0}/data/msg/proper_noun_talisman_name_{1}.msg", settings.GameDir, langsSuffix[i]),
                        msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_name_{0}.msg", langsSuffix[i])))));

                    genericMsgLisDescs.Add(new GenericMsgFile(String.Format("{0}/data/msg/proper_noun_talisman_info_{1}.msg", settings.GameDir, langsSuffix[i]),
                       msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_info_{0}.msg", langsSuffix[i])))));

                    genericMsgListBurst.Add(new GenericMsgFile(String.Format("{0}/data/msg/proper_noun_talisman_info_olt_{1}.msg", settings.GameDir, langsSuffix[i]),
                     msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_info_olt_{0}.msg", langsSuffix[i])))));

                    genericMsgListNameBurstBTLHUD.Add(new GenericMsgFile(String.Format("{0}/data/msg/quest_btlhud_{1}.msg", settings.GameDir, langsSuffix[i]),
                     msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/quest_btlhud_{0}.msg", langsSuffix[i])))));

                    genericMsgListNameBurstPause.Add(new GenericMsgFile(String.Format("{0}/data/msg/pause_text_{1}.msg", settings.GameDir, langsSuffix[i]),
                     msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/pause_text_{0}.msg", langsSuffix[i])))));

                }

                //UNLEASHED: why are we checking for checkboxes? is this a debug option?
                if (chkMsgName.Checked)
                {
                    NamesLoaded = true;
                    FileNameMsgN = String.Format("{0}/data/msg/proper_noun_talisman_name_{1}.msg", settings.GameDir, settings.LanguagePrefix);
                    Names = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_name_{0}.msg", settings.LanguagePrefix)));
                }

            if (chkMsgDesc.Checked)
            {
                //load msgfile for descriptions
                DescsLoaded = true;
                FileNameMsgD = String.Format("{0}/data/msg/proper_noun_talisman_info_{1}.msg", settings.GameDir, settings.LanguagePrefix);
                Descs = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_info_{0}.msg", settings.LanguagePrefix)));

            }

            if (chkMsgBurst.Checked)
            {
                //load msgfile for limit burst descriptions
                BurstLoaded = true;
                FileNameMsgB = String.Format("{0}/data/msg/proper_noun_talisman_info_olt_{1}.msg", settings.GameDir, settings.LanguagePrefix);
                FileNameMsgB_BTLHUD = String.Format("{0}/data/msg/quest_btlhud_{1}.msg", settings.GameDir, settings.LanguagePrefix);
                FileNameMsgB_Pause = String.Format("{0}/data/msg/pause_text_{1}.msg", settings.GameDir, settings.LanguagePrefix);
                Burst = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_info_olt_{0}.msg", settings.LanguagePrefix)));
                BurstBTLHUD = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/quest_btlhud_{0}.msg", settings.LanguagePrefix)));
                BurstPause = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/pause_text_{0}.msg", settings.LanguagePrefix)));
                //UNLEASHED: btlhud/Pause is a shared MSG for various battle text
            }

            //idbItems set
            this.addNewXV2SSEditStripMenuItem.Enabled = true;
           this.msgToolStripMenuItem.Enabled = true;

            Items = new idbItem[count];
            for (int i = 0; i < Items.Length; i++)
            {
                Items[i].Data = new byte[720];
                Array.Copy(idbfile, 16 + (i * 720), Items[i].Data, 0, 720);
                if (NamesLoaded)
                    Items[i].msgIndexName = FindmsgIndex(ref Names, BitConverter.ToUInt16(Items[i].Data, 4));
                if (DescsLoaded)
                    Items[i].msgIndexDesc = FindmsgIndex(ref Descs, BitConverter.ToUInt16(Items[i].Data, 6));
                if (BurstLoaded)
                {
                    Items[i].msgIndexBurst = FindmsgIndex(ref Burst, BitConverter.ToUInt16(Items[i].Data, 40));
                    Items[i].msgIndexBurstBTL = getLB_BTL_Pause_DescID(BurstBTLHUD, Burst.data[Items[i].msgIndexBurst].NameID);
                    Items[i].msgIndexBurstPause = getLB_BTL_Pause_DescID(BurstPause, Burst.data[Items[i].msgIndexBurst].NameID);
                    //UNLEASHED: Add BTL / PAUSE LB Desc
                }
                   
            }

            itemList.Items.Clear();

            for (int i = 0; i < count; i++)
            {
                //DEMON: decided to remove the hex ID from view. Almost no use for it aside from debugging I guess.
                //itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " / " + String.Format("{0:X}", BitConverter.ToUInt16(Items[i].Data, 0)) + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
                if (NamesLoaded)
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
                else
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " / " + String.Format("{0:X}", BitConverter.ToUInt16(Items[i].Data, 0)));
            }
            EffectData();
            itemList.SelectedIndex = 0;
        }

        //UNLEASHED: added this function to retrieve LB_BTL / LB_PAUSE desc, as the ID for it doesn't exist in SS data 


        private int getLB_BTL_Pause_DescID(msg extraBurstMsgFile ,string BurstEntryName)
        {
            int BurstID = -1;
            //cut the string and only get the ID, then convert to int
            if (!Int32.TryParse(BurstEntryName.Substring(13), out BurstID))
            {
                //something went wrong...
                return -1;
            }

            string BurstBTLHUDName = "BHD_OLT_000_";
            if (BurstID < 100)
                BurstBTLHUDName += BurstID.ToString("00");
            else
                BurstBTLHUDName += BurstID.ToString(); //hopefully we can have IDs above 99 that don't need that 000 prefix

            for (int i = 0; i < extraBurstMsgFile.data.Length; i++)
            {

                if (extraBurstMsgFile.data[i].NameID == BurstBTLHUDName)
                    return i;


            }

            return -1;
        }

        //UNLEASHED: function to write empty Msg text for generic Msg files (for syncing purposes)
        void writeToMsgText(int MsgType)
        {
 
        

            switch (MsgType)
            {
                case 0:
                    {
                        for (int i = 0; i < genericMsgListNames.Count; i++)
                        {
                            GenericMsgFile tmp = genericMsgListNames[i];
                            msgData[] Expand2 = new msgData[tmp.msgFile_m.data.Length + 1];
                            Array.Copy(tmp.msgFile_m.data, Expand2, tmp.msgFile_m.data.Length);
                            Expand2[Expand2.Length - 1].NameID = "talisman_" + tmp.msgFile_m.data.Length.ToString("000");
                            Expand2[Expand2.Length - 1].ID = tmp.msgFile_m.data.Length;
                            Expand2[Expand2.Length - 1].Lines = new string[] { "" }; //UNLEASHED: hopefully an empty string is enough
                            tmp.msgFile_m.data = Expand2;
                            genericMsgListNames[i]= tmp;
     
                        }
                            break;
                    }

                case 1:
                    {

                        for (int i = 0; i < genericMsgLisDescs.Count; i++)
                        {
                            GenericMsgFile tmp = genericMsgLisDescs[i];
                            msgData[] Expand = new msgData[tmp.msgFile_m.data.Length + 1];
                            Array.Copy(tmp.msgFile_m.data, Expand, tmp.msgFile_m.data.Length);
                            Expand[Expand.Length - 1].NameID = "talisman_eff_" + tmp.msgFile_m.data.Length.ToString("000");
                            Expand[Expand.Length - 1].ID = tmp.msgFile_m.data.Length;
                            Expand[Expand.Length - 1].Lines = new string[] { "" };
                            tmp.msgFile_m.data = Expand;
                            genericMsgLisDescs[i] = tmp;
                        }

                     
                        break;
                    }

                case 2:
                    {

                        int OLT_ID = -1;
                        for (int i = 0; i < genericMsgListBurst.Count; i++)
                        {
                            GenericMsgFile tmp = genericMsgListBurst[i];
                            msgData[] Expand4 = new msgData[tmp.msgFile_m.data.Length + 1];
                            Array.Copy(tmp.msgFile_m.data, Expand4, tmp.msgFile_m.data.Length);
                            Expand4[Expand4.Length - 1].NameID = "talisman_olt_" + tmp.msgFile_m.data.Length.ToString("000");
                            OLT_ID = tmp.msgFile_m.data.Length;
                            Expand4[Expand4.Length - 1].ID = tmp.msgFile_m.data.Length;
                            Expand4[Expand4.Length - 1].Lines = new string[] { "" };
                            tmp.msgFile_m.data = Expand4;

                            genericMsgListBurst[i] = tmp;
                        }



                        for (int i = 0; i < genericMsgListNameBurstBTLHUD.Count; i++)
                        {
                            GenericMsgFile tmp = genericMsgListNameBurstBTLHUD[i];
                            msgData[] Expand5 = new msgData[tmp.msgFile_m.data.Length + 1];
                            Array.Copy(tmp.msgFile_m.data, Expand5, tmp.msgFile_m.data.Length);
                            Expand5[Expand5.Length - 1].NameID = "BHD_OLT_000_" + OLT_ID.ToString();// 
                            Expand5[Expand5.Length - 1].ID = tmp.msgFile_m.data.Length;
                            Expand5[Expand5.Length - 1].Lines = new string[] { "" };
                            tmp.msgFile_m.data = Expand5;

                            genericMsgListNameBurstBTLHUD[i] = tmp;
                        }




                        for (int i = 0; i < genericMsgListNameBurstPause.Count; i++)
                        {
                            GenericMsgFile tmp = genericMsgListNameBurstPause[i];
                            msgData[] Expand6 = new msgData[tmp.msgFile_m.data.Length + 1];
                            Array.Copy(tmp.msgFile_m.data, Expand6, tmp.msgFile_m.data.Length);
                            Expand6[Expand6.Length - 1].NameID = "BHD_OLT_000_" + OLT_ID.ToString();
                            Expand6[Expand6.Length - 1].ID = tmp.msgFile_m.data.Length;
                            Expand6[Expand6.Length - 1].Lines = new string[] { "" };
                            tmp.msgFile_m.data = Expand6;

                            genericMsgListNameBurstPause[i] = tmp;
                        }

				
                        break;
                    }

       
            }

        }
        #region ListBox Methods

        private void lstvBasic_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstvBasic.SelectedItems.Count != 0 && !lockMod)
            {
                txtEditNameb.Text = lstvBasic.SelectedItems[0].SubItems[0].Text;
                txtEditValueb.Text = lstvBasic.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void txtEditValueb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                lstvBasic.SelectedItems[0].SubItems[1].Text = txtEditValueb.Text;
                float n;
                if (float.TryParse(txtEditValueb.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 144 + (lstvBasic.SelectedItems[0].Index * 4), 4);
            }
        }

        private void lstvEffect1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstvEffect1.SelectedItems.Count != 0 && !lockMod)
            {
                txtEditName1.Text = lstvEffect1.SelectedItems[0].SubItems[0].Text;
                txtEditValue1.Text = lstvEffect1.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void txtEditValue1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                lstvEffect1.SelectedItems[0].SubItems[1].Text = txtEditValue1.Text;
                float n;
                if (float.TryParse(txtEditValue1.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 368 + (lstvEffect1.SelectedItems[0].Index * 4), 4);
            }
        }

        private void lstvEffect2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstvEffect2.SelectedItems.Count != 0 && !lockMod)
            {
                txtEditName2.Text = lstvEffect2.SelectedItems[0].SubItems[0].Text;
                txtEditValue2.Text = lstvEffect2.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void txtEditValue2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                lstvEffect2.SelectedItems[0].SubItems[1].Text = txtEditValue2.Text;
                float n;
                if (float.TryParse(txtEditValue2.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 592 + (lstvEffect2.SelectedItems[0].Index * 4), 4);
            }
        }

        public static void Applybyte(ref byte[] file, byte[] data, int pos, int count)
        {
            for (int i = 0; i < count; i++)
                file[pos + i] = data[i];
        }
        #endregion

        private void SaveXV2SSEdit()
        {
            List<byte> Finalize = new List<byte>();
            //UNLEASHED: HEADER
            Finalize.AddRange(new byte[] { 0x23, 0x49, 0x44, 0x42, 0xFE, 0xFF, 0x07, 0x00 });
            //UNLEASHED: ITEM COUNT
            Finalize.AddRange(BitConverter.GetBytes(Items.Length));
            //UNLEASHED: OFFSET OF FIRST SS
            Finalize.AddRange(new byte[] { 0x10, 0x00, 0x00, 0x00 });

            //UNLEASHED: ADD IDM ITEMS
            for (int i = 0; i < Items.Length; i++)
                Finalize.AddRange(Items[i].Data);

            FileStream fs = new FileStream(FileName, FileMode.Create);
            fs.Write(Finalize.ToArray(), 0, Finalize.Count);
            fs.Close();

            if (NamesLoaded)
                msgStream.Save(Names, FileNameMsgN);

            
            
               

            if (DescsLoaded)
                msgStream.Save(Descs, FileNameMsgD);

            if (BurstLoaded)
            {
                msgStream.Save(Burst, FileNameMsgB);
                msgStream.Save(BurstBTLHUD, FileNameMsgB_BTLHUD);
                msgStream.Save(BurstPause, FileNameMsgB_Pause);
            }
            //UNLEASHED: write all generic msg files, lets use "Names" as the counter since they all share the same length anyway.
            for (int i = 0; i < genericMsgListNames.Count; i++)
            {
                msgStream.Save(genericMsgListNames[i].msgFile_m, genericMsgListNames[i].msgPath_m);
                msgStream.Save(genericMsgLisDescs[i].msgFile_m, genericMsgLisDescs[i].msgPath_m);
                msgStream.Save(genericMsgListBurst[i].msgFile_m, genericMsgListBurst[i].msgPath_m);
                msgStream.Save(genericMsgListNameBurstBTLHUD[i].msgFile_m, genericMsgListNameBurstBTLHUD[i].msgPath_m);
                msgStream.Save(genericMsgListNameBurstPause[i].msgFile_m, genericMsgListNameBurstPause[i].msgPath_m);
            }
        }
        
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

            SaveXV2SSEdit();
        //UNLEASHED: added msgbox
            hasSavedChanges = true;
           MessageBox.Show("Save Successful and files writtin to 'data' folder\nTo see changes in-game, the XV2Patcher must be installed.");
        }

        public void EffectData()
        {

            if (File.Exists(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"/" + "EffectData.xml"))
            {
                XmlDocument xd = new XmlDocument();
                xd.Load(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"/" + "EffectData.xml");
                eList.ConstructList(xd.SelectSingleNode("EffectData/Effects").ChildNodes);
                aList.ConstructList(xd.SelectSingleNode("EffectData/Activations").ChildNodes);
                tList.ConstructList(xd.SelectSingleNode("EffectData/Targets").ChildNodes);
                cList.ConstructList(xd.SelectSingleNode("EffectData/Colors").ChildNodes);
                kList.ConstructList(xd.SelectSingleNode("EffectData/Kitypes").ChildNodes);
                bList.ConstructList(xd.SelectSingleNode("EffectData/Checkboxs").ChildNodes);
            }
            else
            {
                eList.ConstructFromUnknown(ref Items);
                aList.ConstructFromUnknown(ref Items);
                tList.ConstructFromUnknown(ref Items);
                cList.ConstructFromUnknown(ref Items);
                kList.ConstructFromUnknown(ref Items);
                bList.ConstructFromUnknown(ref Items);

                //build EFfectData
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
                {
                    Indent = true,
                    IndentChars = "\t",

                };

                using (XmlWriter xw = XmlWriter.Create("EffectData.xml", xmlWriterSettings))
                {
                    xw.WriteStartDocument();
                    xw.WriteStartElement("EffectData");
                    xw.WriteStartElement("Effects");
                    for (int i = 0; i < eList.effects.Length; i++)
                    {
                        xw.WriteStartElement("Item");
                        xw.WriteStartAttribute("id");
                        xw.WriteValue(eList.effects[i].ID);
                        xw.WriteEndAttribute();
                        xw.WriteStartAttribute("hex");
                        xw.WriteValue(String.Format("{0:X}",eList.effects[i].ID));
                        xw.WriteEndAttribute();
                        xw.WriteValue(eList.effects[i].Description);
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();

                    xw.WriteStartElement("Activations");
                    for (int i = 0; i < aList.activations.Length; i++)
                    {
                        xw.WriteStartElement("Item");
                        xw.WriteStartAttribute("id");
                        xw.WriteValue(aList.activations[i].ID);
                        xw.WriteEndAttribute();
                        xw.WriteStartAttribute("hex");
                        xw.WriteValue(String.Format("{0:X}", aList.activations[i].ID));
                        xw.WriteEndAttribute();
                        xw.WriteValue(aList.activations[i].Description);
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();

                    xw.WriteStartElement("Targets");
                    for (int i = 0; i < tList.targets.Length; i++)
                    {
                        xw.WriteStartElement("Item");
                        xw.WriteStartAttribute("id");
                        xw.WriteValue(tList.targets[i].ID);
                        xw.WriteEndAttribute();
                        xw.WriteStartAttribute("hex");
                        xw.WriteValue(String.Format("{0:X}", tList.targets[i].ID));
                        xw.WriteEndAttribute();
                        xw.WriteValue(tList.targets[i].Description);
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();

                    xw.WriteStartElement("Colors");
                    for (int i = 0; i < cList.colors.Length; i++)
                    {
                        xw.WriteStartElement("Item");
                        xw.WriteStartAttribute("id");
                        xw.WriteValue(cList.colors[i].ID);
                        xw.WriteEndAttribute();
                        xw.WriteStartAttribute("hex");
                        xw.WriteValue(String.Format("{0:X}", cList.colors[i].ID));
                        xw.WriteEndAttribute();
                        xw.WriteValue(cList.colors[i].Description);
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();

                    xw.WriteStartElement("Kitypes");
                    for (int i = 0; i < kList.kitypes.Length; i++)
                    {
                        xw.WriteStartElement("Item");
                        xw.WriteStartAttribute("id");
                        xw.WriteValue(kList.kitypes[i].ID);
                        xw.WriteEndAttribute();
                        xw.WriteStartAttribute("hex");
                        xw.WriteValue(String.Format("{0:X}", kList.kitypes[i].ID));
                        xw.WriteEndAttribute();
                        xw.WriteValue(kList.kitypes[i].Description);
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();

                    xw.WriteStartElement("Checkboxs");
                    for (int i = 0; i < bList.checkboxs.Length; i++)
                    {
                        xw.WriteStartElement("Item");
                        xw.WriteStartAttribute("id");
                        xw.WriteValue(bList.checkboxs[i].ID);
                        xw.WriteEndAttribute();
                        xw.WriteStartAttribute("hex");
                        xw.WriteValue(String.Format("{0:X}", bList.checkboxs[i].ID));
                        xw.WriteEndAttribute();
                        xw.WriteValue(bList.checkboxs[i].Description);
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();

                    xw.WriteEndElement();
                    xw.WriteEndDocument();
                    xw.Close();
                }

            }

            cbEffect1.Items.Clear();
            cbEffect2.Items.Clear();
            cbActive1.Items.Clear();
            cbActive2.Items.Clear();

            for (int i = 0; i < eList.effects.Length; i++)
            {
                //DEMON: Removed the hex ID from view here too. no use aside from debugging.
                cbEffectb.Items.Add(eList.effects[i].ID.ToString() + " - " + eList.effects[i].Description);
                cbEffect1.Items.Add(eList.effects[i].ID.ToString() + " - " + eList.effects[i].Description);
                cbEffect2.Items.Add(eList.effects[i].ID.ToString() + " - " + eList.effects[i].Description);
                //cbEffectb.Items.Add(eList.effects[i].ID.ToString() + "/" + String.Format("{0:X}", eList.effects[i].ID) + " " + eList.effects[i].Description);
                //cbEffect1.Items.Add(eList.effects[i].ID.ToString() + "/" + String.Format("{0:X}", eList.effects[i].ID) + " " + eList.effects[i].Description);
                //cbEffect2.Items.Add(eList.effects[i].ID.ToString() + "/" + String.Format("{0:X}", eList.effects[i].ID) + " " + eList.effects[i].Description);
            }

            for (int i = 0; i < aList.activations.Length; i++)
            {
                cbActiveb.Items.Add(aList.activations[i].ID.ToString() + " - " + aList.activations[i].Description);
                cbActive1.Items.Add(aList.activations[i].ID.ToString() + " - " + aList.activations[i].Description);
                cbActive2.Items.Add(aList.activations[i].ID.ToString() + " - " + aList.activations[i].Description);
                //cbActiveb.Items.Add(aList.activations[i].ID.ToString() + "/" + String.Format("{0:X}", aList.activations[i].ID) + " " + aList.activations[i].Description);
                //cbActive1.Items.Add(aList.activations[i].ID.ToString() + "/" + String.Format("{0:X}", aList.activations[i].ID) + " " + aList.activations[i].Description);
                //cbActive2.Items.Add(aList.activations[i].ID.ToString() + "/" + String.Format("{0:X}", aList.activations[i].ID) + " " + aList.activations[i].Description);
            }

            for (int i = 0; i < tList.targets.Length; i++)
            {
                cbTargetb.Items.Add(tList.targets[i].ID.ToString() + " - " + tList.targets[i].Description);
                cbTarget1.Items.Add(tList.targets[i].ID.ToString() + " - " + tList.targets[i].Description);
                cbTarget2.Items.Add(tList.targets[i].ID.ToString() + " - " + tList.targets[i].Description);
            }

            for (int i = 0; i < cList.colors.Length; i++)
            {
                cbLBColor.Items.Add(cList.colors[i].ID.ToString() + " - " + cList.colors[i].Description);
            }

            for (int i = 0; i < kList.kitypes.Length; i++)
            {
                cbKiType.Items.Add(kList.kitypes[i].ID.ToString() + " - " + kList.kitypes[i].Description);
            }

            for (int i = 0; i < bList.checkboxs.Length; i++)
            {
                cbAuraCheckb.Items.Add(bList.checkboxs[i].ID.ToString() + " - " + bList.checkboxs[i].Description);
                cbAuraCheck1.Items.Add(bList.checkboxs[i].ID.ToString() + " - " + bList.checkboxs[i].Description);
                cbAuraCheck2.Items.Add(bList.checkboxs[i].ID.ToString() + " - " + bList.checkboxs[i].Description);
                cbUnknownCheckb.Items.Add(bList.checkboxs[i].ID.ToString() + " - " + bList.checkboxs[i].Description);
                cbUnknownCheck1.Items.Add(bList.checkboxs[i].ID.ToString() + " - " + bList.checkboxs[i].Description);
                cbUnknownCheck2.Items.Add(bList.checkboxs[i].ID.ToString() + " - " + bList.checkboxs[i].Description);
            }

        }

        public int FindmsgIndex(ref msg msgdata,int id)
        {
            for (int i = 0; i < msgdata.data.Length; i++)
            {
                if (msgdata.data[i].ID == id)
                    return i;
            }
            return 0;
        }

        public byte[] int16byte(string text)
        {
            Int16 value;
            value = Int16.Parse(text);
            return BitConverter.GetBytes(value);
        }

        public byte[] int32byte(string text)
        {
            Int32 value;
            value = Int32.Parse(text);
            return BitConverter.GetBytes(value);
        }

        public byte[] floatbyte(string text)
        {
            float value;
            value = float.Parse(text);
            return BitConverter.GetBytes(value);
        }

        private void itemList_SelectedIndexChanged(object sender, EventArgs e)
        {
                hasSavedChanges = false;
                currentSuperSoulIndex = itemList.SelectedIndex;
                UpdateData();
            
        }

        #region edit item
        private void txtMsgName_TextChanged(object sender, EventArgs e)
        {
            if (NamesLoaded && !lockMod)
            {
                Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0] = txtMsgName.Text;
                itemList.Items[itemList.SelectedIndex] = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 0).ToString() +  " - " + Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0];
            }
        }

        private void txtMsgDesc_TextChanged(object sender, EventArgs e)
        {
            if (DescsLoaded && !lockMod)
                Descs.data[Items[itemList.SelectedIndex].msgIndexDesc].Lines[0] = txtMsgDesc.Text;
        }

        private void txtMsgLBDesc_TextChanged(object sender, EventArgs e)
        {
            //UNLEASHED: get the LB index from the soul (warning, it is shared)
            if (BurstLoaded && !lockMod)
                Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].Lines[0] = txtMsgLBDesc.Text;
        }

        private void cbStar_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
                Array.Copy(BitConverter.GetBytes((short)(cbStar.SelectedIndex + 1)), 0, Items[itemList.SelectedIndex].Data, 2, 2);
        }

        private void txtNameID_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                short ID;
                if (short.TryParse(txtNameID.Text, out ID))
                    Array.Copy(BitConverter.GetBytes(ID), 0, Items[itemList.SelectedIndex].Data, 4, 2);

                if (NamesLoaded)
                {
                    Items[itemList.SelectedIndex].msgIndexName = FindmsgIndex(ref Names, BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 4));
                    txtMsgName.Text = Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0];
                }

                if (NamesLoaded)
                    itemList.Items[itemList.SelectedIndex] = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 0).ToString() +  " - " + Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0];
            }
        }

        private void txtDescID_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                short ID;
                if (short.TryParse(txtDescID.Text, out ID))
                    Array.Copy(BitConverter.GetBytes(ID), 0, Items[itemList.SelectedIndex].Data, 6, 2);

                if (DescsLoaded)
                {
                    Items[itemList.SelectedIndex].msgIndexDesc = FindmsgIndex(ref Descs, BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 6));
                    txtMsgDesc.Text = Descs.data[Items[itemList.SelectedIndex].msgIndexDesc].Lines[0];
                }
            }
        }

        private void txtShopTest_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                short n;
                if (short.TryParse(txtShopTest.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 10, 2);
            }
        }

        private void txtTPTest_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                short n;
                if (short.TryParse(txtTPTest.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 12, 2);
            }
        }

        private void txtBuy_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int n;
                if (int.TryParse(txtBuy.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 16, 4);
            }
        }

        private void txtSell_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int n;
                if (int.TryParse(txtSell.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 20, 4);
            }
        }

        private void txtRace_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                short n;
                if (short.TryParse(txtRace.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 24, 2);
            }
        }

        private void txtBuyTP_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int n;
                if (int.TryParse(txtBuyTP.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 28, 4);
            }
        }

        private void cbKiType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = kList.kitypes[cbKiType.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 32, 4);
            }
        }

        private void txtLBAura_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                short n;
                if (short.TryParse(txtLBAura.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 36, 2);
            }
        }

        private void cbLBColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = cList.colors[cbLBColor.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 38, 2);
            }
        }

        private void txtLBDesc_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                short ID;
                if (short.TryParse(txtLBDesc.Text, out ID))
                    Array.Copy(BitConverter.GetBytes(ID), 0, Items[itemList.SelectedIndex].Data, 40, 2);

                if (BurstLoaded)
                {
                    Items[itemList.SelectedIndex].msgIndexBurst = FindmsgIndex(ref Burst, BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 40));
                    txtMsgLBDesc.Text = Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].Lines[0];
                    //Demon: updates the in battle description text when the description id is changed
                    Items[itemList.SelectedIndex].msgIndexBurstBTL = getLB_BTL_Pause_DescID(BurstBTLHUD, Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].NameID);
                    Items[itemList.SelectedIndex].msgIndexBurstPause = getLB_BTL_Pause_DescID(BurstPause, Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].NameID);
                    txtMsgLBDescBTL.Text = BurstBTLHUD.data[Items[itemList.SelectedIndex].msgIndexBurstBTL].Lines[0];

                }

            }
        }

        private void txtLBSoulID1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                ushort n;
                if (ushort.TryParse(txtLBSoulID1.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 42, 2);
            }
        }

        private void txtLBSoulID2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                ushort n;
                if (ushort.TryParse(txtLBSoulID2.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 44, 2);
            }
        }

        private void txtLBSoulID3_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                ushort n;
                if (ushort.TryParse(txtLBSoulID3.Text, out n))
                    Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 46, 2);
            }
        }

        private void cbEffectb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = eList.effects[cbEffectb.SelectedIndex].ID;
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 48, 4);
            }
        }

        private void cbActiveb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = aList.activations[cbActiveb.SelectedIndex].ID;
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 52, 4);
            }
        }

        private void txtTimesb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtTimesb.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 56, 4);
                }
            }
        }

        private void txtADelayb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtADelayb.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 60, 4);
                }
            }
        }

        private void txtAValb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtAValb.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 64, 4);
                }
            }
        }

        private void txtUnkf20b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf20b.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 68, 4);
                }
            }
        }

        private void txtUnkf24b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf24b.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 72, 4);
                }
            }
        }

        private void txtUnkf28b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf28b.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 76, 4);
                }
            }
        }

        private void txtUnkf32b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf32b.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 80, 4);
                }
            }
        }

        private void txtUnkf36b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf36b.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 84, 4);
                }
            }
        }

        private void txtChanceb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtChanceb.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 88, 4);
                }
            }
        }

        private void txtUnki44b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnki44b.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 92, 4);
                }
            }
        }

        private void txtEAmntb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEAmntb.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 96, 4);
                }
            }
        }

        private void txtETimeb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtETimeb.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 100, 4);
                }
            }
        }

        private void txtEUnknownAb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEUnknownAb.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 104, 4);
                }
            }
        }

        private void txtEUnknownBb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEUnknownBb.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 108, 4);
                }
            }
        }

        private void txtUnkf64b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf64b.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 112, 4);
                }
            }
        }

        private void txtUnkf68b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf68b.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 116, 4);
                }
            }
        }

        private void cbUnknownCheckb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = bList.checkboxs[cbUnknownCheckb.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 120, 4);
            }
        }

        private void txtUnknownb_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnknownb.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 124, 4);
                }
            }
        }

        private void cbAuraCheckb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = bList.checkboxs[cbAuraCheckb.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 128, 4);
            }
        }
        
        private void txtAurab_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtAurab.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 132, 4);
                }
            }
        }

        private void txtUnki88b_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnki88b.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 136, 4);
                }
            }
        }

        private void cbTargetb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = tList.targets[cbTargetb.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 140, 4);
            }
        }

        private void cbEffect1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = eList.effects[cbEffect1.SelectedIndex].ID;
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 272, 4);
            }
        }

        private void cbActive1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = aList.activations[cbActive1.SelectedIndex].ID;
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 276, 4);
            }
        }

        private void txtTimes1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod) { 
                int ID;
                if (int.TryParse(txtTimes1.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 280, 4);
                }
            }
        }

        private void txtADelay1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtADelay1.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 284, 4);
                }
            }
        }

        private void txtAVal1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtAVal1.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 288, 4);
                }
            }
        }

        private void txtUnkf201_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf201.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 292, 4);
                }
            }
        }

        private void txtUnkf241_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf241.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 296, 4);
                }
            }
        }

        private void txtUnkf281_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf281.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 300, 4);
                }
            }
        }

        private void txtUnkf321_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf321.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 304, 4);
                }
            }
        }

        private void txtUnkf361_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf361.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 308, 4);
                }
            }
        }

        private void txtChance1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtChance1.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 312, 4);
                }
            }
        }

        private void txtUnki441_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnki441.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 316, 4);
                }
            }
        }

        private void txtEAmnt1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEAmnt1.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 320, 4);
                }
            }
        }

        private void txtETime1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtETime1.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 324, 4);
                }
            }
        }

        private void txtEUnknownA1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEUnknownA1.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 328, 4);
                }
            }
        }

        private void txtEUnknownB1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEUnknownB1.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 332, 4);
                }
            }
        }

        private void txtUnkf641_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf641.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 336, 4);
                }
            }
        }

        private void txtUnkf681_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf681.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 340, 4);
                }
            }
        }

        private void cbUnknownCheck1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = bList.checkboxs[cbUnknownCheck1.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 344, 4);
            }
        }

        private void txtUnknown1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnknown1.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 348, 4);
                }
            }
        }

        private void cbAuraCheck1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = bList.checkboxs[cbAuraCheck1.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 352, 4);
            }
        }

        private void txtAura1_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtAura1.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 356, 4);
                }
            }
        }

        private void txtUnki881_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnki881.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 360, 4);
                }
            }
        }

        private void cbTarget1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = tList.targets[cbTarget1.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 364, 4);
            }
        }

        private void cbEffect2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = eList.effects[cbEffect2.SelectedIndex].ID;
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 496, 4);
            }
        }

        private void cbActive2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = aList.activations[cbActive2.SelectedIndex].ID;
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 500, 4);
            }
        }

        private void txtTimes2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtTimes2.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 504, 4);
                }
            }
        }

        private void txtADelay2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtADelay2.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 508, 4);
                }
            }
        }

        private void txtAVal2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtAVal2.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 512, 4);
                }
            }
        }

        private void txtUnkf202_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf202.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 516, 4);
                }
            }
        }

        private void txtUnkf242_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf242.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 520, 4);
                }
            }
        }

        private void txtUnkf282_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf282.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 524, 4);
                }
            }
        }

        private void txtUnkf322_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf322.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 528, 4);
                }
            }
        }

        private void txtUnkf362_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf362.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 532, 4);
                }
            }
        }

        private void txtChance2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtChance2.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 536, 4);
                }
            }
        }

        private void txtUnki442_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnki442.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 540, 4);
                }
            }
        }

        private void txtEAmnt2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEAmnt2.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 544, 4);
                }
            }
        }

        private void txtETime2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtETime2.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 548, 4);
                }
            }
        }

        private void txtEUnknownA2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEUnknownA2.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 552, 4);
                }
            }
        }

        private void txtEUnknownB2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtEUnknownB2.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 556, 4);
                }
            }
        }

        private void txtUnkf642_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf642.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 560, 4);
                }
            }
        }

        private void txtUnkf682_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                float ID;
                if (float.TryParse(txtUnkf682.Text, out ID))
                {
                    byte[] pass;

                    pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 564, 4);
                }
            }
        }

        private void cbUnknownCheck2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = bList.checkboxs[cbUnknownCheck2.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 568, 4);
            }
        }

        private void txtUnknown2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnknown2.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 572, 4);
                }
            }
        }

        private void cbAuraCheck2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = bList.checkboxs[cbAuraCheck2.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 576, 4);
            }
        }

        private void txtAura2_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtAura2.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 580, 4);
                }
            }
        }

        private void txtUnki882_TextChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID;
                if (int.TryParse(txtUnki882.Text, out ID))
                {
                    byte[] pass;
                    if (ID == -1)
                        pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                    else
                        pass = BitConverter.GetBytes(ID);

                    Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 584, 4);
                }
            }
        }

        private void cbTarget2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!lockMod)
            {
                int ID = tList.targets[cbTarget2.SelectedIndex].ID;
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 588, 4);
            }
        }
        #endregion

        private void UpdateData()
        {
            lockMod = true;
            // Super Soul Details
            if (NamesLoaded)
                txtMsgName.Text = Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0];
            if (DescsLoaded)
                txtMsgDesc.Text = Descs.data[Items[itemList.SelectedIndex].msgIndexDesc].Lines[0];
            if (BurstLoaded)
            {
                txtMsgLBDesc.Text = Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].Lines[0];
                
                txtMsgLBDescBTL.Text = BurstBTLHUD.data[Items[itemList.SelectedIndex].msgIndexBurstBTL].Lines[0];
                //UNLEASHED: Add BTL LB Desc
            }
                
            txtID.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 0).ToString();
            cbStar.SelectedIndex = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 2) - 1;
            txtNameID.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 4).ToString();
            txtDescID.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 6).ToString();
            //Demon: should never be edited in a talisman idb so commented out
            //txtIDbType.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 8).ToString();
            txtShopTest.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 10).ToString();
            txtTPTest.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 12).ToString();
            //Demon: unknown what this is for but it's always -1 anyway
            //txti_14.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 14).ToString();
            txtBuy.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 16).ToString();
            txtSell.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 20).ToString();
            txtRace.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 24).ToString();
            txtBuyTP.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 28).ToString();
            cbKiType.SelectedIndex = kList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 32));
            txtLBAura.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 36).ToString();
            cbLBColor.SelectedIndex = cList.FindIndex(BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 38));
            txtLBDesc.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 40).ToString();
            txtLBSoulID1.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 42).ToString();
            txtLBSoulID2.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 44).ToString();
            txtLBSoulID3.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 46).ToString();

            // Basic Details
            cbEffectb.SelectedIndex = eList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 48));
            cbActiveb.SelectedIndex = aList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 52));
            txtTimesb.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 56).ToString();
            txtADelayb.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 60).ToString();
            txtAValb.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 64).ToString();
            txtUnkf20b.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 68).ToString();
            txtUnkf24b.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 72).ToString();
            txtUnkf28b.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 76).ToString();
            txtUnkf32b.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 80).ToString();
            txtUnkf36b.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 84).ToString();
            txtChanceb.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 88).ToString();
            txtUnki44b.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 92).ToString();
            txtEAmntb.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 96).ToString();
            txtETimeb.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 100).ToString();
            txtEUnknownAb.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 104).ToString();
            txtEUnknownBb.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 108).ToString();
            txtUnkf64b.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 112).ToString();
            txtUnkf68b.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 116).ToString();
            cbUnknownCheckb.SelectedIndex = bList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 120));
            txtUnknownb.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 124).ToString();
            cbAuraCheckb.SelectedIndex = bList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 128));
            txtAurab.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 132).ToString();
            txtUnki88b.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 136).ToString();
            cbTargetb.SelectedIndex = tList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 140));

            for (int i = 0; i < lstvBasic.Items.Count; i++)
            {
               //Demon: used for subtracting 1 from all the stats in the idb
               //float stateValue = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 144 + (i * 4));
               //stateValue -= 1.0f;
               //Array.Copy(BitConverter.GetBytes(stateValue),0,Items[itemList.SelectedIndex].Data, 144 + (i * 4), 4);

                lstvBasic.Items[i].SubItems[1].Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 144 + (i * 4)).ToString();
            }

            //Effest 1 Details
            cbEffect1.SelectedIndex = eList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 272));
            cbActive1.SelectedIndex = aList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 276));
            txtTimes1.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 280).ToString();
            txtADelay1.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 284).ToString();
            txtAVal1.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 288).ToString();
            txtUnkf201.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 292).ToString();
            txtUnkf241.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 296).ToString();
            txtUnkf281.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 300).ToString();
            txtUnkf321.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 304).ToString();
            txtUnkf361.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 308).ToString();
            txtChance1.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 312).ToString();
            txtUnki441.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 316).ToString();
            txtEAmnt1.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 320).ToString();
            txtETime1.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 324).ToString();
            txtEUnknownA1.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 328).ToString();
            txtEUnknownB1.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 332).ToString();
            txtUnkf641.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 336).ToString();
            txtUnkf681.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 340).ToString();
            cbUnknownCheck1.SelectedIndex = bList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 344));
            txtUnknown1.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 348).ToString();
            cbAuraCheck1.SelectedIndex = bList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 352));
            txtAura1.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 356).ToString();
            txtUnki881.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 360).ToString();
            cbTarget1.SelectedIndex = tList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 364));

            for (int i = 0; i < lstvEffect1.Items.Count; i++)
            {
                //Demon: used for subtracting 1 from all the stats in the idb
                //float stateValue = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 368 + (i * 4));
                //stateValue -= 1.0f;
                //Array.Copy(BitConverter.GetBytes(stateValue),0,Items[itemList.SelectedIndex].Data, 368 + (i * 4), 4);

                lstvEffect1.Items[i].SubItems[1].Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 368 + (i * 4)).ToString();
            }

            //Effect 2 Details
            cbEffect2.SelectedIndex = eList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 496));
            cbActive2.SelectedIndex = aList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 500));
            txtTimes2.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 504).ToString();
            txtADelay2.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 508).ToString();
            txtAVal2.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 512).ToString();
            txtUnkf202.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 516).ToString();
            txtUnkf242.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 520).ToString();
            txtUnkf282.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 524).ToString();
            txtUnkf322.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 528).ToString();
            txtUnkf362.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 532).ToString();
            txtChance2.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 536).ToString();
            txtUnki442.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 540).ToString();
            txtEAmnt2.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 544).ToString();
            txtETime2.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 548).ToString();
            txtEUnknownA2.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 552).ToString();
            txtEUnknownB2.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 556).ToString();
            txtUnkf642.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 560).ToString();
            txtUnkf682.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 564).ToString();
            cbUnknownCheck2.SelectedIndex = bList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 568));
            txtUnknown2.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 572).ToString();
            cbAuraCheck2.SelectedIndex = bList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 576));
            txtAura2.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 580).ToString();
            txtUnki882.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 584).ToString();
            cbTarget2.SelectedIndex = tList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 588));

            for (int i = 0; i < lstvEffect2.Items.Count; i++)
            {
                //Demon: used for subtracting 1 from all the stats in the idb
                //float stateValue = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 592 + (i * 4));
                //stateValue -= 1.0f;
                //Array.Copy(BitConverter.GetBytes(stateValue),0,Items[itemList.SelectedIndex].Data, 592 + (i * 4), 4);

                lstvEffect2.Items[i].SubItems[1].Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 592 + (i * 4)).ToString();
            }

            if (lstvBasic.SelectedItems.Count != 0)
            {
                txtEditNameb.Text = lstvBasic.SelectedItems[0].SubItems[0].Text;
                txtEditValueb.Text = lstvBasic.SelectedItems[0].SubItems[1].Text;
            }

            if (lstvEffect1.SelectedItems.Count != 0)
            {
                txtEditName1.Text = lstvEffect1.SelectedItems[0].SubItems[0].Text;
                txtEditValue1.Text = lstvEffect1.SelectedItems[0].SubItems[1].Text;
            }

            if (lstvEffect2.SelectedItems.Count != 0)
            {
                txtEditName2.Text = lstvEffect2.SelectedItems[0].SubItems[0].Text;
                txtEditValue2.Text = lstvEffect2.SelectedItems[0].SubItems[1].Text;
            }
            lockMod = false;
        }

        private bool isLimitBurst(byte[] SSFData)
        {
            if (BitConverter.ToUInt32(SSFData, 0x18) == 0xFFFFFFFF)
                return false;

            return true;
        }

        private void resolveLBIDsForParentSS(ref List<int> indexCollections, byte[] SSFData, ushort LBID)
        {

            ushort parentSSIndex = BitConverter.ToUInt16(SSFData, 0x18);

            ushort burstSlot = BitConverter.ToUInt16(SSFData, 0x1A);
         
            int parentSSItemIndex = indexCollections[parentSSIndex];

      

            switch (burstSlot)
            {
                case 1:
                    {
                    
                        byte[] changedBytes = BitConverter.GetBytes(LBID);
                        Items[parentSSItemIndex].Data = StaticMethods.replaceBytesInByteArray(Items[parentSSItemIndex].Data, changedBytes, 42);

                        break;
                    }

                case 2:
                    {
                        byte[] changedBytes = BitConverter.GetBytes(LBID);
                        Items[parentSSItemIndex].Data = StaticMethods.replaceBytesInByteArray(Items[parentSSItemIndex].Data, changedBytes, 44);

                        break;
                    }

                case 3:
                    {
                        byte[] changedBytes = BitConverter.GetBytes(LBID);
                        Items[parentSSItemIndex].Data = StaticMethods.replaceBytesInByteArray(Items[parentSSItemIndex].Data, changedBytes, 46);

                        break;
                    }
            }

        }

        private bool importSSP(string sspPath)
        {

            SSP sspFile = new SSP();
            sspFile.SSPRead(sspPath);


            //UNLEASHED: before we import, lets create a backup for all MSG files and the IDB
            //so we can revert back incase anything happens (mostly if there is not enough space to install SS)

            msg orgNames = Names;
            msg orgDescs = Descs;
            msg orgBurst = Burst;
            msg orgBurstBTLHUD = BurstBTLHUD;
            msg orgBurstPause = BurstPause;
            idbItem[] orgItems = Items;

            List<int> indexCollections = new List<int>();
            int index = -1;
            byte[] tempData;
            ushort intIDofLB;

            //UNLEASHED: in the SSP, it is expected that parent souls are installed first and then the child limit burst souls.
            for (int i = 0; i < sspFile.Souls.Count(); i++)
            {
              tempData = sspFile.Souls[i].m_data;
              index = AddSS(tempData);

              if (index < 0)
              {
                  Names = orgNames;
                  Descs = orgDescs;
                  Burst = orgBurst;
                  BurstBTLHUD = orgBurstBTLHUD;
                  BurstPause = orgBurstPause;
                  Items = orgItems;
                  return false;
              }

              intIDofLB = BitConverter.ToUInt16(Items[index].Data, 0);
              if (!isLimitBurst(tempData))
                  indexCollections.Add(index);

              else
                  resolveLBIDsForParentSS(ref indexCollections, tempData,intIDofLB);
              

            }

            return true;

        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //add/import Z -soul
            //load zss file
            OpenFileDialog browseFile = new OpenFileDialog();
            browseFile.Filter = "Super Soul Package | *.ssp";
            browseFile.Title = "Select the Super Soul Package you want to import.";
            if (browseFile.ShowDialog() == DialogResult.Cancel)
                return;

                //and we are done, rebuild itemlist
            if (!importSSP(browseFile.FileName))
            {
                MessageBox.Show("Error occurred when installing SSP file.");
                return;
            }

            itemList.Items.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                if (NamesLoaded)
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() +  " - " + Names.data[Items[i].msgIndexName].Lines[0]);
                else
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " / " + String.Format("{0:X}", BitConverter.ToUInt16(Items[i].Data, 0)));
            }
            itemList.SelectedIndex = 0;
            MessageBox.Show("SSP imported successfully");
         
            
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //remove Z-soul  
            //UNLEASHED: XV1 Left over?
            //      if (Items.Length > 211)
         



               //UNLEASHED: there are probably better methods to do this, but working with Lists is just so much easier.
       
                List<idbItem> Reduce = Items.ToList<idbItem>();
                Reduce.RemoveAt(currentSuperSoulIndex);
                Items = Reduce.ToArray();
                
         
                
                itemList.Items.Clear();
                for (int i = 0; i < Items.Length; i++)
                {
                    if (NamesLoaded)
                        itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() +  " - " + Names.data[Items[i].msgIndexName].Lines[0]);
                    else
                        itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " / " + String.Format("{0:X}", BitConverter.ToUInt16(Items[i].Data, 0)));
                }


                //UNLEASHED: return to first item
                itemList.SelectedIndex = 0;
            }        

        private void nameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //add msg name
            if (NamesLoaded)
            {

                msgData[] Expand = new msgData[Names.data.Length + 1];
                Array.Copy(Names.data, Expand, Names.data.Length);
                Expand[Expand.Length - 1].NameID = "talisman_" + Names.data.Length.ToString("000");
                Expand[Expand.Length - 1].ID = Names.data.Length;
                Expand[Expand.Length - 1].Lines = new string[] { "New Name Entry" };
                Names.data = Expand;

                writeToMsgText(0);

                txtNameID.Text = Names.data[Names.data.Length - 1].ID.ToString();
            }
        }

        private void descriptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //remove msg name
            if (Items.Length > 211 && NamesLoaded)
            {
                msgData[] reduce = new msgData[Names.data.Length - 1];
                Array.Copy(Names.data, reduce, Names.data.Length - 1);
                Names.data = reduce;
            }
        }

        private void nameToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            
            //add msg desc
            if (DescsLoaded)
            {
                
                msgData[] Expand = new msgData[Descs.data.Length + 1];
                Array.Copy(Descs.data, Expand, Descs.data.Length);
                Expand[Expand.Length - 1].NameID = "talisman_eff_" + Descs.data.Length.ToString("000");
                Expand[Expand.Length - 1].ID = Descs.data.Length;
                Expand[Expand.Length - 1].Lines = new string[] { "New Description Entry" };
                Descs.data = Expand;


                writeToMsgText(1);
        
                    txtDescID.Text = Descs.data[Descs.data.Length - 1].ID.ToString();
                
            }
        }

        private void descriptionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //remove msg desc
            if (Items.Length > 211 && DescsLoaded)
            {
                msgData[] reduce = new msgData[Descs.data.Length - 1];
                Array.Copy(Descs.data, reduce, Descs.data.Length - 1);
                Descs.data = reduce;
            }
        }

        //Demon: Old .zss export code. unused so commented out.
        // private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        // {
        //
        //     SaveFileDialog saveFileDialog1 = new SaveFileDialog();
        //     saveFileDialog1.Filter = "Super Soul Share File | *.zss";
        //     saveFileDialog1.Title = "Save a Super Soul File";
        //     saveFileDialog1.ShowDialog();
        //
        //     if (saveFileDialog1.FileName != "")
        //     {
        //
        //         //export ZSS
        //         List<byte> zssfile = new List<byte>();
        //         zssfile.AddRange(new byte[] { 0x23, 0x5A, 0x53, 0x53 });
        //         if (NamesLoaded)
        //             zssfile.AddRange(BitConverter.GetBytes(txtMsgName.TextLength));
        //         else
        //             zssfile.AddRange(BitConverter.GetBytes(0));
        //
        //         if (DescsLoaded)
        //             zssfile.AddRange(BitConverter.GetBytes(txtMsgDesc.TextLength));
        //         else
        //             zssfile.AddRange(BitConverter.GetBytes(0));
        //
        //         if (NamesLoaded)
        //             zssfile.AddRange(CharByteArray(txtMsgName.Text));
        //
        //         if (DescsLoaded)
        //             zssfile.AddRange(CharByteArray(txtMsgDesc.Text));
        //
        //         byte[] itempass = new byte[718];
        //         Array.Copy(Items[itemList.SelectedIndex].Data, 2, itempass, 0, 718);
        //         zssfile.AddRange(itempass);
        //
        //         //FileStream fs = new FileStream(txtMsgName.Text + ".zss", FileMode.Create);
        //         FileStream fs = (FileStream)saveFileDialog1.OpenFile();
        //         fs.Write(zssfile.ToArray(), 0, zssfile.Count);
        //         fs.Close();
        //     }
        //
        // }

        //UNLEASHED: Mugen's code to convert string into byte array
        //UNLEASHED: made public to use within export form. yes i know i could copy it to the export form class and made
        //it its own method, but it doesn't matter much.

        public byte[] CharByteArray(string text)
        {
            //char[] chrArray = text.ToCharArray();
            //List<byte> bytelist = new List<byte>();
            //for (int i = 0; i < chrArray.Length; i++)
            //{
            //    bytelist.AddRange(BitConverter.GetBytes(chrArray[i]));
            //}
            //return bytelist.ToArray();


            //UNLEASHED: can be done in 1 line using .NET method
            return System.Text.Encoding.Unicode.GetBytes(text.ToCharArray());
        }

        //Demon: old zss import code
        //private void importToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    //import
        //    OpenFileDialog browseFile = new OpenFileDialog();
        //    browseFile.Filter = "Z Soul Share File (*.zss)|*.zss";
        //    browseFile.Title = "Browse for Z Soul Share File";
        //    if (browseFile.ShowDialog() == DialogResult.Cancel)
        //        return;
        //    loadzss(browseFile.FileName, itemList.SelectedIndex, 0 , 0, false);
        //    UpdateData();
        //}

        //private void loadzss(string pFileName, int oItem, short nID, short dID, bool useID)
        //{
        //    byte[] zssfile = File.ReadAllBytes(pFileName);
        //    int nameCount = BitConverter.ToInt32(zssfile, 4);
        //    int DescCount = BitConverter.ToInt32(zssfile, 8);
        //
        //    Array.Copy(zssfile, 12 + (nameCount * 2) + (DescCount * 2), Items[oItem].Data, 2, 718);
        //
        //    byte[] pass;
        //    if (nameCount > 0)
        //    {
        //        pass = new byte[nameCount * 2];
        //        Array.Copy(zssfile, 12, pass, 0, nameCount * 2);
        //        txtMsgName.Text = BytetoString(pass);
        //    }
        //
        //    if (DescCount > 0)
        //    {
        //        pass = new byte[DescCount * 2];
        //        Array.Copy(zssfile, 12 + (nameCount * 2), pass, 0, DescCount * 2);
        //        txtMsgDesc.Text = BytetoString(pass);
        //    }
        //    //UpdateData();
        //}

        private string BytetoString(byte[] bytes)
        //UNLEASHED: Mugen's code to convert MSG strings from unicode to ASCII format
        //UNLEASHED: we could have use the .NET natvie System.Text methods. but this works..
        {
            char[] chrArray = new char[bytes.Length / 2];
            for (int i = 0; i < bytes.Length / 2; i++)
                chrArray[i] = BitConverter.ToChar(bytes, i * 2);

            return new string(chrArray);
           
        }

        private void replaceImportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //import/replace
            OpenFileDialog browseFile = new OpenFileDialog();
            browseFile.Filter = "Super Soul File (*.zss)|*.zss";
            browseFile.Title = "Browse for Z Soul Share File";
            if (browseFile.ShowDialog() == DialogResult.Cancel)
                return;

            byte[] zssfile = File.ReadAllBytes(browseFile.FileName);
            int nameCount = BitConverter.ToInt32(zssfile, 4);
            int DescCount = BitConverter.ToInt32(zssfile, 8);

            short nameID = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 4);
            short DescID = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 6);
            Array.Copy(zssfile, 12 + (nameCount * 2) + (DescCount * 2), Items[itemList.SelectedIndex].Data, 2, 718);

            Array.Copy(BitConverter.GetBytes(nameID), 0, Items[itemList.SelectedIndex].Data, 4, 2);
            Array.Copy(BitConverter.GetBytes(DescID), 0, Items[itemList.SelectedIndex].Data, 6, 2);

            byte[] pass;
            if (nameCount > 0)
            {
                pass = new byte[nameCount * 2];
                Array.Copy(zssfile, 12, pass, 0, nameCount * 2);
                txtMsgName.Text = BytetoString(pass);
            }

            if (DescCount > 0)
            {
                pass = new byte[DescCount * 2];
                Array.Copy(zssfile, 12 + (nameCount * 2), pass, 0, DescCount * 2);
                txtMsgDesc.Text = BytetoString(pass);
            }

            UpdateData();
        }

        private void exportToolStripMenuItem1_Click(object sender, EventArgs e)
        {

            //SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            //saveFileDialog1.Filter = "Super Soul Share File | *.zss";
            //saveFileDialog1.Title = "Save a Super Soul File";
            //saveFileDialog1.ShowDialog();

            //if (saveFileDialog1.FileName != "")
            
            //UNLEASHED: SSP export process

                //export ZSS
                //List<byte> SSFFile = new List<byte>();
                //zssfile.AddRange(new byte[] { 0x23, 0x5A, 0x53, 0x53 });
                //if (NamesLoaded)
                //    zssfile.AddRange(BitConverter.GetBytes(txtMsgName.TextLength));
                //else
                //    zssfile.AddRange(BitConverter.GetBytes(0));

       
                //if (DescsLoaded)
                //    zssfile.AddRange(BitConverter.GetBytes(txtMsgDesc.TextLength));
                //else
                //    zssfile.AddRange(BitConverter.GetBytes(0));

                //if (NamesLoaded)
                //    zssfile.AddRange(CharByteArray(txtMsgName.Text));

                //if (DescsLoaded)
                //    zssfile.AddRange(CharByteArray(txtMsgDesc.Text));

                //byte[] itempass = new byte[718];
                //Array.Copy(Items[itemList.SelectedIndex].Data, 2, itempass, 0, 718);
                //zssfile.AddRange(itempass);

                ////FileStream fs = new FileStream(txtMsgName.Text + ".zss", FileMode.Create);
                //FileStream fs = (FileStream)saveFileDialog1.OpenFile();
                //fs.Write(zssfile.ToArray(), 0, zssfile.Count);
                //fs.Close();
            (new Export(this,Names,Descs,Burst,BurstBTLHUD,BurstPause)).Show();
            

        }
        private int AddLB(byte[] SSData)
           
        {

           //UNLEASHED: this function will return the ItemList index of the latest installed Super Soul
           //if the returned int is "-1" then Super Soul failed to install.


            //Create and add Blank Super Soul (its not actually blank, it uses Raditz Super Soul as a base, which is a functional soul that has no effects)
            //loading
            // OpenFileDialog browseFile = new OpenFileDialog();
            // browseFile.Filter = "Super Soul Share File | *.zss";
            // browseFile.Title = "Select the Super Soul you want to import.";
            // if (browseFile.ShowDialog() == DialogResult.Cancel)
            //     return;

            idbItem[] items_org = Items;
            byte[] blankzss = SSData;

            int nameCount = BitConverter.ToInt32(blankzss, 4);
            int DescCount = BitConverter.ToInt32(blankzss, 8);
            int LBDescCount = BitConverter.ToInt32(blankzss, 12);
            int LBDescCountBtl = BitConverter.ToInt32(blankzss, 16);
            int LBDescCountPause = BitConverter.ToInt32(blankzss, 20);



            //UNLEASHED: we are gonna skip expanding itemlist until later..

            //==================================EXPAND ITEMS CODE=========================
            ////expand the item array
            //idbItem[] Expand = new idbItem[Items.Length + 1];
            ////copy the current items to the expanded array
            //Array.Copy(Items, Expand, Items.Length);
            ////add blank IDB data
            //Expand[Expand.Length - 1].Data = new byte[720];


            //Items = Expand;

            //==================================EXPAND ITEMS CODE=========================

            //UNLEASHED: first, lets the get the ID of the last SS's ID and increment by 1
            ushort ID = BitConverter.ToUInt16(Items[Items.Length - 1].Data, 0);
            ID++;



            bool foundProperID = true;
            int newPos = Items.Length; //UNLEASHED: Length = current items count + 1 (which is a  proper ID after we expand the list)
            //UNLEASHED: after incrementing by 1, we check if its above 32700 (very close to Int16.MaxValue)
            if (ID > 32700)
            {
                foundProperID = false;
                int currentItemIndex = Items.Length - 1;
                while ((currentItemIndex - 1) > 0)
                {

                    currentItemIndex--; //UNLEASHED: skiping last SS

                    
                    ushort currID = BitConverter.ToUInt16(Items[currentItemIndex].Data, 0);
                    ushort nextID = BitConverter.ToUInt16(Items[currentItemIndex + 1].Data, 0);
                    if (currID + 1 < nextID && ((currID + 1) <= 32700)) // our new ID can go in the middle
                    {
                        foundProperID = true;
                        newPos = currentItemIndex + 1;
                        ID = (ushort)(currID + 1);
                        break;
                    }
                }
            }

            if (foundProperID)
            {
                //expand the item array
                idbItem[] Expand = new idbItem[Items.Length + 1];
                //copy the current items to the expanded array
                Array.Copy(Items, Expand, Items.Length);
                //add blank IDB data
                Expand[Expand.Length - 1].Data = new byte[720];
                //UNLEASHED: finally, set the new array with proper IDs
                Items = Expand;

                int currentIndex = Items.Length - 1;
                int prevIndex = Items.Length - 2;
                if (prevIndex < 0) //UNLEASHED: incase something went very wrong (corrupt IDB file?)
                {
                    MessageBox.Show("Cannot add new Super Soul");
                    Items = items_org;
                    return -1;
                }

                //UNLEASHED: Swap items until we reach newPos
                while (currentIndex != newPos)
                {
                   
                        idbItem tempIDBItem = Items[currentIndex];
                        Items[currentIndex] = Items[prevIndex];
                        Items[prevIndex] = tempIDBItem;
                        currentIndex--;
                        prevIndex--;
                    

               

                }


            }
            else
            {
                MessageBox.Show("Cannot add new Super Soul");
                Items = items_org;
                return -1;
            }
            Array.Copy(BitConverter.GetBytes(ID), Items[newPos].Data, 2);

            //apply Zss data to added z-soul

            //UNLEASHED: original code was multiplying lengths by 2 (this is because of unicode names)
            //instead, when exporting the SS get the length of the strings and multiy them by 2 before writing to binary
            //so here we read the number normally
            //Array.Copy(blankzss, 12 + (nameCount * 2) + (DescCount * 2), Items[newPos].Data, 2, 718);

            //UNLEASHED: + (4) is for limit burst linker 4 bytes, only useful in SSP

            Array.Copy(blankzss, 0x1C + (nameCount) + (DescCount) + (LBDescCount) + (LBDescCountBtl) + (LBDescCountPause), Items[newPos].Data, 2, 718);
            Items[newPos].msgIndexName = 0;

            Items[newPos].msgIndexDesc = 0;

            Items[newPos].msgIndexBurst = 0;



            Items[newPos].msgIndexBurstBTL = getLB_BTL_Pause_DescID(BurstBTLHUD, Burst.data[Items[newPos].msgIndexBurst].NameID);
            Items[newPos].msgIndexBurstPause = getLB_BTL_Pause_DescID(BurstPause, Burst.data[Items[newPos].msgIndexBurst].NameID);
     
            return newPos;
        }

        private void createNewSoulToolStripMenuItem_Click (object sender, EventArgs e)
        {
            //loadzss(browseFile.FileName, Items.Length - 1);
            //itemList.SelectedIndex = itemList.Items.Count - 1;
            int index = AddSS(Properties.Resources.newss);

            if (index < 0)
                return;

            itemList.Items.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                if (NamesLoaded)
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() +  " - " + Names.data[Items[i].msgIndexName].Lines[0]);
                else
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " / " + String.Format("{0:X}", BitConverter.ToUInt16(Items[i].Data, 0)));
            }
            itemList.SelectedIndex = index;
        }

        //UNLEASHED: decided to wrap this in 1 function so that we don't need to repeat same code for creating new SSF and importing SSP

        private int AddSS(byte[] SSData)
        {

           //UNLEASHED: this function will return the ItemList index of the latest installed Super Soul
           //if the returned int is "-1" then Super Soul failed to install.


            //Create and add Blank Super Soul (its not actually blank, it uses Raditz Super Soul as a base, which is a functional soul that has no effects)
            //loading
            // OpenFileDialog browseFile = new OpenFileDialog();
            // browseFile.Filter = "Super Soul Share File | *.zss";
            // browseFile.Title = "Select the Super Soul you want to import.";
            // if (browseFile.ShowDialog() == DialogResult.Cancel)
            //     return;

            idbItem[] items_org = Items;
            byte[] blankzss = SSData;

            int nameCount = BitConverter.ToInt32(blankzss, 4);
            int DescCount = BitConverter.ToInt32(blankzss, 8);
            int LBDescCount = BitConverter.ToInt32(blankzss, 12);
            int LBDescCountBtl = BitConverter.ToInt32(blankzss, 16);
            int LBDescCountPause = BitConverter.ToInt32(blankzss, 20);



            //UNLEASHED: we are gonna skip expanding itemlist until later..

            //==================================EXPAND ITEMS CODE=========================
            ////expand the item array
            //idbItem[] Expand = new idbItem[Items.Length + 1];
            ////copy the current items to the expanded array
            //Array.Copy(Items, Expand, Items.Length);
            ////add blank IDB data
            //Expand[Expand.Length - 1].Data = new byte[720];


            //Items = Expand;

            //==================================EXPAND ITEMS CODE=========================

            //UNLEASHED: first, lets the get the ID of the last SS's ID and increment by 1
            ushort ID = BitConverter.ToUInt16(Items[Items.Length - 1].Data, 0);
            ID++;



            bool foundProperID = true;
            int newPos = Items.Length; //UNLEASHED: Length = current items count + 1 (which is a  proper ID after we expand the list)
            //UNLEASHED: after incrementing by 1, we check if its above 32700 (very close to Int16.MaxValue)
            if (ID > 32700)
            {
                foundProperID = false;
                int currentItemIndex = Items.Length - 1;
                while ((currentItemIndex - 1) > 0)
                {

                    currentItemIndex--; //UNLEASHED: skiping last SS

                    
                    ushort currID = BitConverter.ToUInt16(Items[currentItemIndex].Data, 0);
                    ushort nextID = BitConverter.ToUInt16(Items[currentItemIndex + 1].Data, 0);
                    if (currID + 1 < nextID && ((currID + 1) <= 32700)) // our new ID can go in the middle
                    {
                        foundProperID = true;
                        newPos = currentItemIndex + 1;
                        ID = (ushort)(currID + 1);
                        break;
                    }
                }
            }

            if (foundProperID)
            {
                //expand the item array
                idbItem[] Expand = new idbItem[Items.Length + 1];
                //copy the current items to the expanded array
                Array.Copy(Items, Expand, Items.Length);
                //add blank IDB data
                Expand[Expand.Length - 1].Data = new byte[720];
                //UNLEASHED: finally, set the new array with proper IDs
                Items = Expand;

                int currentIndex = Items.Length - 1;
                int prevIndex = Items.Length - 2;
                if (prevIndex < 0) //UNLEASHED: incase something went very wrong (corrupt IDB file?)
                {
                    MessageBox.Show("Cannot add new Super Soul");
                    Items = items_org;
                    return -1;
                }

                //UNLEASHED: Swap items until we reach newPos
                while (currentIndex != newPos)
                {
                   
                        idbItem tempIDBItem = Items[currentIndex];
                        Items[currentIndex] = Items[prevIndex];
                        Items[prevIndex] = tempIDBItem;
                        currentIndex--;
                        prevIndex--;
                    

               

                }


            }
            else
            {
                MessageBox.Show("Cannot add new Super Soul");
                Items = items_org;
                return -1;
            }
            Array.Copy(BitConverter.GetBytes(ID), Items[newPos].Data, 2);

            //apply Zss data to added z-soul

            //UNLEASHED: original code was multiplying lengths by 2 (this is because of unicode names)
            //instead, when exporting the SS get the length of the strings and multiy them by 2 before writing to binary
            //so here we read the number normally
            //Array.Copy(blankzss, 12 + (nameCount * 2) + (DescCount * 2), Items[newPos].Data, 2, 718);

            //UNLEASHED: + (4) is for limit burst linker 4 bytes, only useful in SSP

            Array.Copy(blankzss, 0x1C + (nameCount) + (DescCount) + (LBDescCount) + (LBDescCountBtl) + (LBDescCountPause), Items[newPos].Data, 2, 718);

            //expand Names msg
            //UNLEASHED we shouldn't worry about Msg IDs.. i think......
            byte[] pass;
            if (NamesLoaded)
            {
                msgData[] Expand2 = new msgData[Names.data.Length + 1];
                Array.Copy(Names.data, Expand2, Names.data.Length);
                //UNLEASHED:i'm guessing MSG IDs are zero based so calling length is like IDs + 1
                Expand2[Expand2.Length - 1].NameID = "talisman_" + Names.data.Length.ToString("000");
                Expand2[Expand2.Length - 1].ID = Names.data.Length;
                if (nameCount > 0)
                {
                    pass = new byte[nameCount];
                    Array.Copy(blankzss, 0x1C, pass, 0, nameCount);
                    Expand2[Expand2.Length - 1].Lines = new string[] { BytetoString(pass) };
                }
                else
                    Expand2[Expand2.Length - 1].Lines = new string[] { "New Name Entry" };

                byte[] newMSGNameEntryIDBytes = BitConverter.GetBytes((short)Expand2[Expand2.Length - 1].ID);
                Array.Copy(newMSGNameEntryIDBytes, 0, Items[newPos].Data, 4, 2);
                Names.data = Expand2;
                //UNLEASHED: using FindmsgIndex again is pointless, we already have the MSG ID
                //Items[newPos].msgIndexName = FindmsgIndex(ref Names, Names.data[Names.data.Length - 1].ID);
                Items[newPos].msgIndexName = BitConverter.ToInt16(newMSGNameEntryIDBytes, 0);

            }
            writeToMsgText(0);
            //expand description msg
            if (DescsLoaded)
            {

                msgData[] Expand3 = new msgData[Descs.data.Length + 1];
                Array.Copy(Descs.data, Expand3, Descs.data.Length);
                Expand3[Expand3.Length - 1].NameID = "talisman_eff_" + Descs.data.Length.ToString("000");
                Expand3[Expand3.Length - 1].ID = Descs.data.Length;
                if (DescCount > 0)
                {
                    pass = new byte[DescCount];
                    Array.Copy(blankzss, 0x1C + (nameCount), pass, 0, DescCount);
                    Expand3[Expand3.Length - 1].Lines = new string[] { BytetoString(pass) };
                }
                else
                    Expand3[Expand3.Length - 1].Lines = new string[] { "New Description Entry" };

                byte[] newMSGDescEntryIDBytes = BitConverter.GetBytes((short)Expand3[Expand3.Length - 1].ID);
                Array.Copy(newMSGDescEntryIDBytes, 0, Items[newPos].Data, 6, 2);
                Descs.data = Expand3;

                //UNLEASHED: using FindmsgIndex again is pointless, we already have the MSG ID
                //Items[newPos].msgIndexDesc = FindmsgIndex(ref Descs, Descs.data[Descs.data.Length - 1].ID);
                Items[newPos].msgIndexDesc = BitConverter.ToInt16(newMSGDescEntryIDBytes, 0);

            }

            writeToMsgText(1);
            //UNLEASHED: expand LB Desc / LB DescBTL MSG
            if (BurstLoaded)
            {

                msgData[] Expand4 = new msgData[Burst.data.Length + 1];
                Array.Copy(Burst.data, Expand4, Burst.data.Length);
                Expand4[Expand4.Length - 1].NameID = "talisman_olt_" + Burst.data.Length.ToString("000");
                Expand4[Expand4.Length - 1].ID = Burst.data.Length;
                if (LBDescCount > 0)
                {
                    pass = new byte[LBDescCount];
                    Array.Copy(blankzss, 0x1C + (nameCount) + (DescCount), pass, 0, LBDescCount);
                    Expand4[Expand4.Length - 1].Lines = new string[] { BytetoString(pass) };
                }
                else
                    Expand4[Expand4.Length - 1].Lines = new string[] { "New LB Desc Entry" };

                byte[] newMSGLBDescEntryIDBytes = BitConverter.GetBytes((short)Expand4[Expand4.Length - 1].ID);
                Array.Copy(newMSGLBDescEntryIDBytes, 0, Items[newPos].Data, 40, 2);
                Burst.data = Expand4;

                //UNLEASHED: using FindmsgIndex again is pointless, we already have the MSG ID
                //Items[newPos].msgIndexDesc = FindmsgIndex(ref Descs, Descs.data[Descs.data.Length - 1].ID);
                Items[newPos].msgIndexBurst = BitConverter.ToInt16(newMSGLBDescEntryIDBytes, 0);

          




                msgData[] Expand5 = new msgData[BurstBTLHUD.data.Length + 1];
                Array.Copy(BurstBTLHUD.data, Expand5, BurstBTLHUD.data.Length);
                Expand5[Expand5.Length - 1].NameID = "BHD_OLT_000_" + Items[newPos].msgIndexBurst.ToString();// +BurstBTLHUD.data.Length.ToString("000");
                Expand5[Expand5.Length - 1].ID = BurstBTLHUD.data.Length;
                if (LBDescCountBtl > 0)
                {
                    pass = new byte[LBDescCountBtl];
                    Array.Copy(blankzss, 0x1C + (nameCount) + (DescCount) + (LBDescCount), pass, 0, LBDescCountBtl);
                    Expand5[Expand5.Length - 1].Lines = new string[] { BytetoString(pass) };
                }
                else
                    Expand5[Expand5.Length - 1].Lines = new string[] { "New LB Battle Desc Entry" };

                byte[] newMSGLBDescBtlEntryIDBytes = BitConverter.GetBytes((short)Expand5[Expand5.Length - 1].ID);
                //UNLEASHED: the LBDescBtl MSG ID doesn't actually exist in the skill, so no need to copy
                //Array.Copy(newMSGLBDescEntryIDBytes, 0, Items[newPos].Data, 40, 2);
                BurstBTLHUD.data = Expand5;

                //UNLEASHED: using FindmsgIndex again is pointless, we already have the MSG ID
                //Items[newPos].msgIndexDesc = FindmsgIndex(ref Descs, Descs.data[Descs.data.Length - 1].ID);
                Items[newPos].msgIndexBurstBTL = BitConverter.ToInt16(newMSGLBDescBtlEntryIDBytes, 0);




              




                msgData[] Expand6 = new msgData[BurstPause.data.Length + 1];
                Array.Copy(BurstPause.data, Expand6, BurstPause.data.Length);
                Expand6[Expand6.Length - 1].NameID = "BHD_OLT_000_" + Items[newPos].msgIndexBurst.ToString();// +BurstBTLHUD.data.Length.ToString("000");
                Expand6[Expand6.Length - 1].ID = BurstPause.data.Length;
                if (LBDescCountPause > 0)
                {
                    pass = new byte[LBDescCountPause];
                    Array.Copy(blankzss, 0x1C + (nameCount) + (DescCount) + (LBDescCount) + (LBDescCountBtl), pass, 0, LBDescCountPause);
                    Expand6[Expand6.Length - 1].Lines = new string[] { BytetoString(pass) };
                }
                else
                    Expand6[Expand6.Length - 1].Lines = new string[] { "New LB Pause Desc Entry" };

                byte[] newMSGLBDescPauseEntryIDBytes = BitConverter.GetBytes((short)Expand6[Expand6.Length - 1].ID);
                //UNLEASHED: the LBDescBtl MSG ID doesn't actually exist in the skill, so no need to copy
                //Array.Copy(newMSGLBDescEntryIDBytes, 0, Items[newPos].Data, 40, 2);
                BurstPause.data = Expand6;

                //UNLEASHED: using FindmsgIndex again is pointless, we already have the MSG ID
                //Items[newPos].msgIndexDesc = FindmsgIndex(ref Descs, Descs.data[Descs.data.Length - 1].ID);
                Items[newPos].msgIndexBurstPause = BitConverter.ToInt16(newMSGLBDescPauseEntryIDBytes, 0);


                writeToMsgText(2);
            }
            return newPos;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(Forms.Settings form = new Forms.Settings(settings))
            {
                form.ShowDialog();
                if (form.Finished)
                {
                    settings = form.settings;
                    settings.Save();

                    MessageBox.Show(this, "Please restart the application for the changes to take affect.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("XV2 Super Soul Editor Version 1.60\n\nCredits:\nDemonBoy - Tool Creator\nLazybone & Unleashed - Help with fixes/additions\nMugenAttack - Original Source code");
           
         
        }
        //UNLEASHED: added this function to search for SS name
        private void srchBtn_Click(object sender, EventArgs e)
        {
            bool found = false;
            StringComparison sc = (caseSensetiveToolStripMenuItem.Checked == true) ? 
                StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
           // note that "lastInputString" and "lastInputIndex" are global vars.
            if (srchBox.TextLength > 0)
            {
                // if the user actually input a different string 
                // then we need to start from beginning
                if (lastInputString != srchBox.Text)
                {
                    lastInputString = srchBox.Text;
                    lastFoundIndex = 0;
                }
                //start loop from last found index
                //no need to go from start if we already got a match before
                for (int i = lastFoundIndex; i < itemList.Items.Count; i++)
                {
                    // we want to have an incremental search
                    // keep looking for the next item that matches the search
                    if (i > lastFoundIndex)
                    {

                        if (itemList.Items[i].ToString().IndexOf(srchBox.Text, sc) >= 0)
                        {
  
                       
                            lastFoundIndex = i;
                            //set the selection to the found super soul
                            itemList.SelectedIndex = i;
                            found = true;
                            break;
                         }

                    }

                }
                // super soul was never found, reset from begining
                if (found == false)
                {
                    if (lastFoundIndex > 0)
                        MessageBox.Show("couldn't find more super souls ");
                    else
                    MessageBox.Show("super soul not found.. ");
                    lastFoundIndex = 0;
                    
                }
            }
        }

        private void txtMsgLBDescBTL_TextChanged(object sender, EventArgs e)
        {
            if (BurstLoaded && !lockMod)
            {
                BurstBTLHUD.data[Items[itemList.SelectedIndex].msgIndexBurstBTL].Lines[0] = txtMsgLBDescBTL.Text;
                BurstPause.data[Items[itemList.SelectedIndex].msgIndexBurstPause].Lines[0] = txtMsgLBDescBTL.Text;
            }
             
        }

        private void setAsDefaultProgramForSSFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileAssociations.SetAssoc(".ssf", "XV2SS_EDITOR_FILE", "SSF File");
            MessageBox.Show("Extension Set Successfully");
        }

        private void setAsDefaultProgramForSSFPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileAssociations.SetAssoc(".ssp", "XV2SS_EDITOR_FILE", "SSP File");
            MessageBox.Show("Extension Set Successfully");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void setCurrentBurstIDsToNULLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtLBAura.Text = "-1";
            txtLBDesc.Text = "0";
            txtLBSoulID1.Text = "65535";
            txtLBSoulID2.Text = "65535";
            txtLBSoulID3.Text = "65535";
            cbLBColor.SelectedIndex = 0;
        }

        private void setCurrentBurstToATKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtLBAura.Text = "250";
            txtLBDesc.Text = "0";
            txtLBSoulID1.Text = "500";
            txtLBSoulID2.Text = "501";
            txtLBSoulID3.Text = "502";
            cbLBColor.SelectedIndex = 1;
        }

        private void setCurrentBurstToDEFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtLBAura.Text = "251";
            txtLBDesc.Text = "1";
            txtLBSoulID1.Text = "520";
            txtLBSoulID2.Text = "521";
            txtLBSoulID3.Text = "522";
            cbLBColor.SelectedIndex = 2;
        }

        private void setCurrentBurstToRECToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtLBAura.Text = "252";
            txtLBDesc.Text = "2";
            txtLBSoulID1.Text = "540";
            txtLBSoulID2.Text = "541";
            txtLBSoulID3.Text = "542";
            cbLBColor.SelectedIndex = 3;
        }

        private void setCurrentBurstToGRDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtLBAura.Text = "253";
            txtLBDesc.Text = "3";
            txtLBSoulID1.Text = "560";
            txtLBSoulID2.Text = "561";
            txtLBSoulID3.Text = "562";
            cbLBColor.SelectedIndex = 4;
        }

        private void setCurrentBurstToREVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtLBAura.Text = "253";
            txtLBDesc.Text = "4";
            txtLBSoulID1.Text = "580";
            txtLBSoulID2.Text = "581";
            txtLBSoulID3.Text = "582";
            cbLBColor.SelectedIndex = 4;
        }

        private void removeCurrentSuperSoulFromShopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtRace.Text = "0";
            txtShopTest.Text = "255";
            txtTPTest.Text = "32767";
            txtBuy.Text = "0";
            txtSell.Text = "0";
            txtBuyTP.Text = "0";
            cbStar.SelectedIndex = 4;
        }

        private void store_defaultBtn_Click(object sender, EventArgs e)
        {
            txtRace.Text = "255";
            txtShopTest.Text = "-1";
            txtTPTest.Text = "30";
            txtBuy.Text = "10";
            txtSell.Text = "100";
            txtBuyTP.Text = "1";
            cbStar.SelectedIndex = 4;
        }


        private void debugLBSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (debugLBSelect.SelectedIndex == 0)
            {
                txtLBAura.Text = "250";
                txtLBDesc.Text = "0";
                txtLBSoulID1.Text = "500";
                txtLBSoulID2.Text = "501";
                txtLBSoulID3.Text = "502";
                cbLBColor.SelectedIndex = 1;
            }

            if (debugLBSelect.SelectedIndex == 1)
            {
                txtLBAura.Text = "251";
                txtLBDesc.Text = "1";
                txtLBSoulID1.Text = "520";
                txtLBSoulID2.Text = "521";
                txtLBSoulID3.Text = "522";
                cbLBColor.SelectedIndex = 2;
            }

            if (debugLBSelect.SelectedIndex == 2)
            {
                txtLBAura.Text = "252";
                txtLBDesc.Text = "2";
                txtLBSoulID1.Text = "540";
                txtLBSoulID2.Text = "541";
                txtLBSoulID3.Text = "542";
                cbLBColor.SelectedIndex = 3;
            }

            if (debugLBSelect.SelectedIndex == 3)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "3";
                txtLBSoulID1.Text = "560";
                txtLBSoulID2.Text = "561";
                txtLBSoulID3.Text = "562";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 4)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "4";
                txtLBSoulID1.Text = "580";
                txtLBSoulID2.Text = "581";
                txtLBSoulID3.Text = "582";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 5)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "5";
                txtLBSoulID1.Text = "600";
                txtLBSoulID2.Text = "601";
                txtLBSoulID3.Text = "602";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 6)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "6";
                txtLBSoulID1.Text = "605";
                txtLBSoulID2.Text = "606";
                txtLBSoulID3.Text = "607";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 7)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "7";
                txtLBSoulID1.Text = "610";
                txtLBSoulID2.Text = "611";
                txtLBSoulID3.Text = "612";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 8)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "8";
                txtLBSoulID1.Text = "615";
                txtLBSoulID2.Text = "616";
                txtLBSoulID3.Text = "617";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 9)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "9";
                txtLBSoulID1.Text = "620";
                txtLBSoulID2.Text = "621";
                txtLBSoulID3.Text = "622";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 10)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "10";
                txtLBSoulID1.Text = "625";
                txtLBSoulID2.Text = "626";
                txtLBSoulID3.Text = "627";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 11)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "11";
                txtLBSoulID1.Text = "630";
                txtLBSoulID2.Text = "631";
                txtLBSoulID3.Text = "632";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 12)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "12";
                txtLBSoulID1.Text = "635";
                txtLBSoulID2.Text = "636";
                txtLBSoulID3.Text = "637";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 13)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "13";
                txtLBSoulID1.Text = "640";
                txtLBSoulID2.Text = "641";
                txtLBSoulID3.Text = "642";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 14)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "14";
                txtLBSoulID1.Text = "645";
                txtLBSoulID2.Text = "646";
                txtLBSoulID3.Text = "647";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 15)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "15";
                txtLBSoulID1.Text = "648";
                txtLBSoulID2.Text = "649";
                txtLBSoulID3.Text = "650";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 16)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "16";
                txtLBSoulID1.Text = "651";
                txtLBSoulID2.Text = "652";
                txtLBSoulID3.Text = "653";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 17)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "17";
                txtLBSoulID1.Text = "654";
                txtLBSoulID2.Text = "655";
                txtLBSoulID3.Text = "656";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 18)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "18";
                txtLBSoulID1.Text = "657";
                txtLBSoulID2.Text = "658";
                txtLBSoulID3.Text = "659";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 19)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "19";
                txtLBSoulID1.Text = "660";
                txtLBSoulID2.Text = "661";
                txtLBSoulID3.Text = "662";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 20)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "20";
                txtLBSoulID1.Text = "663";
                txtLBSoulID2.Text = "664";
                txtLBSoulID3.Text = "665";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 21)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "21";
                txtLBSoulID1.Text = "666";
                txtLBSoulID2.Text = "667";
                txtLBSoulID3.Text = "668";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 22)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "22";
                txtLBSoulID1.Text = "669";
                txtLBSoulID2.Text = "670";
                txtLBSoulID3.Text = "671";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 23)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "23";
                txtLBSoulID1.Text = "672";
                txtLBSoulID2.Text = "673";
                txtLBSoulID3.Text = "674";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 24)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "24";
                txtLBSoulID1.Text = "675";
                txtLBSoulID2.Text = "676";
                txtLBSoulID3.Text = "677";
                cbLBColor.SelectedIndex = 4;
            }

            if (debugLBSelect.SelectedIndex == 25)
            {
                txtLBAura.Text = "253";
                txtLBDesc.Text = "25";
                txtLBSoulID1.Text = "678";
                txtLBSoulID2.Text = "679";
                txtLBSoulID3.Text = "680";
                cbLBColor.SelectedIndex = 4;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (hasSavedChanges == false)
            {
                if (MessageBox.Show("You have unsaved data, close editor?", "Unsaved Data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    e.Cancel = true;
                else
                    e.Cancel = false;
            }
            else
                Application.Exit();
        }

        private void copyCurrentSuperSoulToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //System.Text.Encoding.Unicode.GetBytes(text.ToCharArray());
            List<byte> copiedBytes = new List<byte>();


            string nameText = Names.data[Items[currentSuperSoulIndex].msgIndexName].Lines[0];
            string DescText = Descs.data[Items[currentSuperSoulIndex].msgIndexDesc].Lines[0];
            string LBDescText = Burst.data[Items[currentSuperSoulIndex].msgIndexBurst].Lines[0];
            string LBBTLHUDDescText = BurstBTLHUD.data[Items[currentSuperSoulIndex].msgIndexBurstBTL].Lines[0];
            string LBPauseDescText = BurstPause.data[Items[currentSuperSoulIndex].msgIndexBurstPause].Lines[0];


            int nameCount = nameText.Length * 2;
            int DescCount = DescText.Length * 2;
            int LBDescCount = LBDescText.Length * 2;
            int LBBTLHUDDescCount = LBBTLHUDDescText.Length * 2;
            int LBPauseDescCount = LBPauseDescText.Length * 2;
            
            copiedBytes.AddRange(new byte[] { 0x23, 0x53, 0x53, 0x46 });
            copiedBytes.AddRange(BitConverter.GetBytes(nameCount));
            copiedBytes.AddRange(BitConverter.GetBytes(DescCount));
            copiedBytes.AddRange(BitConverter.GetBytes(LBDescCount));
            copiedBytes.AddRange(BitConverter.GetBytes(LBBTLHUDDescCount));
            copiedBytes.AddRange(BitConverter.GetBytes(LBPauseDescCount));

            copiedBytes.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

            copiedBytes.AddRange(System.Text.Encoding.Unicode.GetBytes(nameText.ToCharArray()));
            copiedBytes.AddRange(System.Text.Encoding.Unicode.GetBytes(DescText.ToCharArray()));
            copiedBytes.AddRange(System.Text.Encoding.Unicode.GetBytes(LBDescText.ToCharArray()));
            copiedBytes.AddRange(System.Text.Encoding.Unicode.GetBytes(LBBTLHUDDescText.ToCharArray()));
            copiedBytes.AddRange(System.Text.Encoding.Unicode.GetBytes(LBPauseDescText.ToCharArray()));

            byte[] tmp = new byte[718];
            Array.Copy(Items[currentSuperSoulIndex].Data,2,tmp,0,718);
            copiedBytes.AddRange(tmp);
            clipboardData = copiedBytes.ToArray();
            MessageBox.Show("Super Soul copied successfully");



        }

        private void createNewSoulFromClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (clipboardData == null)
            {
                MessageBox.Show("Clipboard is empty.");
                return;
            }

            int index = AddSS(clipboardData);

            if (index < 0)
                return;

            itemList.Items.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                if (NamesLoaded)
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
                else
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " / " + String.Format("{0:X}", BitConverter.ToUInt16(Items[i].Data, 0)));
            }
            itemList.SelectedIndex = index;
        }

        private void addNewEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[] blankzss = Properties.Resources.newss;
      
            int nameCount = BitConverter.ToInt32(blankzss, 4);
            int DescCount = BitConverter.ToInt32(blankzss, 8);
            int LBDescCount = BitConverter.ToInt32(blankzss, 12);
            int LBDescCountBtl = BitConverter.ToInt32(blankzss, 16);
            int LBDescCountPause = BitConverter.ToInt32(blankzss, 20);
            byte[] pass;
            if (BurstLoaded)
            {

                msgData[] Expand4 = new msgData[Burst.data.Length + 1];
                Array.Copy(Burst.data, Expand4, Burst.data.Length);
                Expand4[Expand4.Length - 1].NameID = "talisman_olt_" + Burst.data.Length.ToString("000");
                Expand4[Expand4.Length - 1].ID = Burst.data.Length;
                if (LBDescCount > 0)
                {
                    pass = new byte[LBDescCount];
                    Array.Copy(blankzss, 0x1C + (nameCount) + (DescCount), pass, 0, LBDescCount);
                    Expand4[Expand4.Length - 1].Lines = new string[] { BytetoString(pass) };
                }
                else
                    Expand4[Expand4.Length - 1].Lines = new string[] { "New LB Desc Entry" };

                byte[] newMSGLBDescEntryIDBytes = BitConverter.GetBytes((short)Expand4[Expand4.Length - 1].ID);
                Array.Copy(newMSGLBDescEntryIDBytes, 0, Items[currentSuperSoulIndex].Data, 40, 2);
                Burst.data = Expand4;

           
                Items[currentSuperSoulIndex].msgIndexBurst = BitConverter.ToInt16(newMSGLBDescEntryIDBytes, 0);



         


                msgData[] Expand5 = new msgData[BurstBTLHUD.data.Length + 1];
                Array.Copy(BurstBTLHUD.data, Expand5, BurstBTLHUD.data.Length);
                Expand5[Expand5.Length - 1].NameID = "BHD_OLT_000_" + Items[currentSuperSoulIndex].msgIndexBurst.ToString();// +BurstBTLHUD.data.Length.ToString("000");
                Expand5[Expand5.Length - 1].ID = BurstBTLHUD.data.Length;
                if (LBDescCountBtl > 0)
                {
                    pass = new byte[LBDescCountBtl];
                    Array.Copy(blankzss, 0x1C + (nameCount) + (DescCount) + (LBDescCount), pass, 0, LBDescCountBtl);
                    Expand5[Expand5.Length - 1].Lines = new string[] { BytetoString(pass) };
                }
                else
                    Expand5[Expand5.Length - 1].Lines = new string[] { "New LB Battle Desc Entry" };

                byte[] newMSGLBDescBtlEntryIDBytes = BitConverter.GetBytes((short)Expand5[Expand5.Length - 1].ID);
     
                BurstBTLHUD.data = Expand5;

    
                Items[currentSuperSoulIndex].msgIndexBurstBTL = BitConverter.ToInt16(newMSGLBDescBtlEntryIDBytes, 0);



       





                msgData[] Expand6 = new msgData[BurstPause.data.Length + 1];
                Array.Copy(BurstPause.data, Expand6, BurstPause.data.Length);
                Expand6[Expand6.Length - 1].NameID = "BHD_OLT_000_" + Items[currentSuperSoulIndex].msgIndexBurst.ToString();// +BurstBTLHUD.data.Length.ToString("000");
                Expand6[Expand6.Length - 1].ID = BurstPause.data.Length;
                if (LBDescCountPause > 0)
                {
                    pass = new byte[LBDescCountPause];
                    Array.Copy(blankzss, 0x1C + (nameCount) + (DescCount) + (LBDescCount) + (LBDescCountBtl), pass, 0, LBDescCountPause);
                    Expand6[Expand6.Length - 1].Lines = new string[] { BytetoString(pass) };
                }
                else
                    Expand6[Expand6.Length - 1].Lines = new string[] { "New LB Pause Desc Entry" };

                byte[] newMSGLBDescPauseEntryIDBytes = BitConverter.GetBytes((short)Expand6[Expand6.Length - 1].ID);
             
                BurstPause.data = Expand6;

         
                Items[currentSuperSoulIndex].msgIndexBurstPause = BitConverter.ToInt16(newMSGLBDescPauseEntryIDBytes, 0);
                writeToMsgText(2);


                txtLBDesc.Text = Expand4[Expand4.Length - 1].ID.ToString();
               
            }
        }

        private void limitBurstToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void createNewSoulAsLimitBurstToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = AddLB(Properties.Resources.newss);

            if (index < 0)
                return;

            itemList.Items.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                if (NamesLoaded)
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
                else
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " / " + String.Format("{0:X}", BitConverter.ToUInt16(Items[i].Data, 0)));
            }
            itemList.SelectedIndex = index;

            txtRace.Text = "0";
            txtShopTest.Text = "255";
            txtTPTest.Text = "32767";
            txtBuy.Text = "0";
            txtSell.Text = "0";
            txtBuyTP.Text = "0";
            cbStar.SelectedIndex = 4;

            txtLBAura.Text = "-1";
            txtLBDesc.Text = "0";
            txtLBSoulID1.Text = "65535";
            txtLBSoulID2.Text = "65535";
            txtLBSoulID3.Text = "65535";
            cbLBColor.SelectedIndex = 0;
        }

        private void txtID_TextChanged(object sender, EventArgs e)
        {

        }




        //I'll Maybe work on fixing the patches at another time

        //        private void patchesToolStripMenuItem_Click(object sender, EventArgs e)
        //        {
        //
        //        }
        //
        //        private void patchAllForStoreToolStripMenuItem_Click(object sender, EventArgs e)
        //        {
        //            //patch for store
        //            for (int i = 0; i < Items.Length; i++)
        //            {
        //                int sellValue = BitConverter.ToInt32(Items[i].Data, 20);
        //                Array.Copy(BitConverter.GetBytes(sellValue * 2), 0, Items[i].Data, 16, 4);
        //            }
        //        }
        //
        //        private void noGainNoLossPatchToolStripMenuItem_Click(object sender, EventArgs e)
        //        {
        //            for (int i = 0; i < Items.Length; i++)
        //            {
        //                for (int j = 0; j < 25; j++)
        //                {
        //                    float v = BitConverter.ToSingle(Items[i].Data, 32 + (j * 4));
        //                    if (v != 1)
        //                        v = 1;
        //
        //                    Array.Copy(BitConverter.GetBytes(v), 0, Items[i].Data, 32 + (j * 4), 4);
        //
        //                }
        //            }
        //            UpdateData();
        //        }
        //
        //        private void noNegativePatchToolStripMenuItem_Click(object sender, EventArgs e)
        //        {
        //            for (int i = 0; i < Items.Length; i++)
        //            {
        //                for (int j = 0; j < 25; j++)
        //                {
        //                    float v = BitConverter.ToSingle(Items[i].Data, 32 + (j * 4));
        //                    if (v < 1)
        //                        v = 1;
        //
        //                    Array.Copy(BitConverter.GetBytes(v), 0, Items[i].Data, 32 + (j * 4), 4);
        //
        //                }
        //            }
        //            UpdateData();
        //        }
        //

    }

}