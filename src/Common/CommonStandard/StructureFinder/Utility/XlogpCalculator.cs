using CompMs.Common.StructureFinder.Property;
using NCDK;
using NCDK.QSAR.Descriptors.Moleculars;
using System;

namespace CompMs.Common.StructureFinder.Utility
{
    internal class XlogpCalculator
    {
        private XlogpCalculator() { }

        public static double XlogP(Structure structure)
        {

            var xlogp = -1.0;
            var descriptor = new XLogPDescriptor();
            var parameters = new Object[2] { true, true };

            //try {
            //    descriptor.setParameters(parameters);
            //}
            //catch (CDKException ex) {
            //    Console.WriteLine(ex.ToString());
            //}

            XLogPDescriptor.Result xlogpResult = null;
            try
            {
                xlogpResult = descriptor.Calculate(structure.IContainer);
            }
            catch (CDKException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (System.NullReferenceException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            if (xlogpResult != null)
            {
                return xlogpResult.Value;
            }
            else
            {
                return -1.0;
            }
            //if (double.TryParse(xlogpResult, out xlogp)) {
            //    return xlogp;
            //}
            //else {
            //    return -1.0;
            //}
        }
    }
}
