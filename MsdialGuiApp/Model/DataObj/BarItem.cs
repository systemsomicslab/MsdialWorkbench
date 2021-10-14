namespace CompMs.App.Msdial.Model.DataObj
{
    public class BarItem
    {
        public BarItem(string class_, double height, double error) {
            Class = class_;
            Height = height;
            Error = error;
        }

        public string Class { get; }
        public double Height { get; }
        public double Error { get; }
    }
}
