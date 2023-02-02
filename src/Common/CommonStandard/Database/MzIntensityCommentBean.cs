using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of each MS peak in MSP format.
    /// In addition to mz and intensity, comment field is also stored.
    /// Also, I preapred 'Frag' field to annotate lipids by means of fatty acids or polar heads fragment. 
    /// These frags should be stored in MSP file as under-bar query (_f_. see the msp parcer or LBM file included in MS-DIAL folder) in comment.
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class MzIntensityCommentBean
    {
        [DataMember]
        float mz;
        [DataMember]
        float intensity;
        [DataMember]
        string comment;
        [DataMember]
        string frag;

        public MzIntensityCommentBean()
        {
            comment = string.Empty;
            frag = string.Empty;
            mz = 0;
            intensity = 0;
        }

        [Key(0)]
        public string Frag
        {
            get { return frag; }
            set { frag = value; }
        }

        [Key(1)]
        public float Mz
        {
            get { return mz; }
            set { mz = value; }
        }

        [Key(2)]
        public float Intensity
        {
            get { return intensity; }
            set { intensity = value; }
        }

        [Key(3)]
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }
    }
}
