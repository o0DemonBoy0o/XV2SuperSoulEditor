using System.Xml;

//Parses and reads the lists in the EffectData xml
namespace XV2SSEdit
{
    struct Effect
    {
        public int ID;
        public string Description;  
    }

    struct Activator
    {
        public int ID;
        public string Description;   
    }

    struct Target
    {
        public int ID;
        public string Description;
    }

    struct LBColor
    {
        public int ID;
        public string Description;
    }

    struct Kitype
    {
        public int ID;
        public string Description;
    }

    struct VfxType
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

    class ActivatorList
    {
        public Activator[] activators;
        public void ConstructList(XmlNodeList activatorlist)
        {
            activators = new Activator[activatorlist.Count];
            for (int i = 0; i < activatorlist.Count; i++)
            {
                activators[i].ID = int.Parse(activatorlist[i].Attributes["id"].Value);
                activators[i].Description = activatorlist[i].InnerText;
            }
        }

        public int FindIndex(int ID)
        {
            for (int i = 0; i < activators.Length; i++)
            {
                if (activators[i].ID == ID)
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

    class LBColorList
    {
        public LBColor[] colors;
        public void ConstructList(XmlNodeList limitcolors)
        {
            colors = new LBColor[limitcolors.Count];
            for (int i = 0; i < limitcolors.Count; i++)
            {
                colors[i].ID = int.Parse(limitcolors[i].Attributes["id"].Value);
                colors[i].Description = limitcolors[i].InnerText;
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

    class VFXList
    {
        public VfxType[] vfxtypes;
        public void ConstructList(XmlNodeList vfxlist)
        {
            vfxtypes = new VfxType[vfxlist.Count];
            for (int i = 0; i < vfxlist.Count; i++)
            {
                vfxtypes[i].ID = int.Parse(vfxlist[i].Attributes["id"].Value);
                vfxtypes[i].Description = vfxlist[i].InnerText;
            }
        }

        public int FindIndex(int ID)
        {
            for (int i = 0; i < vfxtypes.Length; i++)
            {
                if (vfxtypes[i].ID == ID)
                    return i;
            }
            return 0;
        }
    }

}
