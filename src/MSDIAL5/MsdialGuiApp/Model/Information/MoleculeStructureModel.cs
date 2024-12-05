using CompMs.Common.DataStructure;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using NCDK.Depict;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.Model.Information;

internal sealed class MoleculeStructureModel : DisposableModelBase
{
    private CacheProxy<IMoleculeProperty, MoleculeImage>? _proxy;

    public MoleculeStructureModel() {
        _proxy = new CacheProxy<IMoleculeProperty, MoleculeImage>(20, MoleculeImage.Create, MoleculeImage.Comparer);
    }

    public MoleculeImage? Current {
        get => _current;
        private set => SetProperty(ref _current, value);
    }
    private MoleculeImage? _current;

    public void UpdateMolecule(IMoleculeProperty? molecule) {
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

internal sealed class MoleculeImage : DisposableModelBase {
    public static readonly MoleculeImage FailedMoleculeImage = new MoleculeImage();
    public static readonly IEqualityComparer<IMoleculeProperty> Comparer = new SMILESComparer();

    private CancellationTokenSource? _cts;

    private MoleculeImage() {
        Image = Task.FromResult((BitmapSource?)null);
        IsFailed = true;
        _cts = null;
    }

    public MoleculeImage(IMoleculeProperty molecule) {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        Image = Task.Run(() =>
        {
            IsLoading = true;
            try {
                var atomContainer = molecule.ToAtomContainer();
                token.ThrowIfCancellationRequested();
                var depiction = new DepictionGenerator().Depict(atomContainer);
                token.ThrowIfCancellationRequested();
                BitmapSource bitmapSource = depiction.ToBitmap();
                bitmapSource.Freeze();
                return bitmapSource;
            }
            catch (OperationCanceledException) {
                IsFailed = false;
                return null;
            }
            catch {
                IsFailed = true;
                return null;
            }
            finally {
                IsLoading = false;
            }
        }, token);
    }

    public Task<BitmapSource?> Image { get; }

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

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        _cts?.Cancel();
        _cts = null;
    }

    public static MoleculeImage Create(IMoleculeProperty molecule) {
        return new MoleculeImage(molecule);
    }

    class SMILESComparer : IEqualityComparer<IMoleculeProperty>
    {
        bool IEqualityComparer<IMoleculeProperty>.Equals(IMoleculeProperty x, IMoleculeProperty y) {
            return x.SMILES == y.SMILES;
        }

        int IEqualityComparer<IMoleculeProperty>.GetHashCode(IMoleculeProperty obj) {
            return obj.SMILES?.GetHashCode() ?? 0;
        }
    }
}
