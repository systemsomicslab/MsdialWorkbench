using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Riken.Metabolomics.NetCDF4;

namespace Riken.Metabolomics.NcData
{
    public enum MeasurementType { SCAN, MRM, PDA, Analog };
    public enum QualAnalysisType { MS }
    public enum ScanType { MS, MS2, MS3, MS4, SWATH, MSe }
    public enum AcquisitionMethod { SWATH }
    public enum ProcessMethod { centroid, profile, unknown }
    public enum Polarity { positive, negative, both, unknown }
    /// <summary>
    /// storing NetCDF dimention info
    /// </summary>
    class NcDim
    {
        private int id;
        internal int Id { get { return this.id; } }
        private string name;
        internal string Name { get { return this.name; } }
        private int len;
        internal int Len { get { return this.len; } }

        /// <summary>
        /// NcDimの作成
        /// </summary>
        /// <param name="idIn">Dimension ID : 0..</param>
        /// <param name="nameIn">Name</param>
        /// <param name="lenIn">Length</param>
        internal NcDim(int idIn, string nameIn, int lenIn)
        {
            id = idIn;
            name = nameIn;
            len = lenIn;
        }
    }// NcDic
    /// <summary>
    /// storing NetCDF attributes
    /// </summary>
    class NcAtt
    {
        private int id;
        internal int Id { get { return this.id; } }
        private string name;
        internal string Name { get { return this.name; } }
        private NetCDF.NcType type;
        internal NetCDF.NcType Type { get { return this.type; } }
        private int len;
        internal int Len { get { return this.len; } }
        /// <summary>
        /// NcAttの作成
        /// </summary>
        /// <param name="idIn">Attribute ID : 0..</param>
        /// <param name="nameIn">Name</param>
        /// <param name="typeIn">NcType</param>
        /// <param name="lenIn">Length</param>
        public NcAtt(int idIn, string nameIn, NetCDF.NcType typeIn, int lenIn)
        {
            id = idIn;
            name = nameIn;
            type = typeIn;
            len = lenIn;
        }
    }// NcAtt
    /// <summary>
    /// storing NetCDF variables
    /// </summary>
    class NcVar
    {
        private int id;
        internal int Id { get { return this.id; } }
        private string name;
        internal string Name { get { return this.name; } }
        private NetCDF.NcType type;
        internal NetCDF.NcType Type { get { return this.type; } }
        private int numDim;
        internal int NumDim { get { return this.numDim; } }
        private int[] dimids;
        internal int[] Dimids { get { return this.dimids; } }
        private int numAtt;
        internal int NumAtt { get { return this.numAtt; } }

        /// <summary>
        /// NcVar generation
        /// </summary>
        /// <param name="idIn">Variable ID : 0..</param>
        /// <param name="nameIn">Name</param>
        /// <param name="typeIn">Type</param>
        /// <param name="numDimIn">#Dimension</param>
        /// <param name="dimidsIn">Dimension ID配列</param>
        /// <param name="numAttIn">#Attribute</param>
        public NcVar(int idIn, string nameIn, NetCDF.NcType typeIn, int numDimIn, int[] dimidsIn, int numAttIn)
        {
            id = idIn;
            name = nameIn;
            type = typeIn;
            numDim = numDimIn;
            dimids = dimidsIn;
            numAtt = numAttIn;
        }
    }// NcVar
    /// <summary>
    /// storing NetCDF file info
    /// </summary>
    class NcObject
    {
        private string fileName;
        public string FileName { get { return this.fileName; } }
        private bool isReadOnly;
        public bool IsReadOnly { get { return this.isReadOnly; } }
        private bool isOpened;
        public bool IsOpened { get { return this.isOpened; } }
        public Measurement Measurement;
        private int isError;
        public int IsError { get { return this.isError; } }
        private int ncid;
        private int numDim;
        private int numVar;
        private int numGAtt;

        private int ulvid;
        private int[] scanIndex;
        private int[] pointCount;
        private double[] scanAcquisitionTime;
        private double[] totalIntensity;

        //dicDim
        static int numDimMax = (int)NetCDF.Limits.NC_MAX_DIMS;
        private Dictionary<int, NcDim> dicDimId = new Dictionary<int, NcDim>(numDimMax);
        private Dictionary<string, NcDim> dicDimName = new Dictionary<string, NcDim>(numDimMax);

