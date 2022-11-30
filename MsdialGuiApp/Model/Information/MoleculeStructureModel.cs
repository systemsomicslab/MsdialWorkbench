using CompMs.Common.DataStructure;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using NCDK.Depict;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.Model.Information
{
    internal sealed class MoleculeStructureModel : DisposableModelBase
    {
        private CacheProxy<IMoleculeProperty, MoleculeImage> _proxy;

        public MoleculeStructureModel() {
            _proxy = new CacheProxy<IMoleculeProperty, MoleculeImage>(20, MoleculeImage.Create);
        }

        public MoleculeImage Current {
            get => _current;
            private set => SetProperty(ref _current, value);
        }
        private MoleculeImage _current;

        public void UpdateMolecule(IMoleculeProperty molecule) {
            if (molecule is null) {
                Current = MoleculeImage.FailedMoleculeImage;
                return;
            }
            Current = _proxy?.GetOrAdd(molecule) ?? MoleculeImage.FailedMoleculeImage;
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _proxy = null;
            Current = null;
        }
    }

    internal sealed class MoleculeImage : BindableBase {
        public static readonly MoleculeImage FailedMoleculeImage = new MoleculeImage();

        private MoleculeImage() {
            Image = Task.FromResult((BitmapSource)null);
            IsFailed = true;
        }

        public MoleculeImage(IMoleculeProperty molecule) {
            Image = Task.Run(() =>
            {
                IsLoading = true;

                try {
                    var atomContainer = molecule.ToAtomContainer();
                    var depiction = new DepictionGenerator().Depict(atomContainer);
                    BitmapSource bitmapSource = depiction.ToBitmap();
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
                catch {
                    IsFailed = true;
                    return null;
                }
                finally {
                    IsLoading = false;
                }
            });
        }

        public Task<BitmapSource> Image { get; }

        public bool IsLoading {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }
        private bool _isLoading = false;

        public bool IsFailed {
            get => _isFailed;
            private set => SetProperty(ref _isFailed, value);
        }
        private bool _isFailed = false;

        public static MoleculeImage Create(IMoleculeProperty molecule) {
            return new MoleculeImage(molecule);
        }
    }
}
