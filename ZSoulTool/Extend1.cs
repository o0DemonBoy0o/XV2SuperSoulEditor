using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Msgfile;

namespace XV2SSEdit
{
    struct Effect
    {
        public int ID;
        public string Description;  
    }

    struct Activation
    {
        public int ID;
        public string Description;   
    }

    struct Target
    {
        public int ID;
        public string Description;
    }

    struct Color
    {
        public int ID;
        public string Description;
    }

    struct Kitype
    {
        public int ID;
        public string Description;
    }

    struct Checkbox
    {
        public int ID;
        public string Description;
    }
    //UNLEASHED: made public to help with exporting
   public struct idbItem
    {
        public int msgIndexName;
        public int msgIndexDesc;
        public int msgIndexBurst;
        public int msgIndexBurstBTL;
        public int msgIndexBurstPause;
        public byte[] Data;
    }

    class EffectList
    {
        public Effect[] effects;
        public void ConstructList(XmlNodeList effectlist)
        {
            effects = new Effect[effectlist.Count];
            for (int i = 0; i < effectlist.Count; i++)
            {
                effects[i].ID = int.Parse(effectlist[i].Attributes["id"].Value);
                effects[i].Description = effectlist[i].InnerText;
            }
        }

        public void ConstructFromUnknown(ref idbItem[] items)
        {
            List<int> IDs = new List<int>();
            for (int i = 0; i < items.Length; i++)
            {
                if (!IDs.Contains(BitConverter.ToInt32(items[i].Data, 160)))
                    IDs.Add(BitConverter.ToInt32(items[i].Data, 160));

                if (!IDs.Contains(BitConverter.ToInt32(items[i].Data, 384)))
                    IDs.Add(BitConverter.ToInt32(items[i].Data, 384));
            }

            IDs.Sort();

            effects = new Effect[IDs.Count];
            for (int i = 0; i < IDs.Count; i++)
            {
                effects[i].ID = IDs[i];
                effects[i].Description = "Undetermined";
            }
        }

