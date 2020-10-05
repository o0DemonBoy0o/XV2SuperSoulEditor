using System;
using System.Collections.Generic;
using System.Linq;
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
            "Stamina Resoration Rate","Stamina Damage to Enemy","Stamina Damage to User","Ground Speed","Air Speed","Boost Speed","Dash Speed",
            "Basic Melee Damage","Basic Ki Blast Damage","Strike Skill Damage","Ki Skill Damage","Basic Melee Damage Taken",
            "Basic Ki Blast Damage Taken","Strike Skill Damage Taken","Ki Skill Damage Taken","Transform Skill Duration",
            "Reinforcement Skill Duration","Unknown 1","HP Restored on Revive","User Revive Speed","Ally Revive Speed","Unknown 2",
            "Assist Effect 1","Assist Effect 2","Assist Effect 3","Assist Effect 4","Assist Effect 5","Assist Effect 6"};

        //DEMON: Changed the list names for better readability.
        string FileName;
        EffectList effList;
        ActivatorList actList;
        TargetList trgList;
        LBColorList lbcList;
        KitypeList kitList;
        VFXList vfxList;
        string FileNameMsgN;
        string FileNameMsgD;
        string FileNameMsgB;
        string FileNameMsgB_BTLHUD;
        string FileNameMsgB_Pause;

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

        //UNLEASHED: helper vars for searching
        private int lastFoundIndex = 0;
        private string lastInputString = "";

        //UNLEASHED: helper var for deleting
        private int currentSuperSoulIndex = -1;

        //UNLEASHED: remind user to save changes.
        private bool hasSavedChanges = true;

        //UNLEASHED: copy and paste
        private byte[] clipboardData = null;


        //UNLEASHED: Version
        string toolVersion = "1.76";
        #endregion

        public Form1()
        {
            InitializeComponent();

            this.Text = $"XV2 Super Soul Editor - {toolVersion}";

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
                    itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);

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
                using (Forms.Settings settingsForm = new Forms.Settings(settings))
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
                //DEMON: different exception message for users?
                //throw new Exception("Cannot init Xv2FileIO because the set game directory is invalid.");
                throw new Exception("DBXV2 game directory is invalid. Please reset settings and set the correct path.");
            }
        }

        private void LoadFiles()
        {
            byte[] idbfile = new byte[1];
            effList = new EffectList();
            actList = new ActivatorList();
            trgList = new TargetList();
            lbcList = new LBColorList();
            kitList = new KitypeList();
            vfxList = new VFXList();

            //Load Talisman IDB
            //Each Super Soul size: 720 bytes
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
                //SS Names
                genericMsgListNames.Add(new GenericMsgFile(String.Format("{0}/data/msg/proper_noun_talisman_name_{1}.msg", settings.GameDir, langsSuffix[i]),
                    msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_name_{0}.msg", langsSuffix[i])))));
                //SS Descriptions
                genericMsgLisDescs.Add(new GenericMsgFile(String.Format("{0}/data/msg/proper_noun_talisman_info_{1}.msg", settings.GameDir, langsSuffix[i]),
                   msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_info_{0}.msg", langsSuffix[i])))));
                //LB Desciptions (Menu)
                genericMsgListBurst.Add(new GenericMsgFile(String.Format("{0}/data/msg/proper_noun_talisman_info_olt_{1}.msg", settings.GameDir, langsSuffix[i]),
                 msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_info_olt_{0}.msg", langsSuffix[i])))));
                //LB Descriptions (Battle)
                genericMsgListNameBurstBTLHUD.Add(new GenericMsgFile(String.Format("{0}/data/msg/quest_btlhud_{1}.msg", settings.GameDir, langsSuffix[i]),
                 msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/quest_btlhud_{0}.msg", langsSuffix[i])))));
                //LB Descriptions (Battle2)
                genericMsgListNameBurstPause.Add(new GenericMsgFile(String.Format("{0}/data/msg/pause_text_{1}.msg", settings.GameDir, langsSuffix[i]),
                 msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/pause_text_{0}.msg", langsSuffix[i])))));
            }

            //load msg file for names
            FileNameMsgN = String.Format("{0}/data/msg/proper_noun_talisman_name_{1}.msg", settings.GameDir, settings.LanguagePrefix);
            Names = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_name_{0}.msg", settings.LanguagePrefix)));

            //load msg file for descriptions
            FileNameMsgD = String.Format("{0}/data/msg/proper_noun_talisman_info_{1}.msg", settings.GameDir, settings.LanguagePrefix);
            Descs = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_info_{0}.msg", settings.LanguagePrefix)));

            //load msgfile for limit burst descriptions
            //UNLEASHED: btlhud/Pause is a shared MSG for various battle text
            FileNameMsgB = String.Format("{0}/data/msg/proper_noun_talisman_info_olt_{1}.msg", settings.GameDir, settings.LanguagePrefix);
            FileNameMsgB_BTLHUD = String.Format("{0}/data/msg/quest_btlhud_{1}.msg", settings.GameDir, settings.LanguagePrefix);
            FileNameMsgB_Pause = String.Format("{0}/data/msg/pause_text_{1}.msg", settings.GameDir, settings.LanguagePrefix);
            Burst = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/proper_noun_talisman_info_olt_{0}.msg", settings.LanguagePrefix)));
            BurstBTLHUD = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/quest_btlhud_{0}.msg", settings.LanguagePrefix)));
            BurstPause = msgStream.Load(fileIO.GetFileFromGame(String.Format("msg/pause_text_{0}.msg", settings.LanguagePrefix)));


            //idb items set
            this.addNewXV2SSEditStripMenuItem.Enabled = true;
            this.msgToolStripMenuItem.Enabled = true;

            Items = new idbItem[count];
            for (int i = 0; i < Items.Length; i++)
            {
                Items[i].Data = new byte[720];
                Array.Copy(idbfile, 16 + (i * 720), Items[i].Data, 0, 720);

                Items[i].msgIndexName = FindmsgIndex(ref Names, BitConverter.ToUInt16(Items[i].Data, 4));
                Items[i].msgIndexDesc = FindmsgIndex(ref Descs, BitConverter.ToUInt16(Items[i].Data, 6));

                //UNLEASHED: Add BTL / PAUSE LB Desc
                Items[i].msgIndexBurst = FindmsgIndex(ref Burst, BitConverter.ToUInt16(Items[i].Data, 40));
                Items[i].msgIndexBurstBTL = getLB_BTL_Pause_DescID(BurstBTLHUD, Burst.data[Items[i].msgIndexBurst].NameID);
                Items[i].msgIndexBurstPause = getLB_BTL_Pause_DescID(BurstPause, Burst.data[Items[i].msgIndexBurst].NameID);
            }

            itemList.Items.Clear();

            for (int i = 0; i < count; i++)
                itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
            EffectData();
            itemList.SelectedIndex = 0;
        }

        //UNLEASHED: added this function to retrieve LB_BTL / LB_PAUSE desc, as the ID for it doesn't exist in SS data 
        private int getLB_BTL_Pause_DescID(msg extraBurstMsgFile, string BurstEntryName)
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
        void writeToMsgText(int MsgType, string Msg, int OLT_ID = -1)
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
                            Expand2[Expand2.Length - 1].Lines = new string[] { Msg };
                            tmp.msgFile_m.data = Expand2;
                            genericMsgListNames[i] = tmp;
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
                            Expand[Expand.Length - 1].Lines = new string[] { Msg };
                            tmp.msgFile_m.data = Expand;
                            genericMsgLisDescs[i] = tmp;
                        }
                        break;
                    }

                case 2:
                    {
                        for (int i = 0; i < genericMsgListBurst.Count; i++)
                        {
                            GenericMsgFile tmp = genericMsgListBurst[i];
                            msgData[] Expand4 = new msgData[tmp.msgFile_m.data.Length + 1];
                            Array.Copy(tmp.msgFile_m.data, Expand4, tmp.msgFile_m.data.Length);
                            Expand4[Expand4.Length - 1].NameID = "talisman_olt_" + tmp.msgFile_m.data.Length.ToString("000");
                            OLT_ID = tmp.msgFile_m.data.Length;
                            Expand4[Expand4.Length - 1].ID = tmp.msgFile_m.data.Length;
                            Expand4[Expand4.Length - 1].Lines = new string[] { Msg };
                            tmp.msgFile_m.data = Expand4;
                            genericMsgListBurst[i] = tmp;
                        }
                        break;
                    }

                case 3:
                    {

                        for (int i = 0; i < genericMsgListNameBurstBTLHUD.Count; i++)
                        {
                            GenericMsgFile tmp = genericMsgListNameBurstBTLHUD[i];
                            msgData[] Expand5 = new msgData[tmp.msgFile_m.data.Length + 1];
                            Array.Copy(tmp.msgFile_m.data, Expand5, tmp.msgFile_m.data.Length);
                            Expand5[Expand5.Length - 1].NameID = "BHD_OLT_000_" + OLT_ID.ToString();
                            Expand5[Expand5.Length - 1].ID = tmp.msgFile_m.data.Length;
                            Expand5[Expand5.Length - 1].Lines = new string[] { Msg };
                            tmp.msgFile_m.data = Expand5;
                            genericMsgListNameBurstBTLHUD[i] = tmp;
                        }
                        break;
                    }

                case 4:
                    {

                        for (int i = 0; i < genericMsgListNameBurstPause.Count; i++)
                        {
                            GenericMsgFile tmp = genericMsgListNameBurstPause[i];
                            msgData[] Expand6 = new msgData[tmp.msgFile_m.data.Length + 1];
                            Array.Copy(tmp.msgFile_m.data, Expand6, tmp.msgFile_m.data.Length);
                            Expand6[Expand6.Length - 1].NameID = "BHD_OLT_000_" + OLT_ID.ToString();
                            Expand6[Expand6.Length - 1].ID = tmp.msgFile_m.data.Length;
                            Expand6[Expand6.Length - 1].Lines = new string[] { Msg };
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
            if (lstvBasic.SelectedItems.Count != 0)
            {
                txtEditNameb.Text = lstvBasic.SelectedItems[0].SubItems[0].Text;
                txtEditValueb.Text = lstvBasic.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void txtEditValueb_TextChanged(object sender, EventArgs e)
        {

            lstvBasic.SelectedItems[0].SubItems[1].Text = txtEditValueb.Text;
            float n;

            if (float.TryParse(txtEditValueb.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 144 + (lstvBasic.SelectedItems[0].Index * 4), 4);

        }

        private void lstvEffect1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstvEffect1.SelectedItems.Count != 0)
            {
                txtEditName1.Text = lstvEffect1.SelectedItems[0].SubItems[0].Text;
                txtEditValue1.Text = lstvEffect1.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void txtEditValue1_TextChanged(object sender, EventArgs e)
        {
            lstvEffect1.SelectedItems[0].SubItems[1].Text = txtEditValue1.Text;
            float n;

            if (float.TryParse(txtEditValue1.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 368 + (lstvEffect1.SelectedItems[0].Index * 4), 4);
        }

        private void lstvEffect2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstvEffect2.SelectedItems.Count != 0)
            {
                txtEditName2.Text = lstvEffect2.SelectedItems[0].SubItems[0].Text;
                txtEditValue2.Text = lstvEffect2.SelectedItems[0].SubItems[1].Text;
            }
        }

        private void txtEditValue2_TextChanged(object sender, EventArgs e)
        {
            lstvEffect2.SelectedItems[0].SubItems[1].Text = txtEditValue2.Text;
            float n;

            if (float.TryParse(txtEditValue2.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 592 + (lstvEffect2.SelectedItems[0].Index * 4), 4);
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

            msgStream.Save(Names, FileNameMsgN);
            msgStream.Save(Descs, FileNameMsgD);
            msgStream.Save(Burst, FileNameMsgB);
            msgStream.Save(BurstBTLHUD, FileNameMsgB_BTLHUD);
            msgStream.Save(BurstPause, FileNameMsgB_Pause);

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
        
            MessageBox.Show("Save Successful and files writtin to 'data' folder\nTo see changes in-game, the XV2Patcher must be installed.", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void EffectData()
        {
            if (File.Exists(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"/" + "EffectData.xml"))
            {
                //DEMON: This is now considered a debug feature.
                //load external EffectData.xml if it is found within the exe directory.
                XmlDocument ed = new XmlDocument();
                ed.Load(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"/" + "EffectData.xml");
                effList.ConstructList(ed.SelectSingleNode("EffectData/Effects").ChildNodes);
                actList.ConstructList(ed.SelectSingleNode("EffectData/Activators").ChildNodes);//Changed the section name from "Activations" cause it bothered me
                trgList.ConstructList(ed.SelectSingleNode("EffectData/Targets").ChildNodes);
                lbcList.ConstructList(ed.SelectSingleNode("EffectData/Colors").ChildNodes);
                kitList.ConstructList(ed.SelectSingleNode("EffectData/Kitypes").ChildNodes);
                vfxList.ConstructList(ed.SelectSingleNode("EffectData/Vfxtypes").ChildNodes);////Changed the section name from "Checkboxs" cause typo and to better reflect actual funtion.
            }

            else
            {
                //DEMON: We load an internal effect data now.
                //No more creating a blank one if the file isn't found within the exe directory.
                System.Reflection.Assembly dedxml = System.Reflection.Assembly.GetExecutingAssembly();
                XmlDocument ded = new XmlDocument();
                ded.Load(dedxml.GetManifestResourceStream("XV2SSEdit.Resources.DefaultEffectData.xml"));
                effList.ConstructList(ded.SelectSingleNode("EffectData/Effects").ChildNodes);
                actList.ConstructList(ded.SelectSingleNode("EffectData/Activators").ChildNodes);
                trgList.ConstructList(ded.SelectSingleNode("EffectData/Targets").ChildNodes);
                lbcList.ConstructList(ded.SelectSingleNode("EffectData/Colors").ChildNodes);
                kitList.ConstructList(ded.SelectSingleNode("EffectData/Kitypes").ChildNodes);
                vfxList.ConstructList(ded.SelectSingleNode("EffectData/Vfxtypes").ChildNodes);
            }

            cbEffect1.Items.Clear();
            cbEffect2.Items.Clear();
            cbActive1.Items.Clear();
            cbActive2.Items.Clear();

            //Load names from lists
            for (int i = 0; i < effList.effects.Length; i++)
            {
                cbEffectb.Items.Add(effList.effects[i].ID.ToString() + " - " + effList.effects[i].Description);
                cbEffect1.Items.Add(effList.effects[i].ID.ToString() + " - " + effList.effects[i].Description);
                cbEffect2.Items.Add(effList.effects[i].ID.ToString() + " - " + effList.effects[i].Description);
            }

            for (int i = 0; i < actList.activators.Length; i++)
            {
                cbActiveb.Items.Add(actList.activators[i].ID.ToString() + " - " + actList.activators[i].Description);
                cbActive1.Items.Add(actList.activators[i].ID.ToString() + " - " + actList.activators[i].Description);
                cbActive2.Items.Add(actList.activators[i].ID.ToString() + " - " + actList.activators[i].Description);
            }

            for (int i = 0; i < trgList.targets.Length; i++)
            {
                //cbTargetb.Items.Add(trgList.targets[i].ID.ToString() + " - " + trgList.targets[i].Description);
                //cbTarget1.Items.Add(trgList.targets[i].ID.ToString() + " - " + trgList.targets[i].Description);
                //cbTarget2.Items.Add(trgList.targets[i].ID.ToString() + " - " + trgList.targets[i].Description);

                cbTargetb.Items.Add(trgList.targets[i].Description.ToString());
                cbTarget1.Items.Add(trgList.targets[i].Description.ToString());
                cbTarget2.Items.Add(trgList.targets[i].Description.ToString());
            }

            for (int i = 0; i < lbcList.colors.Length; i++)
            {
                //cbLBColor.Items.Add(lbcList.colors[i].ID.ToString() + " - " + lbcList.colors[i].Description);

                cbLBColor.Items.Add(lbcList.colors[i].Description.ToString());
            }

            for (int i = 0; i < kitList.kitypes.Length; i++)
            {
                //cbKiType.Items.Add(kitList.kitypes[i].ID.ToString() + " - " + kitList.kitypes[i].Description);

                cbKiType.Items.Add(kitList.kitypes[i].Description.ToString());
            }

            for (int i = 0; i < vfxList.vfxtypes.Length; i++)
            {
                //cbVfxType1B.Items.Add(vfxList.vfxtypes[i].ID.ToString() + " - " + vfxList.vfxtypes[i].Description);
                //cbVfxType11.Items.Add(vfxList.vfxtypes[i].ID.ToString() + " - " + vfxList.vfxtypes[i].Description);
                //cbVfxType12.Items.Add(vfxList.vfxtypes[i].ID.ToString() + " - " + vfxList.vfxtypes[i].Description);
                //cbVfxType2B.Items.Add(vfxList.vfxtypes[i].ID.ToString() + " - " + vfxList.vfxtypes[i].Description);
                //cbVfxType21.Items.Add(vfxList.vfxtypes[i].ID.ToString() + " - " + vfxList.vfxtypes[i].Description);
                //cbVfxType22.Items.Add(vfxList.vfxtypes[i].ID.ToString() + " - " + vfxList.vfxtypes[i].Description);

                cbVfxType1B.Items.Add(vfxList.vfxtypes[i].Description.ToString());
                cbVfxType11.Items.Add(vfxList.vfxtypes[i].Description.ToString());
                cbVfxType12.Items.Add(vfxList.vfxtypes[i].Description.ToString());
                cbVfxType2B.Items.Add(vfxList.vfxtypes[i].Description.ToString());
                cbVfxType21.Items.Add(vfxList.vfxtypes[i].Description.ToString());
                cbVfxType22.Items.Add(vfxList.vfxtypes[i].Description.ToString());
            }
        }

        public int FindmsgIndex(ref msg msgdata, int id)
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
      
            currentSuperSoulIndex = itemList.SelectedIndex;
            UpdateData();
        }

        #region edit item

        //Soul Details
        private void txtMsgName_TextChanged(object sender, EventArgs e)
        {
            Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0] = txtMsgName.Text;
            itemList.Items[itemList.SelectedIndex] = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 0).ToString() + " - " + Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0];
        }

        private void txtMsgDesc_TextChanged(object sender, EventArgs e)
        {
            Descs.data[Items[itemList.SelectedIndex].msgIndexDesc].Lines[0] = txtMsgDesc.Text;
        }

        private void txtMsgLBDesc_TextChanged(object sender, EventArgs e)
        {
            //UNLEASHED: get the LB index from the soul (warning, it is shared)
            Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].Lines[0] = txtMsgLBDesc.Text;
        }

        private void cbStar_SelectedIndexChanged(object sender, EventArgs e)
        {
            Array.Copy(BitConverter.GetBytes((short)(cbStar.SelectedIndex + 1)), 0, Items[itemList.SelectedIndex].Data, 2, 2);
        }

        private void txtNameID_TextChanged(object sender, EventArgs e)
        {
            short ID;

            if (short.TryParse(txtNameID.Text, out ID))
                Array.Copy(BitConverter.GetBytes(ID), 0, Items[itemList.SelectedIndex].Data, 4, 2);

            Items[itemList.SelectedIndex].msgIndexName = FindmsgIndex(ref Names, BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 4));
            txtMsgName.Text = Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0];
            itemList.Items[itemList.SelectedIndex] = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 0).ToString() + " - " + Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0];
         
        }

        private void txtDescID_TextChanged(object sender, EventArgs e)
        {
            short ID;

            if (short.TryParse(txtDescID.Text, out ID))
                Array.Copy(BitConverter.GetBytes(ID), 0, Items[itemList.SelectedIndex].Data, 6, 2);

            Items[itemList.SelectedIndex].msgIndexDesc = FindmsgIndex(ref Descs, BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 6));
            txtMsgDesc.Text = Descs.data[Items[itemList.SelectedIndex].msgIndexDesc].Lines[0];
         
        }

        private void txtShopTest_TextChanged(object sender, EventArgs e)
        {
            short n;

            if (short.TryParse(txtShopTest.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 10, 2);
        }

        private void txtTPTest_TextChanged(object sender, EventArgs e)
        {
            short n;

            if (short.TryParse(txtTPTest.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 12, 2);
        }

        private void txtBuy_TextChanged(object sender, EventArgs e)
        {
            int n;

            if (int.TryParse(txtBuy.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 16, 4);
        }

        private void txtSell_TextChanged(object sender, EventArgs e)
        {
            int n;

            if (int.TryParse(txtSell.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 20, 4);
        }

        private void txtRace_TextChanged(object sender, EventArgs e)
        {
            short n;

            if (short.TryParse(txtRace.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 24, 2);
        }

        private void txtBuyTP_TextChanged(object sender, EventArgs e)
        {
            int n;

            if (int.TryParse(txtBuyTP.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 28, 4);
        }

        private void cbKiType_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = kitList.kitypes[cbKiType.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 32, 4);
        
        }

        private void txtLBAura_TextChanged(object sender, EventArgs e)
        {
            short n;

            if (short.TryParse(txtLBAura.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 36, 2);
        }

        private void cbLBColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = lbcList.colors[cbLBColor.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 38, 2);
        }

        private void txtLBDesc_TextChanged(object sender, EventArgs e)
        {
            short ID;

            if (short.TryParse(txtLBDesc.Text, out ID))
                Array.Copy(BitConverter.GetBytes(ID), 0, Items[itemList.SelectedIndex].Data, 40, 2);

            Items[itemList.SelectedIndex].msgIndexBurst = FindmsgIndex(ref Burst, BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 40));
            txtMsgLBDesc.Text = Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].Lines[0];

            //Demon: updates the in battle description text when the description id is changed
            Items[itemList.SelectedIndex].msgIndexBurstBTL = getLB_BTL_Pause_DescID(BurstBTLHUD, Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].NameID);
            Items[itemList.SelectedIndex].msgIndexBurstPause = getLB_BTL_Pause_DescID(BurstPause, Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].NameID);
            txtMsgLBDescBTL.Text = BurstBTLHUD.data[Items[itemList.SelectedIndex].msgIndexBurstBTL].Lines[0];
        }

        private void txtLBSoulID1_TextChanged(object sender, EventArgs e)
        {
            ushort n;

            if (ushort.TryParse(txtLBSoulID1.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 42, 2);
        }

        private void txtLBSoulID2_TextChanged(object sender, EventArgs e)
        {
            ushort n;

            if (ushort.TryParse(txtLBSoulID2.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 44, 2);
        }

        private void txtLBSoulID3_TextChanged(object sender, EventArgs e)
        {
            ushort n;

            if (ushort.TryParse(txtLBSoulID3.Text, out n))
                Array.Copy(BitConverter.GetBytes(n), 0, Items[itemList.SelectedIndex].Data, 46, 2);
        }

        //Basic Details
        private void cbEffectb_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = effList.effects[cbEffectb.SelectedIndex].ID;
            byte[] pass;

            if (ID == -1)
                pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            else
                pass = BitConverter.GetBytes(ID);

            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 48, 4);
        }

        private void cbActiveb_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = actList.activators[cbActiveb.SelectedIndex].ID;
            byte[] pass;

            if (ID == -1)
                pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            else
                pass = BitConverter.GetBytes(ID);

            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 52, 4);
        }

        private void txtLimitB_TextChanged(object sender, EventArgs e)
        {
            int ID;

            if (int.TryParse(txtLimitB.Text, out ID))
            {
                byte[] pass;

                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 56, 4);
            }
        }

        private void txtDurationB_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtDurationB.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 60, 4);
            }
        }

        private void txtValue1B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtValue1B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 64, 4);
            }
        }

        private void txtValue2B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtValue2B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 68, 4);
            }
        }

        private void txtValue3B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtValue3B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 72, 4);
            }
        }

        private void txtValue4B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtValue4B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 76, 4);
            }
        }

        private void txtValue5B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtValue5B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 80, 4);
            }
        }

        private void txtValue6B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtValue6B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 84, 4);
            }
        }

        private void txtChanceb_TextChanged(object sender, EventArgs e)
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

        private void txtUnki44b_TextChanged(object sender, EventArgs e)
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

        private void txtAmount1B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtAmount1B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 96, 4);
            }
        }

        private void txtAmount2B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtAmount2B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 100, 4);
            }
        }

        private void txtAmount3B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtAmount3B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 104, 4);
            }
        }

        private void txtAmount4B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtAmount4B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 108, 4);
            }
        }

        private void txtAmount5B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtAmount5B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 112, 4);
            }
        }

        private void txtAmount6B_TextChanged(object sender, EventArgs e)
        {
            float ID;

            if (float.TryParse(txtAmount6B.Text, out ID))
            {
                byte[] pass;
                pass = BitConverter.GetBytes(ID);
                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 116, 4);
            }
        }

        private void cbVfxType2B_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = vfxList.vfxtypes[cbVfxType2B.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 120, 4);
        }

        private void txtVfxId2B_TextChanged(object sender, EventArgs e)
        {
            int ID;

            if (int.TryParse(txtVfxId2B.Text, out ID))
            {
                byte[] pass;

                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 124, 4);
            }
        }

        private void cbVfxType1B_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = vfxList.vfxtypes[cbVfxType1B.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 128, 4);
        }

        private void txtVfxId1B_TextChanged(object sender, EventArgs e)
        {
            int ID;

            if (int.TryParse(txtVfxId1B.Text, out ID))
            {
                byte[] pass;

                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 132, 4);
            }
        }

        private void txtUnki88b_TextChanged(object sender, EventArgs e)
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

        private void cbTargetb_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = trgList.targets[cbTargetb.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 140, 4);
        }

        //Effect Details 1
        private void cbEffect1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = effList.effects[cbEffect1.SelectedIndex].ID;
            byte[] pass;

            if (ID == -1)
                pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            else
                pass = BitConverter.GetBytes(ID);

            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 272, 4);
        }

        private void cbActive1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = actList.activators[cbActive1.SelectedIndex].ID;
            byte[] pass;

            if (ID == -1)
                pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            else
                pass = BitConverter.GetBytes(ID);

            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 276, 4);
        }

        private void txtLimit1_TextChanged(object sender, EventArgs e)
        {
            int ID;

            if (int.TryParse(txtLimit1.Text, out ID))
            {
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 280, 4);
            }
        }

        private void txtDuration1_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtDuration1.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 284, 4);
            }
        }

        private void txtValue11_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue11.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 288, 4);
            }
        }

        private void txtValue21_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue21.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 292, 4);
            }
        }

        private void txtValue31_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue31.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 296, 4);
            }
        }

        private void txtValue41_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue41.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 300, 4);
            }
        }

        private void txtValue51_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue51.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 304, 4);
            }
        }

        private void txtValue61_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue61.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 308, 4);
            }
        }

        private void txtChance1_TextChanged(object sender, EventArgs e)
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

        private void txtUnki441_TextChanged(object sender, EventArgs e)
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

        private void txtAmount11_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount11.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 320, 4);
            }
        }

        private void txtAmount21_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount21.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 324, 4);
            }
        }

        private void txtAmount31_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount31.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 328, 4);
            }
        }

        private void txtAmount41_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount41.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 332, 4);
            }
        }

        private void txtAmount51_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount51.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 336, 4);
            }
        }

        private void txtAmount61_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount61.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 340, 4);
            }
        }

        private void cbVfxType21_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = vfxList.vfxtypes[cbVfxType21.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 344, 4);
        }

        private void txtVfxId21_TextChanged(object sender, EventArgs e)
        {
            int ID;
            if (int.TryParse(txtVfxId21.Text, out ID))
            {
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 348, 4);
            }
        }

        private void cbVfxType11_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = vfxList.vfxtypes[cbVfxType11.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 352, 4);
        }

        private void txtVfxId11_TextChanged(object sender, EventArgs e)
        {
            int ID;
            if (int.TryParse(txtVfxId11.Text, out ID))
            {
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 356, 4);
            }
        }

        private void txtUnki881_TextChanged(object sender, EventArgs e)
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

        private void cbTarget1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = trgList.targets[cbTarget1.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 364, 4);
        }

        //Effect Details 2
        private void cbEffect2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = effList.effects[cbEffect2.SelectedIndex].ID;
            byte[] pass;
            if (ID == -1)
                pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            else
                pass = BitConverter.GetBytes(ID);

            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 496, 4);
        }

        private void cbActive2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = actList.activators[cbActive2.SelectedIndex].ID;
            byte[] pass;
            if (ID == -1)
                pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            else
                pass = BitConverter.GetBytes(ID);

            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 500, 4);
        }

        private void txtLimit2_TextChanged(object sender, EventArgs e)
        {
            int ID;
            if (int.TryParse(txtLimit2.Text, out ID))
            {
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 504, 4);
            }
        }

        private void txtDuration2_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtDuration2.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 508, 4);
            }
        }

        private void txtValue12_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue12.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 512, 4);
            }
        }

        private void txtValue22_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue22.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 516, 4);
            }
        }

        private void txtValue32_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue32.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 520, 4);
            }
        }

        private void txtValue42_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue42.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 524, 4);
            }
        }

        private void txtValue52_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue52.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 528, 4);
            }
        }

        private void txtValue62_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtValue62.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 532, 4);
            }
        }

        private void txtChance2_TextChanged(object sender, EventArgs e)
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

        private void txtUnki442_TextChanged(object sender, EventArgs e)
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

        private void txtAmount12_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount12.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 544, 4);
            }
        }

        private void txtAmount22_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount22.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 548, 4);
            }
        }

        private void txtAmount32_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount32.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 552, 4);
            }
        }

        private void txtAmount42_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount42.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 556, 4);
            }
        }

        private void txtAmount52_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount52.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 560, 4);
            }
        }

        private void txtAmount62_TextChanged(object sender, EventArgs e)
        {
            float ID;
            if (float.TryParse(txtAmount62.Text, out ID))
            {
                byte[] pass;

                pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 564, 4);
            }
        }

        private void cbVfxType22_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = vfxList.vfxtypes[cbVfxType22.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 568, 4);
        }

        private void txtVfxId22_TextChanged(object sender, EventArgs e)
        {
            int ID;
            if (int.TryParse(txtVfxId22.Text, out ID))
            {
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 572, 4);
            }
        }

        private void cbVfxType12_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = vfxList.vfxtypes[cbVfxType12.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 576, 4);
        }

        private void txtVfxId12_TextChanged(object sender, EventArgs e)
        {
            int ID;
            if (int.TryParse(txtVfxId12.Text, out ID))
            {
                byte[] pass;
                if (ID == -1)
                    pass = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
                else
                    pass = BitConverter.GetBytes(ID);

                Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 580, 4);
            }
        }

        private void txtUnki882_TextChanged(object sender, EventArgs e)
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

        private void cbTarget2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ID = trgList.targets[cbTarget2.SelectedIndex].ID;
            byte[] pass;
            pass = BitConverter.GetBytes(ID);
            Array.Copy(pass, 0, Items[itemList.SelectedIndex].Data, 588, 4);
        }
        #endregion

        private void UpdateData()
        {
            // Super Soul Details
            txtMsgName.Text = Names.data[Items[itemList.SelectedIndex].msgIndexName].Lines[0];
            txtMsgDesc.Text = Descs.data[Items[itemList.SelectedIndex].msgIndexDesc].Lines[0];

            //UNLEASHED: Add BTL LB Desc
            txtMsgLBDesc.Text = Burst.data[Items[itemList.SelectedIndex].msgIndexBurst].Lines[0];
            txtMsgLBDescBTL.Text = BurstBTLHUD.data[Items[itemList.SelectedIndex].msgIndexBurstBTL].Lines[0];

            txtID.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 0).ToString();
            cbStar.SelectedIndex = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 2) - 1;
            txtNameID.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 4).ToString();
            txtDescID.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 6).ToString();

            //Demon: this value sets the item type for the menus and shops.
            //this should never be edited in a talisman idb else the souls won't be souls anymore, so commented out
            //txtIDbType.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 8).ToString();

            txtShopTest.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 10).ToString();
            txtTPTest.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 12).ToString();

            //Demon: unknown what this is for but it's always -1 for every soul, so commented out. probably just filler.
            //txti_14.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 14).ToString();

            txtBuy.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 16).ToString();
            txtSell.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 20).ToString();
            txtRace.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 24).ToString();
            txtBuyTP.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 28).ToString();
            cbKiType.SelectedIndex = kitList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 32));
            txtLBAura.Text = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 36).ToString();
            cbLBColor.SelectedIndex = lbcList.FindIndex(BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 38));
            txtLBDesc.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 40).ToString();
            txtLBSoulID1.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 42).ToString();
            txtLBSoulID2.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 44).ToString();
            txtLBSoulID3.Text = BitConverter.ToUInt16(Items[itemList.SelectedIndex].Data, 46).ToString();

            // Basic Details
            cbEffectb.SelectedIndex = effList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 48));
            cbActiveb.SelectedIndex = actList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 52));
            txtLimitB.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 56).ToString();
            txtDurationB.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 60).ToString();
            txtValue1B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 64).ToString();
            txtValue2B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 68).ToString();
            txtValue3B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 72).ToString();
            txtValue4B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 76).ToString();
            txtValue5B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 80).ToString();
            txtValue6B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 84).ToString();
            txtChanceb.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 88).ToString();
            txtUnki44b.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 92).ToString();
            txtAmount1B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 96).ToString();
            txtAmount2B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 100).ToString();
            txtAmount3B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 104).ToString();
            txtAmount4B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 108).ToString();
            txtAmount5B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 112).ToString();
            txtAmount6B.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 116).ToString();
            cbVfxType2B.SelectedIndex = vfxList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 120));
            txtVfxId2B.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 124).ToString();
            cbVfxType1B.SelectedIndex = vfxList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 128));
            txtVfxId1B.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 132).ToString();
            txtUnki88b.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 136).ToString();
            cbTargetb.SelectedIndex = trgList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 140));

            //Stats for Basic Details
            for (int i = 0; i < lstvBasic.Items.Count; i++)
            {
                lstvBasic.Items[i].SubItems[1].Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 144 + (i * 4)).ToString();
            }

            //Effest 1 Details
            cbEffect1.SelectedIndex = effList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 272));
            cbActive1.SelectedIndex = actList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 276));
            txtLimit1.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 280).ToString();
            txtDuration1.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 284).ToString();
            txtValue11.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 288).ToString();
            txtValue21.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 292).ToString();
            txtValue31.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 296).ToString();
            txtValue41.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 300).ToString();
            txtValue51.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 304).ToString();
            txtValue61.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 308).ToString();
            txtChance1.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 312).ToString();
            txtUnki441.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 316).ToString();
            txtAmount11.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 320).ToString();
            txtAmount21.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 324).ToString();
            txtAmount31.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 328).ToString();
            txtAmount41.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 332).ToString();
            txtAmount51.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 336).ToString();
            txtAmount61.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 340).ToString();
            cbVfxType21.SelectedIndex = vfxList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 344));
            txtVfxId21.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 348).ToString();
            cbVfxType11.SelectedIndex = vfxList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 352));
            txtVfxId11.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 356).ToString();
            txtUnki881.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 360).ToString();
            cbTarget1.SelectedIndex = trgList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 364));

            //Stats for Effect 1 Details
            for (int i = 0; i < lstvEffect1.Items.Count; i++)
            {
                lstvEffect1.Items[i].SubItems[1].Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 368 + (i * 4)).ToString();
            }

            //Effect 2 Details
            cbEffect2.SelectedIndex = effList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 496));
            cbActive2.SelectedIndex = actList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 500));
            txtLimit2.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 504).ToString();
            txtDuration2.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 508).ToString();
            txtValue12.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 512).ToString();
            txtValue22.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 516).ToString();
            txtValue32.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 520).ToString();
            txtValue42.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 524).ToString();
            txtValue52.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 528).ToString();
            txtValue62.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 532).ToString();
            txtChance2.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 536).ToString();
            txtUnki442.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 540).ToString();
            txtAmount12.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 544).ToString();
            txtAmount22.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 548).ToString();
            txtAmount32.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 552).ToString();
            txtAmount42.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 556).ToString();
            txtAmount52.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 560).ToString();
            txtAmount62.Text = BitConverter.ToSingle(Items[itemList.SelectedIndex].Data, 564).ToString();
            cbVfxType22.SelectedIndex = vfxList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 568));
            txtVfxId22.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 572).ToString();
            cbVfxType12.SelectedIndex = vfxList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 576));
            txtVfxId12.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 580).ToString();
            txtUnki882.Text = BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 584).ToString();
            cbTarget2.SelectedIndex = trgList.FindIndex(BitConverter.ToInt32(Items[itemList.SelectedIndex].Data, 588));

            //Stats for Effect 1 Details
            for (int i = 0; i < lstvEffect2.Items.Count; i++)
            {
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
                    resolveLBIDsForParentSS(ref indexCollections, tempData, intIDofLB);
            }
            return true;
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //add/import super soul
            //load ssp file
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
                itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
            }

            itemList.SelectedIndex = 0;
            MessageBox.Show("SSP imported successfully");
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //remove super soul  
            //UNLEASHED: there are probably better methods to do this, but working with Lists is just so much easier.

            List<idbItem> Reduce = Items.ToList<idbItem>();
            Reduce.RemoveAt(currentSuperSoulIndex);
            Items = Reduce.ToArray();
            itemList.Items.Clear();

            for (int i = 0; i < Items.Length; i++)
            {
                itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
            }

            //UNLEASHED: return to first item
            itemList.SelectedIndex = 0;
        }

        private void nameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //add msg name
            msgData[] Expand = new msgData[Names.data.Length + 1];
            Array.Copy(Names.data, Expand, Names.data.Length);
            Expand[Expand.Length - 1].NameID = "talisman_" + Names.data.Length.ToString("000");
            Expand[Expand.Length - 1].ID = Names.data.Length;
            Expand[Expand.Length - 1].Lines = new string[] { "New Name Entry" };
            Names.data = Expand;
            writeToMsgText(0, "New Name Entry");
            txtNameID.Text = Names.data[Names.data.Length - 1].ID.ToString();
        }

        private void descriptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //remove msg name
                msgData[] reduce = new msgData[Names.data.Length - 1];
                Array.Copy(Names.data, reduce, Names.data.Length - 1);
                Names.data = reduce;
        }

        private void nameToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //add msg desc
            msgData[] Expand = new msgData[Descs.data.Length + 1];
            Array.Copy(Descs.data, Expand, Descs.data.Length);
            Expand[Expand.Length - 1].NameID = "talisman_eff_" + Descs.data.Length.ToString("000");
            Expand[Expand.Length - 1].ID = Descs.data.Length;
            Expand[Expand.Length - 1].Lines = new string[] { "New Description Entry" };
            Descs.data = Expand;
            writeToMsgText(1, "New Description Entry");
            txtDescID.Text = Descs.data[Descs.data.Length - 1].ID.ToString();
        }

        private void descriptionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //remove msg desc
            msgData[] reduce = new msgData[Descs.data.Length - 1];
            Array.Copy(Descs.data, reduce, Descs.data.Length - 1);
            Descs.data = reduce;
        }

        //UNLEASHED: Mugen's code to convert string into byte array
        //UNLEASHED: made public to use within export form. yes i know i could copy it to the export form class and made
        //it its own method, but it doesn't matter much.
        public byte[] CharByteArray(string text)
        {
            return System.Text.Encoding.Unicode.GetBytes(text.ToCharArray());
        }

        //UNLEASHED: Mugen's code to convert MSG strings from unicode to ASCII format
        //UNLEASHED: we could have use the .NET natvie System.Text methods. but this works..
        private string BytetoString(byte[] bytes)
        {
            char[] chrArray = new char[bytes.Length / 2];

            for (int i = 0; i < bytes.Length / 2; i++)
                chrArray[i] = BitConverter.ToChar(bytes, i * 2);

            return new string(chrArray);
        }

        //DEMON: old code for replacing the currently selected SS with a .zss
        //commented out for now in case i want to re add this feature
       //private void replaceImportToolStripMenuItem_Click(object sender, EventArgs e)
       //{
       //    //import/replace
       //    OpenFileDialog browseFile = new OpenFileDialog();
       //    browseFile.Filter = "Super Soul File (*.zss)|*.zss";
       //    browseFile.Title = "Browse for Z Soul Share File";
       //    if (browseFile.ShowDialog() == DialogResult.Cancel)
       //        return;
       //
       //    byte[] zssfile = File.ReadAllBytes(browseFile.FileName);
       //
       //    int nameCount = BitConverter.ToInt32(zssfile, 4);
       //    int DescCount = BitConverter.ToInt32(zssfile, 8);
       //    short nameID = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 4);
       //    short DescID = BitConverter.ToInt16(Items[itemList.SelectedIndex].Data, 6);
       //
       //    Array.Copy(zssfile, 12 + (nameCount * 2) + (DescCount * 2), Items[itemList.SelectedIndex].Data, 2, 718);
       //    Array.Copy(BitConverter.GetBytes(nameID), 0, Items[itemList.SelectedIndex].Data, 4, 2);
       //    Array.Copy(BitConverter.GetBytes(DescID), 0, Items[itemList.SelectedIndex].Data, 6, 2);
       //
       //    byte[] pass;
       //
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
       //
       //    UpdateData();
       //}

        private void exportToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            (new Export(this, Names, Descs, Burst, BurstBTLHUD, BurstPause)).Show();
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

        private void createNewSoulToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = AddSS(Properties.Resources.newss);

            if (index < 0)
                return;

            itemList.Items.Clear();

            for (int i = 0; i < Items.Length; i++)
            {
                itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
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
            byte[] pass = null;
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

            if (nameCount > 0)
                writeToMsgText(0, BytetoString(pass));
            else
                writeToMsgText(0, "New Name Entry");

            //expand description msg
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

            if (DescCount > 0)
                writeToMsgText(1, BytetoString(pass));
            else
                writeToMsgText(1, "New Description Entry");

            //UNLEASHED: expand LB Desc / LB DescBTL MSG
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

            if (LBDescCount > 0)
                writeToMsgText(2, BytetoString(pass));
            else
                writeToMsgText(2, "New LB Desc Entry");

            int OLT_ID = Items[newPos].msgIndexBurst;
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

            if (LBDescCountBtl > 0)
                writeToMsgText(3, BytetoString(pass), OLT_ID);
            else
                writeToMsgText(3, "New LB Battle Desc Entry", OLT_ID);

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

            if (LBDescCountPause > 0)
                writeToMsgText(4, BytetoString(pass), OLT_ID);
            else
                writeToMsgText(4, "New LB Pause Desc Entry", OLT_ID);

            return newPos;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Forms.Settings form = new Forms.Settings(settings))
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
            MessageBox.Show($"XV2 Super Soul Editor Version {toolVersion}\n\nCredits:\nDemonBoy - Tool Creator\nLazybone & Unleashed - Help with fixes/additions\nMugenAttack - Original Source code", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        MessageBox.Show("couldn't find any more super souls");
                    else
                        MessageBox.Show("super soul not found.. ");
                    lastFoundIndex = 0;
                }
            }
        }

        private void txtMsgLBDescBTL_TextChanged(object sender, EventArgs e)
        {
            BurstBTLHUD.data[Items[itemList.SelectedIndex].msgIndexBurstBTL].Lines[0] = txtMsgLBDescBTL.Text;
            // UNLEASHED: Some SSs don't have the LBPauseDesc (we aren't sure if its evne used by the game..)
            if (Items[itemList.SelectedIndex].msgIndexBurstPause > -1)
            BurstPause.data[Items[itemList.SelectedIndex].msgIndexBurstPause].Lines[0] = txtMsgLBDescBTL.Text;
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

        //DEMON: This was previously set up for debugging but became a full feature.
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

            if (debugLBSelect.SelectedIndex == 26)
            {
                txtLBAura.Text = "-1";
                txtLBDesc.Text = "0";
                txtLBSoulID1.Text = "65535";
                txtLBSoulID2.Text = "65535";
                txtLBSoulID3.Text = "65535";
                cbLBColor.SelectedIndex = 0;
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
            List<byte> copiedBytes = new List<byte>();

            string nameText = Names.data[Items[currentSuperSoulIndex].msgIndexName].Lines[0];
            string DescText = Descs.data[Items[currentSuperSoulIndex].msgIndexDesc].Lines[0];
            string LBDescText = Burst.data[Items[currentSuperSoulIndex].msgIndexBurst].Lines[0];
            string LBBTLHUDDescText = BurstBTLHUD.data[Items[currentSuperSoulIndex].msgIndexBurstBTL].Lines[0];

            // UNLEASHED: Some SSs don't have the LBPauseDesc (we aren't sure if its even used by the game..)
            string LBPauseDescText;

            if (Items[currentSuperSoulIndex].msgIndexBurstPause > -1)
                LBPauseDescText = BurstPause.data[Items[currentSuperSoulIndex].msgIndexBurstPause].Lines[0];
            else
                LBPauseDescText = "";

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
            Array.Copy(Items[currentSuperSoulIndex].Data, 2, tmp, 0, 718);
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
                itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
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

            byte[] pass = null;
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

            if (LBDescCount > 0)
                writeToMsgText(2, BytetoString(pass));
            else
                writeToMsgText(2, "New LB Desc Entry");

            int OLT_ID = Items[currentSuperSoulIndex].msgIndexBurst;
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

            if (LBDescCountBtl > 0)
                writeToMsgText(3, BytetoString(pass), OLT_ID);
            else
                writeToMsgText(3, "New LB Battle Desc Entry", OLT_ID);

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
            if (LBDescCountPause > 0)

                writeToMsgText(4, BytetoString(pass), OLT_ID);
            else
                writeToMsgText(4, "New LB Pause Desc Entry", OLT_ID);

            txtLBDesc.Text = Expand4[Expand4.Length - 1].ID.ToString();
        }

        private void createNewSoulAsLimitBurstToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = AddLB(Properties.Resources.newss);

            if (index < 0)
                return;

            itemList.Items.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                itemList.Items.Add(BitConverter.ToUInt16(Items[i].Data, 0).ToString() + " - " + Names.data[Items[i].msgIndexName].Lines[0]);
            }

            itemList.SelectedIndex = index;

            txtNameID.Text = "0";
            txtDescID.Text = "0";
            cbKiType.SelectedIndex = 0;
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
    }
}