        // dicGAtt
        static int numAttMax = (int)NetCDF.Limits.NC_MAX_ATTRS;
        internal Dictionary<int, NcAtt> dicGAttId = new Dictionary<int, NcAtt>(numAttMax);
        internal Dictionary<string, NcAtt> dicGAttName = new Dictionary<string, NcAtt>(numAttMax);
        internal Dictionary<string, string> dicGAttString = new Dictionary<string, string>(numAttMax);
        internal Dictionary<string, short[]> dicGAttShort = new Dictionary<string, short[]>(numAttMax);
        internal Dictionary<string, int[]> dicGAttInt = new Dictionary<string, int[]>(numAttMax);
        internal Dictionary<string, double[]> dicGAttDouble = new Dictionary<string, double[]>(numAttMax);
        internal Dictionary<string, float[]> dicGAttFloat = new Dictionary<string, float[]>(numAttMax);

        // dicVar
        static int numVarMax = (int)NetCDF.Limits.NC_MAX_VARS;
        internal Dictionary<string, NcVar> dicVar = new Dictionary<string, NcVar>((int)NetCDF.Limits.NC_MAX_VARS);
        internal Dictionary<string, short[]> dicVarScanShort = new Dictionary<string, short[]>(numVarMax);
        internal Dictionary<string, int[]> dicVarScanInt = new Dictionary<string, int[]>(numVarMax);
        internal Dictionary<string, double[]> dicVarScanDouble = new Dictionary<string, double[]>(numVarMax);
        internal Dictionary<string, float[]> dicVarScanFloat = new Dictionary<string, float[]>(numVarMax);
        internal Dictionary<string, short[]> dicVarPointShort = new Dictionary<string, short[]>(numVarMax);
        internal Dictionary<string, int[]> dicVarPointInt = new Dictionary<string, int[]>(numVarMax);
        internal Dictionary<string, double[]> dicVarPointDouble = new Dictionary<string, double[]>(numVarMax);
        internal Dictionary<string, float[]> dicVarPointFloat = new Dictionary<string, float[]>(numVarMax);
        internal Dictionary<string, string> dicVarInstrumentString = new Dictionary<string, string>(numVarMax);