        public int FindIndex(int ID)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].ID == ID)
                    return i;
            }
            return 0;
        }
    }

    class ActivationList
    {
        public Activation[] activations;
        public void ConstructList(XmlNodeList activationlist)
        {
            activations = new Activation[activationlist.Count];
            for (int i = 0; i < activationlist.Count; i++)
            {
                activations[i].ID = int.Parse(activationlist[i].Attributes["id"].Value);
                activations[i].Description = activationlist[i].InnerText;
            }
        }

        public void ConstructFromUnknown(ref idbItem[] items)
        {
            List<int> IDs = new List<int>();
            for (int i = 0; i < items.Length; i++)
            {
                if (!IDs.Contains(BitConverter.ToInt32(items[i].Data, 164)))
                    IDs.Add(BitConverter.ToInt32(items[i].Data, 164));

                if (!IDs.Contains(BitConverter.ToInt32(items[i].Data, 388)))
                    IDs.Add(BitConverter.ToInt32(items[i].Data, 388));
            }

            IDs.Sort();

            activations = new Activation[IDs.Count];
            for (int i = 0; i < IDs.Count; i++)
            {
                activations[i].ID = IDs[i];
                activations[i].Description = "Undetermined";
            }
        }

        public int FindIndex(int ID)
        {
            for (int i = 0; i < activations.Length; i++)
            {
                if (activations[i].ID == ID)
                    return i;
            }
            return 0;
        }

    }

    class TargetList
    {
        public Target[] targets;
        public void ConstructList(XmlNodeList targetlist)
        {
            targets = new Target[targetlist.Count];
            for (int i = 0; i < targetlist.Count; i++)
            {
                targets[i].ID = int.Parse(targetlist[i].Attributes["id"].Value);
                targets[i].Description = targetlist[i].InnerText;
            }
        }

        public void ConstructFromUnknown(ref idbItem[] items)
        {
            List<int> IDs = new List<int>();
            for (int i = 0; i < items.Length; i++)
            {
                if (!IDs.Contains(BitConverter.ToInt32(items[i].Data, 160)))
                    IDs.Add(BitConverter.ToInt32(items[i].Data, 160));

                if (!IDs.Contains(BitConverter.ToInt32(items[i].Data, 384)))
                    IDs.Add(BitConverter.ToInt32(items[i].Data, 384));
            }

            IDs.Sort();

            targets = new Target[IDs.Count];
            for (int i = 0; i < IDs.Count; i++)
            {
                targets[i].ID = IDs[i];
                targets[i].Description = "Undetermined";
            }
        }

        public int FindIndex(int ID)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].ID == ID)
                    return i;
            }
            return 0;
        }
    }

    class ColorList
    {
        public Color[] colors;
        public void ConstructList(XmlNodeList colorlist)
        {
            colors = new Color[colorlist.Count];
            for (int i = 0; i < colorlist.Count; i++)
            {
                colors[i].ID = int.Parse(colorlist[i].Attributes["id"].Value);
                colors[i].Description = colorlist[i].InnerText;
            }
        }

        public void ConstructFromUnknown(ref idbItem[] items)
        {
            List<int> IDs = new List<int>();
            for (int i = 0; i < items.Length; i++)
            {
                if (!IDs.Contains(BitConverter.ToInt16(items[i].Data, 160)))
                    IDs.Add(BitConverter.ToInt16(items[i].Data, 160));

                if (!IDs.Contains(BitConverter.ToInt16(items[i].Data, 384)))
                    IDs.Add(BitConverter.ToInt16(items[i].Data, 384));
            }

            IDs.Sort();

            colors = new Color[IDs.Count];
            for (int i = 0; i < IDs.Count; i++)
            {
                colors[i].ID = IDs[i];
                colors[i].Description = "Undetermined";
            }
        }

        public int FindIndex(int ID)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i].ID == ID)
                    return i;
            }
            return 0;
        }
    }

    class KitypeList
    {
        public Kitype[] kitypes;
        public void ConstructList(XmlNodeList kitypelist)
        {
            kitypes = new Kitype[kitypelist.Count];
            for (int i = 0; i < kitypelist.Count; i++)
            {
                kitypes[i].ID = int.Parse(kitypelist[i].Attributes["id"].Value);
                kitypes[i].Description = kitypelist[i].InnerText;
            }
        }

        public void ConstructFromUnknown(ref idbItem[] items)
        {
            List<int> IDs = new List<int>();
            for (int i = 0; i < items.Length; i++)
            {
                if (!IDs.Contains(BitConverter.ToInt32(items[i].Data, 160)))
                    IDs.Add(BitConverter.ToInt32(items[i].Data, 160));

                if (!IDs.Contains(BitConverter.ToInt32(items[i].Data, 384)))
                    IDs.Add(BitConverter.ToInt32(items[i].Data, 384));
            }

            IDs.Sort();

            kitypes = new Kitype[IDs.Count];
            for (int i = 0; i < IDs.Count; i++)
            {
                kitypes[i].ID = IDs[i];
                kitypes[i].Description = "Undetermined";
            }
        }

        public int FindIndex(int ID)
        {
            for (int i = 0; i < kitypes.Length; i++)
            {
                if (kitypes[i].ID == ID)
                    return i;
            }
            return 0;
        }
    }

    class CheckboxList
    {
        public Checkbox[] checkboxs;
        public void ConstructList(XmlNodeList checkboxlist)
        {
            checkboxs = new Checkbox[checkboxlist.Count];
            for (int i = 0; i < checkboxlist.Count; i++)
            {
                checkboxs[i].ID = int.Parse(checkboxlist[i].Attributes["id"].Value);
                checkboxs[i].Description = checkboxlist[i].InnerText;
            }
        }

        public void ConstructFromUnknown(ref idbItem[] items)
        {
            List<int> IDs = new List<int>();
            for (int i = 0; i < items.Length; i++)
            {
                if (!IDs.Contains(BitConverter.ToInt16(items[i].Data, 160)))
                    IDs.Add(BitConverter.ToInt16(items[i].Data, 160));

                if (!IDs.Contains(BitConverter.ToInt16(items[i].Data, 384)))
                    IDs.Add(BitConverter.ToInt16(items[i].Data, 384));
            }

            IDs.Sort();

            checkboxs = new Checkbox[IDs.Count];
            for (int i = 0; i < IDs.Count; i++)
            {
                checkboxs[i].ID = IDs[i];
                checkboxs[i].Description = "Undetermined";
            }
        }

        public int FindIndex(int ID)
        {
            for (int i = 0; i < checkboxs.Length; i++)
            {
                if (checkboxs[i].ID == ID)
                    return i;
            }
            return 0;
        }
    }

}
