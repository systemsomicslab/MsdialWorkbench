using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using CompMs.Common.DataObj.Result;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsFastaAnnotationSettingModel : FastaAnnotationSettingModel {
        
        public ParameterBase ParameterBase { get; }
        public LcmsFastaAnnotationSettingModel(FastaAnnotationSettingModel other, ParameterBase paramBase)
            : base(other) {
            this.ParameterBase = paramBase;
        }

        public override ISerializableAnnotatorContainer Build(ParameterBase parameter) {
            var molecules = LoadDataBase(parameter);
            return Build(parameter.ProjectParam, molecules);
        }

        private ISerializableAnnotatorContainer Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new ShotgunProteomicsDBAnnotatorContainer(
                new LcmsFastaAnnotator(molecules.Database, Parameter, this.ParameterBase.ProteomicsParam, AnnotatorID, molecules.SourceType),
                molecules,
                Parameter);
        }

        private MoleculeDataBase LoadDataBase(ParameterBase parameter) {
            switch (DBSource) {
                case DataBaseSource.Fasta:
                    return new MoleculeDataBase(LoadFastaDataBase(DataBasePath, AnnotationSource == SourceType.FastaDB ? false : true, parameter), DataBaseID, DBSource, AnnotationSource);
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }

        private List<MoleculeMsReference> LoadFastaDataBase(string dataBasePath, bool isDecoyDB, ParameterBase param) {
            var fastaQueries = FastaParser.ReadFastaUniProtKB(dataBasePath);
            if (isDecoyDB) {
                fastaQueries = DecoyCreator.Convert2DecoyQueries(fastaQueries);
            }

            var cleavageSites = ProteinDigestion.GetCleavageSites(param.EnzymesForDigestion);
            var modContainer = ModificationUtility.GetModificationContainer(param.FixedModifications, param.VariableModifications);
            var maxMissedCleavage = param.MaxMissedCleavage;
            var maxNumberOfModificationsPerPeptide = param.MaxNumberOfModificationsPerPeptide;
            var adduct = AdductIonParser.GetAdductIonBean("[M+H]+");

            var refQueries = new List<MoleculeMsReference>();
            //foreach (var fQuery in fastaQueries) {
            //    if (fQuery.IsValidated) {
            //        var sequence = fQuery.Sequence;
            //        var digestedPeptides = ProteinDigestion.GetDigestedPeptideSequences(sequence, cleavageSites, maxMissedCleavage, 
            //            fQuery.UniqueIdentifier, fQuery.Index);

            //        //Console.WriteLine(fQuery.Header);
            //        if (!digestedPeptides.IsEmptyOrNull()) {

            //            var mPeptides = ModificationUtility.GetModifiedPeptides(digestedPeptides, modContainer, maxNumberOfModificationsPerPeptide);
            //            foreach (var peptide in mPeptides.OrderByDescending(n => n.ExactMass)) {
            //                var refSpec = SequenceToSpec.Convert2SpecObj(peptide, adduct, CollisionType.HCD);
            //                refQueries.Add(refSpec);
            //            }
            //        }
            //    }
            //}
            return refQueries;
        }
    }
}
