using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class StudyContextModel : BindableBase
    {
        private readonly ProjectBaseParameter _parameter;

        public StudyContextModel(ProjectBaseParameter parameter)
        {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _authors = parameter.Authors;
            _license = parameter.License;
            _instrument = parameter.Instrument;
            _instrumentType = parameter.InstrumentType;
            _collisionEnergy = parameter.CollisionEnergy ;
            _comment = parameter.Comment;
            _constrainToMassBankFormat = parameter.ConstrainToMassBankFormat;
            _massBankContributorId = parameter.MassBankContributorId ?? string.Empty;
        }

        public string Authors {
            get => _authors;
            set => SetProperty(ref _authors, value);
        }
        private string _authors;

        public string License {
            get => _license;
            set => SetProperty(ref _license, value);
        }
        private string _license;

        public string Instrument {
            get => _instrument;
            set => SetProperty(ref _instrument, value);
        }
        private string _instrument;

        public string InstrumentType {
            get => _instrumentType;
            set => SetProperty(ref _instrumentType, value);
        }
        private string _instrumentType;

        public string CollisionEnergy {
            get => _collisionEnergy;
            set => SetProperty(ref _collisionEnergy, value);
        }
        private string _collisionEnergy = string.Empty;

        public string Comment {
            get => _comment;
            set => SetProperty(ref _comment, value);
        }
        private string _comment = string.Empty;

        /// <summary>
        /// Whether Project property setting only accepts metadata that makes valid MassBank records; see
        /// <see cref="ProjectBaseParameter.ConstrainToMassBankFormat"/>.
        /// </summary>
        public bool ConstrainToMassBankFormat {
            get => _constrainToMassBankFormat;
            set => SetProperty(ref _constrainToMassBankFormat, value);
        }
        private bool _constrainToMassBankFormat;

        /// <summary>The contributor ID MassBank assigned, which the MassBank export dialogs start from; empty when not set.</summary>
        public string MassBankContributorId {
            get => _massBankContributorId;
            set => SetProperty(ref _massBankContributorId, value ?? string.Empty);
        }
        private string _massBankContributorId = string.Empty;

        /// <summary>The kind of instrument the project was created for; it does not change after the project is created.</summary>
        public MachineCategory MachineCategory => _parameter.MachineCategory;

        public void Commit() {
            _parameter.Authors = _authors;
            _parameter.License = _license;
            _parameter.Instrument = _instrument;
            _parameter.InstrumentType = _instrumentType;
            _parameter.CollisionEnergy = _collisionEnergy;
            _parameter.Comment = _comment;
            _parameter.ConstrainToMassBankFormat = _constrainToMassBankFormat;
            _parameter.MassBankContributorId = _massBankContributorId;
        }

        public void Cancel() {
            Authors = _parameter.Authors;
            License = _parameter.License;
            Instrument = _parameter.Instrument;
            InstrumentType = _parameter.InstrumentType;
            CollisionEnergy = _parameter.CollisionEnergy ;
            Comment = _parameter.Comment;
            ConstrainToMassBankFormat = _parameter.ConstrainToMassBankFormat;
            MassBankContributorId = _parameter.MassBankContributorId;
        }
    }
}
