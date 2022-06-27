namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class MsSelectionItem
    {
        public MsSelectionItem(int id, double collisionEnergy) {
            Id = id;
            CollisionEnergy = collisionEnergy;
        }

        public int Id { get; }
        public double CollisionEnergy { get; }
    }
}
