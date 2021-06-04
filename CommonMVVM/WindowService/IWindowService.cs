using System;

namespace CompMs.Common.WindowService
{
    public interface IWindowService<TViewModel>
    {
        bool? ShowDialog(TViewModel viewmodel);
        void Show(TViewModel viewmodel);
    }
}