        public int VarScanValInt(string name, int id)
        {
            //Console.WriteLine("{0}",dicVar);
            if (dicVar.ContainsKey(name)) {
                NcVar ncVar = dicVar[name];
                //Console.WriteLine("{0}", ncVar.Type);
                return id;
            }
            else {
                return -1;
            }
        }
        internal int NumScan { get { return this.dicDimName.ContainsKey("scan_number") ? this.dicDimName["scan_number"].Len : 0; } }
        internal int NumPoint { get { return this.dicDimName.ContainsKey("point_number") ? this.dicDimName["point_number"].Len : 0; } }
        public int[] ScanIndex { get { return this.scanIndex; } }
        public int[] PointCount { get { return this.pointCount; } }
        public double[] ScanAcquisitionTime { get { return this.scanAcquisitionTime; } }
        public double[] TotalIntensity { get { return this.totalIntensity; } }
        /// <summary>
        /// NetCDF open
        /// </summary>
        /// <returns>true or false</returns>
        private bool Open()
        {
            if (!this.isOpened) this.Close();
            this.isError = NetCDF.nc_open(this.fileName, NetCDF.ModeOpen.NC_NOWRITE, out this.ncid);
            if (this.isError == 0) { this.isOpened = true; return true; } else { return false; }
        }
        /// <summary>
        /// NetCDF close
        /// </summary>
        /// <returns>true or false</returns>
        private bool Close()
        {
            if (this.IsOpened) NetCDF.nc_close(this.ncid);
            this.isOpened = false;
            return true;
        }
        internal string GAttValString(string varnm)
        {
            if (this.dicGAttString.ContainsKey(varnm)) {
                return dicGAttString[varnm];
            }
            else {
                return "";
            }
        }
        /// <summary>
        /// RawObject generation
        /// </summary>
        /// <param name="fileNameIn">NetCDF file</param>
        /// <param name="rflag">check if the reader is started when the file is generated</param>
        public NcObject(string fileNameIn, bool rflag = false)
        {
            this.fileName = fileNameIn;
            this.isReadOnly = true;
            if (!this.Open()) return;
            this.Close();
            if (rflag == true) { this.ReadFile(); }
        }
        /// <summary>
        /// RawObject destructor
        /// </summary>
        ~NcObject()
        {
            if (this.isOpened) this.Close();
            //Console.Error.WriteLine("Freed {0}", this.fileName);
        }
        /// <summary>
        /// read NetCDF
        /// </summary>
        /// <returns>return true if succeeded</returns>
        private bool ReadFile()
        {
            if (!this.isOpened) {
                NetCDF.nc_open(this.fileName, NetCDF.ModeOpen.NC_NOWRITE, out this.ncid);
                this.isOpened = true;
            }
            int ret1 = NetCDF.nc_inq(this.ncid, out this.numDim, out this.numVar, out this.numGAtt, out this.ulvid);
            int nmlenmax = (int)NetCDF.Limits.NC_MAX_NAME;

            for (int dimid = 0; dimid < numDim; dimid++) {
                StringBuilder sbname = new StringBuilder(nmlenmax);
                int len;
                int ret = NetCDF.nc_inq_dim(ncid, dimid, sbname, out len);
                string name = sbname.ToString();
                NcDim ncDim = new NcDim(dimid, name, len);
                this.dicDimId.Add(dimid, ncDim);
                this.dicDimName.Add(name, ncDim);
            }

            for (int gattid = 0; gattid < numGAtt; gattid++) // Global Attribute
            {
                StringBuilder sbname = new StringBuilder(nmlenmax);
                NetCDF.NcType type;
                int len;
                int ret = NetCDF.nc_inq_attname(ncid, -1, gattid, sbname);
                string name = sbname.ToString();
                ///Console.WriteLine("{0}",name);
                int ret2 = NetCDF.nc_inq_att(ncid, -1, name, out type, out len);
                ///Console.WriteLine("{0} {1} {2}",name, type,len);
                NcAtt ncAtt = new NcAtt(gattid, name, type, len);
                this.dicGAttId.Add(gattid, ncAtt);
                this.dicGAttName.Add(name, ncAtt);

                if (type == NetCDF.NcType.NC_CHAR) {
                    StringBuilder sbval = new StringBuilder(len);
                    int ret3 = NetCDF.nc_get_att_text(ncid, -1, name, sbval);
                    this.dicGAttString.Add(name, sbval.ToString());
                }
                else if (type == NetCDF.NcType.NC_SHORT) {
                    short[] val = new short[len];
                    int ret3 = NetCDF.nc_get_att_short(ncid, -1, name, val);
                    this.dicGAttShort.Add(name, val);
                }
                else if (type == NetCDF.NcType.NC_DOUBLE) {
                    double[] val = new double[len];
                    int ret3 = NetCDF.nc_get_att_double(ncid, -1, name, val);
                    this.dicGAttDouble.Add(name, val);
                }
                else if (type == NetCDF.NcType.NC_FLOAT) {
                    float[] val = new float[len];
                    int ret3 = NetCDF.nc_get_att_float(ncid, -1, name, val);
                    this.dicGAttFloat.Add(name, val);
                }
                else if (type == NetCDF.NcType.NC_INT) {
                    int[] val = new int[len];
                    int ret3 = NetCDF.nc_get_att_int(ncid, -1, name, val);
                    this.dicGAttInt.Add(name, val);
                }
                else {
                    Console.Error.WriteLine("?????? {0} {1} {2}", name, type, len);
                    Environment.Exit(0);
                }
            }
            for (var varid = 0; varid < numVar; varid++) {
                StringBuilder sbname = new StringBuilder(nmlenmax);
                NetCDF.NcType type;
                int numDimVar;
                int numAttVar;
                int retVar1 = NetCDF.nc_inq_varndims(ncid, varid, out numDimVar);
                int[] dimids = new int[numDimVar];
                int retVar2 = NetCDF.nc_inq_var(ncid, varid, sbname, out type, out numDimVar, dimids, out numAttVar);
                string varnm = sbname.ToString();
                for (int attid = 0; attid < numAttVar; attid++) {

                }
                NcVar ncVar = new NcVar(varid, varnm, type, numDimVar, dimids, numAttVar);
                this.dicVar.Add(varnm, ncVar);
                NcDim dim0 = dicDimId[dimids[0]];

                if (dim0.Name == "scan_number" || dim0.Name == "point_number") {
                    if (type == NetCDF.NcType.NC_INT) {
                        int[] vals = new int[dim0.Len];
                        int retVar = NetCDF.nc_get_var_int(ncid, varid, vals);
                        if (dim0.Name == "scan_number") this.dicVarScanInt.Add(varnm, vals);
                        if (dim0.Name == "point_number") this.dicVarPointInt.Add(varnm, vals);
                    }
                    else if (type == NetCDF.NcType.NC_SHORT) {
                        short[] vals = new short[dim0.Len];
                        int retVar = NetCDF.nc_get_var_short(ncid, varid, vals);
                        if (dim0.Name == "scan_number") this.dicVarScanShort.Add(varnm, vals);
                        if (dim0.Name == "point_number") this.dicVarPointShort.Add(varnm, vals);
                    }
                    else if (type == NetCDF.NcType.NC_DOUBLE) {
                        double[] vals = new double[dim0.Len];
                        int retVar = NetCDF.nc_get_var_double(ncid, varid, vals);
                        if (dim0.Name == "scan_number") this.dicVarScanDouble.Add(varnm, vals);
                        if (dim0.Name == "point_number") this.dicVarPointDouble.Add(varnm, vals);
                    }
                    else if (type == NetCDF.NcType.NC_FLOAT) {
                        float[] vals = new float[dim0.Len];
                        int retVar = NetCDF.nc_get_var_float(ncid, varid, vals);
                        if (dim0.Name == "scan_number") this.dicVarScanFloat.Add(varnm, vals);
                        if (dim0.Name == "point_number") this.dicVarPointFloat.Add(varnm, vals);
                    }
                    else {
                        Console.WriteLine("Unknown var(scan_number) varnm:{0} type:{1}", varnm, type);
                        Environment.Exit(1);
                    }
                }
                else if (dim0.Name == "instrument_number") {
                    if (type == NetCDF.NcType.NC_CHAR) {
                        NcDim dim1 = dicDimId[dimids[1]];
                        //string val = new string(dim1.Len);
                        byte[] data = new byte[dim1.Len];
                        int retVar = NetCDF.nc_get_var_text(ncid, varid, data);
                        string val = System.Text.Encoding.ASCII.GetString(data);
                        this.dicVarInstrumentString.Add(varnm, val);
                    }
                }
                else if (dim0.Name == "error_number") {

                }
                else {
                    Console.WriteLine("Unknown var({0}) varnm:{1} type:{2}", dim0.Name, varnm, type);
                    Environment.Exit(1);
                }
            }
            if (this.dicVarScanInt.ContainsKey("scan_index")) {
                this.scanIndex = dicVarScanInt["scan_index"];
            }
            else {
                Console.WriteLine("no scan_index");
                Environment.Exit(1);
            }
            if (this.dicVarScanInt.ContainsKey("point_count")) {
                this.pointCount = dicVarScanInt["point_count"];
            }
            else {
                Console.WriteLine("no point_count");
                Environment.Exit(1);
            }
            if (dicVarScanDouble.ContainsKey("scan_acquisition_time")) {
                this.scanAcquisitionTime = dicVarScanDouble["scan_acquisition_time"];
            }
            else {
                Console.WriteLine("no scan_acquisition_time");
                Environment.Exit(1);
            }
            if (dicVarScanDouble.ContainsKey("total_intensity")) {
                this.totalIntensity = dicVarScanDouble["total_intensity"];
            }
            else {
                Console.WriteLine("no total_intensity");
                Environment.Exit(1);
            }
            if (!dicVar.ContainsKey("mass_values")) {
                Console.WriteLine("no mass_values");
                Environment.Exit(1);
            }
            if (!dicVar.ContainsKey("intensity_values")) {
                Console.WriteLine("no intensity_values");
                Environment.Exit(1);
            }
           
            this.Measurement = new Measurement(this, MeasurementType.SCAN);
            return true;
        }

    }
    // RawObject
    /// <summary>
    /// 
    /// </summary>
    class Measurement
    {
        private NcObject rawObject;
        public NcObject RawObject { get { return this.rawObject; } }
        private MeasurementType type;
        public MeasurementType Type { get { return this.type; } }
        private ProcessMethod processMethod;
        public ProcessMethod ProcessMethod { get { return this.processMethod; } }
        private string originalFileName;
        public string OriginalFileName { get { return this.originalFileName; } }
        private string sampleName;
        public string SampleName { get { return this.sampleName; } }
        public bool IsStandard { set; get; }
        public bool IsQualityControled { set; get; }
        private string instrumentName;
        public string InstrumentName { get { return this.instrumentName; } }
        private string instrumentModel;
        public string InstrumentModel { get { return this.instrumentModel; } }
        private string measurementOperator;
        public string MeasurementOperator { get { return this.measurementOperator; } }
        private Polarity measurementPolarity;
        public Polarity MeasurementPolarity { get { return this.measurementPolarity; } }
        public string PlateName { set; get; }
        public string PlatePosition { set; get; }
        public string RackCode { set; get; }
        public string RackPosition { set; get; }
        public string VialPosition { set; get; }
        private string measurementDate;
        public string MeasurementDate { get { return this.measurementDate; } }

