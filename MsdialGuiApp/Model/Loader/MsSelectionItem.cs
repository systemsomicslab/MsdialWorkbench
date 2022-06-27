using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class MsSelectionItem : BindableBase
    {
        private readonly double _collisionEnergy;

        public MsSelectionItem(int id, double collisionEnergy) {
            Id = id;
            _collisionEnergy = collisionEnergy;
        }

        public int Id { get; }

        public override string ToString() {
            return $"{Id}(CE:{_collisionEnergy})";
        }
    }
}
