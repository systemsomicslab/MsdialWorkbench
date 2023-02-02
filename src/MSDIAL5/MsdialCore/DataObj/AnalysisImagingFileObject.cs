using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class AnalysisImagingFileObject
    {
        private readonly string _image;
        private readonly string _spectra;

        public AnalysisImagingFileObject(AnalysisFileBean analysisFile) {
            var document = XDocument.Load(analysisFile.AnalysisFilePath);
            _image = document.Descendants("ImageFile").FirstOrDefault()?.Value;
            if (_image is null) {
                throw new ArgumentException(string.Format("{0} doesn't contain \"ImageFile\" info", analysisFile.AnalysisFilePath));
            }
            if (!File.Exists(_image)) {
                throw new FileNotFoundException(string.Format("Image file {0} is not found.", _image));
            }
            _spectra = Path.ChangeExtension(analysisFile.AnalysisFilePath, ".d");
            if (!Directory.Exists(_spectra)) {
                throw new FileNotFoundException(string.Format("Spectra file {0} is not found.", _spectra));
            }
        }
    }
}