        private List<QualAnalysis> qualAnalysisList;
        public List<QualAnalysis> QualAnalysisList { get { return this.qualAnalysisList; } }
        /// <summary>
        /// Measurement
        /// </summary>
        /// <param name="rawObjectIn">RawObject</param>
        public Measurement(NcObject rawObjectIn, MeasurementType typeIn)
        {
            this.rawObject = rawObjectIn;
            this.type = typeIn;
            QualAnalysis qualAnalysis = new QualAnalysis(this, QualAnalysisType.MS);
            this.qualAnalysisList = new List<QualAnalysis>(1);
            this.qualAnalysisList.Add(qualAnalysis);
            string experiment_type = rawObject.GAttValString("experiment_type");
            if (System.Text.RegularExpressions.Regex.IsMatch(experiment_type, @"centroid", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                this.processMethod = ProcessMethod.centroid;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(experiment_type, @"profile", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                this.processMethod = ProcessMethod.profile;
            }
            else {
                this.processMethod = ProcessMethod.unknown;
            }
            this.originalFileName = rawObject.GAttValString("source_file_reference");
            this.sampleName = rawObject.GAttValString("experiment_title");
            this.IsStandard = false;
            this.IsQualityControled = false;
            this.instrumentName = rawObject.dicVarInstrumentString.ContainsKey("instrument_name") ? rawObject.dicVarInstrumentString["instrument_name"] : "-";
            this.instrumentModel = rawObject.dicVarInstrumentString.ContainsKey("instrument_model") ? rawObject.dicVarInstrumentString["instrument_model"] : "-";
            this.measurementDate = rawObject.GAttValString("experiment_date_time_stamp");
            this.measurementOperator = rawObject.GAttValString("operator_name");
            string test_ionization_polarity = rawObject.GAttValString("test_ionization_polarity");
            if (System.Text.RegularExpressions.Regex.IsMatch(test_ionization_polarity, @"positive", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                this.measurementPolarity = Polarity.positive;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(test_ionization_polarity, @"negative", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                this.measurementPolarity = Polarity.negative;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(test_ionization_polarity, @"both", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                this.measurementPolarity = Polarity.both;
            }
            else {
                this.measurementPolarity = Polarity.unknown;
            }
            this.PlateName = "";
            this.PlatePosition = "";
            this.RackCode = "";
            this.RackPosition = "";
            this.VialPosition = "";

        }
    }// Measurement
    /// <summary>
    /// 
    /// </summary>
    class QualAnalysis
    {
        private Measurement measurement;
        public Measurement Measurement { get { return this.measurement; } }
        private QualAnalysisType type;
        public QualAnalysisType Type { get { return this.type; } }
        public NcObject RawObject { get { return this.measurement.RawObject; } }
        public int NumScan { get { return this.RawObject.NumScan; } }
        public int NumPoint { get { return this.RawObject.NumPoint; } }
        public List<Spectrum> SpectrumList
        {
            get
            {
                NcObject rawObject = this.RawObject;
                List<Spectrum> spectrumList = new List<Spectrum>(rawObject.NumScan);
                int[] scanIndex = rawObject.ScanIndex;
                int[] pointCount = rawObject.PointCount;
                double[] scanAcquisitionTime = rawObject.ScanAcquisitionTime;
                double[] totalIntensity = rawObject.TotalIntensity;

                for (int i = 0; i < rawObject.NumScan; i++) {
                    Spectrum spectrum = new Spectrum(this, i, ScanType.MS, scanIndex[i], pointCount[i], scanAcquisitionTime[i], totalIntensity[i]);
                    //Console.WriteLine("\t\t1:{0}/{1}",i,rawObject.NumScan);
                    //Console.WriteLine("\t\t1:{0}", i);

                    spectrumList.Add(spectrum);
                }
                return spectrumList;
            }
        }
        /// <summary>
        /// QualAnalysis constructor
        /// </summary>
        /// <param name="measurementIn">Measurement</param>
        /// <param name="typeIn">Type</param>
        public QualAnalysis(Measurement measurementIn, QualAnalysisType typeIn)
        {
            this.measurement = measurementIn;
            this.type = typeIn;
        }
    }// QualAnalysis
    /// <summary>
    /// 
    /// </summary>
    class Spectrum
    {
        private QualAnalysis qualAnalysis;
        public QualAnalysis QualAnalysis { get { return this.qualAnalysis; } }
        public Measurement Measurement { get { return this.QualAnalysis.Measurement; } }
        public NcObject RawObject { get { return this.Measurement.RawObject; } }
        private int id;
        public int Id { get { return this.id; } }
        private ScanType scanType;
        public ScanType ScanType { get { return this.scanType; } }
        private int scanIndex;
        public int ScanIndex { get { return this.scanIndex; } }
        private int pointCount;
        public int PointCount { get { return this.pointCount; } }
        private double scanAcquisitionTime;
        public double ScanAcquisitionTime { get { return this.scanAcquisitionTime; } }
        private double totalIntensity;
        public double TotalIntensity { get { return this.totalIntensity; } }
        private double[] peakMass;
        public double[] PeakMass { get { return this.peakMass; } }
        private double[] peakIntensity;
        public double[] PeakIntensity { get { return this.peakIntensity; } }
        private double highestPeakMass;
        public double HighestPeakMass { get { return this.highestPeakMass; } }
        private double lowestPeakMass;
        public double LowestPeakMass { get { return this.lowestPeakMass; } }
        private double highestPeakIntensity;
        public double HighestPeakIntensity { get { return this.highestPeakIntensity; } }
        private double lowestPeakIntensity;
        public double LowestPeakIntensity { get { return this.lowestPeakIntensity; } }
        public int ScanNumber { get { return this.id; } }
        public double RetentionTime { get { return this.ScanAcquisitionTime / 60; } }
        public List<Peak> PeakList
        {
            get
            {
                List<Peak> peakList = new List<Peak>(this.PointCount);

                for (int peakid = scanIndex; peakid < scanIndex + pointCount; peakid++) {
                    Peak peak = new Peak(this, peakid);
                    peakList.Add(peak);
                }
                return peakList;
            }
        }
        /// <summary>
        /// Spectrum
        /// </summary>
        /// <param name="qualAnalysisIn">QualAnalysis</param>
        /// <param name="idIn">QualAnalysis ID : 0..</param>
        /// <param name="typeIn">Type</param>
        /// <param name="scanIndexIn">ScanIndex</param>
        /// <param name="pointCountIn">PointCount</param>
        /// <param name="scanAcquisitionTimeIn">ScanAcquisitionTime(Sec)</param>
        /// <param name="totalIntensityIn">TotalIntensity</param>
        public Spectrum(QualAnalysis qualAnalysisIn, int idIn, ScanType typeIn, int scanIndexIn, int pointCountIn, double scanAcquisitionTimeIn, double totalIntensityIn)
        {
            this.qualAnalysis = qualAnalysisIn;
            this.id = idIn;
            this.scanType = typeIn;
            this.scanIndex = scanIndexIn;
            this.pointCount = pointCountIn;
            this.scanAcquisitionTime = scanAcquisitionTimeIn;
            this.totalIntensity = totalIntensityIn;
            NcObject rawObject = qualAnalysis.RawObject;
            NcVar ncVarMassValues = rawObject.dicVar["mass_values"];
            NcVar ncVarIntensityValues = rawObject.dicVar["intensity_values"];
            this.peakMass = new double[pointCount];
            this.peakIntensity = new double[pointCount];
            if (ncVarMassValues.Type == NetCDF.NcType.NC_DOUBLE) {
                double[] massValues = rawObject.dicVarPointDouble["mass_values"];
                Array.Copy(massValues, scanIndex, peakMass, 0, pointCount);
            }
            else if (ncVarMassValues.Type == NetCDF.NcType.NC_FLOAT) {
                float[] massValues = rawObject.dicVarPointFloat["mass_values"];
                for (int i = scanIndex; i < scanIndex + pointCount; i++) {
                    peakMass[i - scanIndex] = (double)massValues[i];
                }
                //Array.Copy(massValues, scanIndex, peakMass, 0, pointCount);
            }
            else if (ncVarMassValues.Type == NetCDF.NcType.NC_INT) {
                int[] massValues = rawObject.dicVarPointInt["mass_values"];
                Array.Copy(massValues, scanIndex, peakMass, 0, pointCount);
            }
            else if (ncVarMassValues.Type == NetCDF.NcType.NC_SHORT) {
                short[] massValues = rawObject.dicVarPointShort["mass_values"];
                Array.Copy(massValues, scanIndex, peakMass, 0, pointCount);
            }
            this.lowestPeakMass = peakMass.Length > 0 ? peakMass[0] : -1;
            this.highestPeakMass = peakMass.Length > 0 ? peakMass[peakMass.Length - 1] : -1;
            if (ncVarIntensityValues.Type == NetCDF.NcType.NC_DOUBLE) {
                double[] intensityValues = rawObject.dicVarPointDouble["intensity_values"];
                Array.Copy(intensityValues, scanIndex, peakIntensity, 0, pointCount);
            }
            else if (ncVarIntensityValues.Type == NetCDF.NcType.NC_FLOAT) {
                float[] intensityValues = rawObject.dicVarPointFloat["intensity_values"];
                for (int i = scanIndex; i < scanIndex + pointCount; i++) {
                    this.peakIntensity[i - scanIndex] = (double)intensityValues[i];
                }
                //Array.Copy(intensityValues, scanIndex, peakIntensity, 0, pointCount);
            }
            else if (ncVarIntensityValues.Type == NetCDF.NcType.NC_INT) {
                int[] intensityValues = rawObject.dicVarPointInt["intensity_values"];
                Array.Copy(intensityValues, scanIndex, peakIntensity, 0, pointCount);
            }
            else if (ncVarIntensityValues.Type == NetCDF.NcType.NC_SHORT) {
                short[] intensityValues = rawObject.dicVarPointShort["intensity_values"];
                Array.Copy(intensityValues, scanIndex, peakIntensity, 0, pointCount);
            }
            //this.highestPeakIntensity = peakIntensity.Max();
            //this.lowestPeakIntensity = peakIntensity.Min();
            this.highestPeakIntensity = 0;
            this.lowestPeakIntensity = 0;
        }
    }// Spectrum
    /// <summary>
    /// 
    /// </summary>
    class Peak
    {
        private Spectrum spectrum;
        public Spectrum Spectrum { get { return this.spectrum; } }
        private int id;
        public int Id { get { return this.id; } }
        private int rid;
        public int Rid { get { return this.rid; } }
        public QualAnalysis QualAnalysis { get { return this.Spectrum.QualAnalysis; } }
        public Measurement Measurement { get { return this.QualAnalysis.Measurement; } }
        public NcObject RawObject { get { return this.Measurement.RawObject; } }
        /// <summary>
        /// Peak(Point) as double
        /// </summary>
        public double ValueDouble(string varnm)
        {
            NcObject rawObject = this.RawObject;
            if (rawObject.dicVar.ContainsKey(varnm)) {
                NcVar ncVar = rawObject.dicVar[varnm];
                if (ncVar.Type == NetCDF.NcType.NC_DOUBLE) {
                    return rawObject.dicVarPointDouble[varnm][this.Id];
                }
                else if (ncVar.Type == NetCDF.NcType.NC_FLOAT) {
                    return (double)rawObject.dicVarPointFloat[varnm][this.Id];
                }
                else if (ncVar.Type == NetCDF.NcType.NC_INT) {
                    return (double)rawObject.dicVarPointInt[varnm][this.Id];
                }
                else if (ncVar.Type == NetCDF.NcType.NC_SHORT) {
                    return (double)rawObject.dicVarPointShort[varnm][this.Id];
                }
                else {
                    return -1;
                }
            }
            else {
                return -1;
            }

        }
        public double Mz_ { get { return this.ValueDouble("mass_values"); } }
        public double Intensity_ { get { return this.ValueDouble("intensity_values"); } }
        public double Mz { get { return this.Spectrum.PeakMass[this.Rid]; } }
        public double Intensity { get { return this.Spectrum.PeakIntensity[this.Rid]; } }
        /// <summary>
        /// Peak constructor
        /// </summary>
        /// <param name="spectrumIn">Spectrum</param>
        /// <param name="idIn">Peak ID : 0..</param>
        public Peak(Spectrum spectrumIn, int idIn)
        {
            this.spectrum = spectrumIn;
            this.id = idIn;
            this.rid = id - spectrum.ScanIndex;
        }
    }// Peak
}
