using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of 'model' peak used in the deconvolution algorithm. 
    /// The raw chromatogram will be fitted by this storage by means of least square method.
    /// The model peak should be satisfied with the ideal slope value criteria etc.
    /// </summary>
    [Serializable()]
    [DataContract]
    [MessagePackObject]
    public class ModelpeakBean
    {
        [DataMember]
        int startScanNumber;
        [DataMember]
        int endScanNumber;
        [DataMember]
        int peaktopScanNumber;
        [DataMember]
        float startRt;
        [DataMember]
        float endRt;
        [DataMember]
        float peaktopRt;
        [DataMember]
        float uniqueMs;
        [DataMember]
        bool focusPeak;
        [DataMember]
        List<int> mzList;
        [DataMember]
        List<int> intensityList;
        [DataMember]
        List<float> rtList;

        public ModelpeakBean()
        {
            intensityList = new List<int>();
            rtList = new List<float>();
            mzList = new List<int>();
        }

        [Key(0)]
        public List<float> RtList
        {
            get { return rtList; }
            set { rtList = value; }
        }

        [Key(1)]
        public int PeaktopScanNumber
        {
            get { return peaktopScanNumber; }
            set { peaktopScanNumber = value; }
        }

        [Key(2)]
        public float PeaktopRt
        {
            get { return peaktopRt; }
            set { peaktopRt = value; }
        }


        [Key(3)]
        public int EndScanNumber
        {
            get { return endScanNumber; }
            set { endScanNumber = value; }
        }

        [Key(4)]
        public int StartScanNumber
        {
            get { return startScanNumber; }
            set { startScanNumber = value; }
        }

        [Key(5)]
        public float EndRt
        {
            get { return endRt; }
            set { endRt = value; }
        }

        [Key(6)]
        public float StartRt
        {
            get { return startRt; }
            set { startRt = value; }
        }

        [Key(7)]
        public List<int> IntensityList
        {
            get { return intensityList; }
            set { intensityList = value; }
        }

        [Key(8)]
        public List<int> MzList
        {
            get { return mzList; }
            set { mzList = value; }
        }

        [Key(9)]
        public float UniqueMs
        {
            get { return uniqueMs; }
            set { uniqueMs = value; }
        }

        [Key(10)]
        public bool FocusPeak
        {
            get { return focusPeak; }
            set { focusPeak = value; }
        }
    }
}
